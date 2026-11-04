# 0003 – Use the DbContext directly, no repository pattern

- **Status:** Accepted
- **Date:** 2026-10-18

## Context

Many .NET codebases wrap EF Core in repositories – either a generic `IRepository<T>` with
`GetById`, `GetAll`, `Add`, `Update`, `Delete`, or one repository per aggregate. The previous
project used repositories because it worked with Dapper and plain SQL, where a repository is
the natural place for queries.

EF Core already implements both patterns:

- `DbContext` is a **unit of work**: it tracks changes and writes them in one transaction with
  `SaveChangesAsync`.
- `DbSet<T>` is a **repository**: a collection-like interface to query and add entities.

## Decision

Application services use the `LeaveManagementDbContext` directly. There is no generic
repository and no repository per entity.

- **Read queries** project straight to DTOs with `Select` and `AsNoTracking()`, so only the
  needed columns are loaded.
- **Write use cases** load the entity, call a domain method, and call `SaveChangesAsync`.
- **Queries used in more than one place** (e.g. "requests overlapping a period", "approved
  days of an employee in a year") become extension methods on `IQueryable<T>` or small query
  classes in the Infrastructure project – named after the business question they answer.

### Alternatives considered

- **Generic repository (`IRepository<T>`):**
  - Either it returns `IQueryable<T>`, then EF Core leaks through the abstraction anyway, or it
    returns materialized lists, then filtering, paging, projections and `Include` are lost or
    have to be rebuilt as parameters.
  - `Update` and `Delete` methods suggest that every call writes, while EF Core only writes on
    `SaveChangesAsync` – a second unit of work abstraction is then needed on top.
- **Specific repositories per aggregate** (`ILeaveRequestRepository`): a legitimate choice in
  domain-driven designs with strict aggregate boundaries, or when the persistence technology
  must be replaceable. Neither applies here, and each new query would need changes to an
  interface, an implementation and usually a fake for tests.
- **Repositories to make services unit-testable:** the services are covered by integration
  tests against a real PostgreSQL (ADR 0002); the business rules themselves live in the domain
  and are unit-tested without any database. Mocking a repository would mostly test the mock.

## Consequences

- Fewer types, and queries can use the full power of EF Core (projections, `Include`, paging
  in the database).
- Application services depend on EF Core. This is accepted: replacing EF Core is not a
  realistic requirement, and the domain stays free of it.
- Code review has to watch that query logic does not get duplicated across services – the
  rule of three from the coding guidelines applies, extracted queries go into named
  extension methods.
- If a part of the application later needs a stricter boundary (e.g. a second persistence
  technology), a repository can be introduced for that part only.
