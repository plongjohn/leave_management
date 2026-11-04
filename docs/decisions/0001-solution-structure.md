# 0001 – Solution structure: Api, Infrastructure, Domain

- **Status:** Accepted
- **Date:** 2026-10-18
- **Updated:** 2026-10-20 – placement of the authentication code (phase 5), test database per
  ADR 0005

## Context

The backend is an ASP.NET Core Web API for leave management: employees request leave,
managers approve or reject it, and the remaining balance is calculated per year. The
business rules (overlapping requests, remaining balance, who may approve, working days
without weekends and public holidays) are the core of the application and must be testable
without a database or a web server.

At the same time the application is small: a handful of entities and a few dozen endpoints.
The structure has to separate business rules from technical details without adding layers
that only forward calls.

## Decision

The backend consists of three projects plus their test projects:

```text
backend/src/
  LeaveManagement.Domain/          entities, business rules, working-day calculation
  LeaveManagement.Infrastructure/  EF Core DbContext, entity configurations, migrations,
                                   Identity user store and refresh token storage
  LeaveManagement.Api/             endpoints, request/response DTOs, validation,
                                   authentication (token issuing, rotation), ProblemDetails
backend/tests/
  LeaveManagement.Domain.Tests/    unit tests of business rules
  LeaveManagement.Api.Tests/       integration tests (WebApplicationFactory + PostgreSQL, ADR 0005)
```

Dependencies point inwards only:

```text
Api → Infrastructure → Domain
Api → Domain
```

- **Domain** references no other project and no EF Core, ASP.NET Core or Identity package.
  Entities protect their invariants through methods (`Approve`, `Reject`, `Cancel`) instead of
  public setters.
- **Infrastructure** maps the domain to the database and implements technical services.
- **Api** orchestrates use cases: it loads entities through the `DbContext`, calls domain
  methods and saves. Use-case logic lives in application services inside the Api project,
  grouped by feature – endpoints stay thin.

No CQRS, no MediatR, no separate application project.

### Alternatives considered

- **Single project with folders:** least ceremony, but nothing stops the domain from
  referencing EF Core or ASP.NET Core; the boundary would only be a convention.
- **Clean Architecture with a separate Application project** (Domain, Application,
  Infrastructure, Api), often combined with CQRS and MediatR: common in larger systems and
  in many company templates. For this size it mostly adds interfaces, request/handler pairs
  and mapping without a second consumer of the application layer. If a second entry point
  appears (e.g. a background worker or a second API), the application services can be moved
  into their own project without changing the domain.

## Consequences

- Business rules are tested with plain unit tests – fast and without infrastructure.
- The compiler enforces that the domain stays free of infrastructure concerns.
- Application services in the Api project use the `DbContext` directly (see ADR 0003); moving
  them later into an Application project is a mechanical refactoring.
- Without MediatR, cross-cutting concerns (validation, logging) are handled with ASP.NET Core
  endpoint filters and middleware instead of pipeline behaviours.
