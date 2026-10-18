# 0006 – Minimal APIs with one endpoint class per feature

- **Status:** Accepted
- **Date:** 2026-10-18

## Context

ASP.NET Core offers two ways to define HTTP endpoints: MVC controllers and Minimal APIs.
Controllers are still the most common style in existing company codebases. For new projects,
Microsoft recommends Minimal APIs; since .NET 7 they support route groups, endpoint filters,
typed results and OpenAPI metadata, which used to be the main reasons for controllers.

The coding guidelines require thin endpoints: map the request, call one application service,
map the result.

## Decision

Endpoints are written as **Minimal APIs**, organised by feature:

```text
LeaveManagement.Api/
  Employees/
    EmployeeEndpoints.cs          AddEmployeeFeature() + MapEmployeeEndpoints()
    EmployeeQueries.cs            read use cases (EF Core projections)
    EmployeeListItemResponse.cs   response DTO
  Program.cs                      only calls Add…Feature() / Map…Endpoints()
```

- Each feature has a static `…Endpoints` class with one extension method that registers its
  services and one that maps a **route group** (`/api/employees`) with OpenAPI tags.
- Handlers are private static methods, not inline lambdas, so they have names in stack traces
  and stay readable when they grow.
- Handlers return **`TypedResults`** (`Ok<T>`, `NotFound`, `ValidationProblem` …), so the
  possible responses are part of the method signature and of the OpenAPI document.
- Cross-cutting concerns (validation, authorization) are added per group with endpoint
  filters and `RequireAuthorization`.

### Alternatives considered

- **MVC controllers:** familiar to most .NET developers and well suited for very large APIs
  with many conventions (filters, model binding customisations). They bring the MVC pipeline
  and attribute routing into a project that does not need them, and actions often end up as
  fat methods inside large controller classes.
- **Minimal APIs defined directly in `Program.cs`:** fine for a demo, but `Program.cs` grows
  with every endpoint and features are spread over one file.
- **Libraries such as FastEndpoints or Carter:** add structure (one class per endpoint), but
  also a third-party dependency and their own conventions on top of what ASP.NET Core already
  provides.

## Consequences

- `Program.cs` stays short; adding a feature means adding one folder and two calls.
- Developers coming from controllers need to know route groups, endpoint filters and typed
  results – the employee feature serves as the template.
- If the API grows very large, moving single features to controllers is possible, as both
  styles can coexist in one application.
