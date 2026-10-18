# Coding Guidelines

These rules apply to all code in this repository. Where a rule can be enforced by tooling
(analyzers, linters, formatters), the tooling is the source of truth and the CI fails on
violations. Everything else is checked in code review.

## General

- **Code explains itself.** Small, focused methods and descriptive names for types, methods,
  variables and parameters. Prefer clear code over clever code.
- **Comments explain why, not what.** Comment business rules, trade-offs, workarounds and
  intentional duplication. If a comment is needed to understand *what* code does, refactor
  first (better name, extracted method).
- **No magic numbers.** Use a named constant; if the value is not self-evident, note why it
  was chosen.
- **English** for code, comments, commit messages and documentation. UI texts are German.
- **Decisions are recorded.** Every architectural or technology decision gets an ADR in
  `docs/decisions/` (`NNNN-short-title.md`).

## Design principles – how we apply them

### Single responsibility

- **Endpoints are thin.** They map the request, call one application service or handler,
  and map the result. No business rules and no EF Core queries in endpoints.
- **Business rules live in the domain.** State changes go through methods on the entity
  (`leaveRequest.Approve(approver, now)`), not through public setters set from the outside.
  Entities protect their invariants.
- **One application service per use case area**, not one service for everything.
  If a class needs many unrelated dependencies, it does too much.

### DRY – with judgement

- Extract shared code once the same *knowledge* appears for the third time (rule of three).
- Two pieces of code that look alike but represent different business concepts stay
  separate – coupling them makes later changes harder.
- Business rules exist exactly once (in the domain), never duplicated in the frontend as
  the only check. The frontend may validate for usability; the backend always validates.

### SOLID – pragmatically

- **Interfaces only where there is a real reason:** a second implementation, an external
  system (mail, clock, file storage) or a test seam. No `IFooService` for every `FooService`.
- Use `TimeProvider` for the current time – never `DateTime.Now` / `DateTime.UtcNow` in
  business code.
- Depend on abstractions at the boundaries (infrastructure), not inside the domain.
- No generic repository over EF Core – `DbContext` is the unit of work and `DbSet<T>` the
  repository (see ADR). Complex or reused queries go into extension methods or
  dedicated query classes.

## Backend (C# / ASP.NET Core / EF Core)

### Project layout and dependencies

```text
LeaveManagement.Api            → Infrastructure, Domain
LeaveManagement.Infrastructure → Domain
LeaveManagement.Domain         → (nothing)
```

- The domain project has no reference to EF Core, ASP.NET Core or any infrastructure package.
- Namespaces follow folders. Plural folder names (`Entities/`, `Services/`, `Endpoints/`).

### Language and compiler

- `Nullable` and `ImplicitUsings` enabled in all projects (`Directory.Build.props`).
- .NET analyzers enabled; the CI builds with warnings as errors.
- `async` all the way down; every async method that does I/O accepts a
  `CancellationToken` and passes it on. No `.Result` / `.Wait()`.
- File-scoped namespaces, `sealed` by default for classes that are not designed for
  inheritance, `record` for DTOs.

### API

- **Never expose entities.** Requests and responses are DTOs (records).
- **Errors as `ProblemDetails`** (RFC 9457) – validation errors as `ValidationProblemDetails`
  with field names, business rule violations as 409/422 with a clear message.
- **Validation with FluentValidation** for request shape (required, lengths, formats).
  Business rules (overlap, remaining balance, who may approve) are checked in the domain.
- Status codes: `200` read, `201` + `Location` create, `204` update/delete without body,
  `400` invalid input, `401` / `403` auth, `404` not found, `409` concurrency or conflict.
- Paging, filtering and sorting happen in the database, never in memory.

### EF Core

- Entity configuration in separate `IEntityTypeConfiguration<T>` classes – no data
  annotations on domain entities.
- Read-only queries use `AsNoTracking()` and project to DTOs with `Select`.
- Optimistic concurrency via a concurrency token on entities that can be edited
  concurrently; a `DbUpdateConcurrencyException` becomes a `409`.
- Schema changes only through migrations; migrations are reviewed like code.
- No lazy loading.

### Security

- Self-issued JWT: short-lived access token, rotating refresh token stored hashed,
  refresh token only in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie (see ADR).
- Authorization through policies/roles on every endpoint; deny by default.
- Secrets never in Git, not even development passwords – locally they come from environment
  variables (ADR 0005).
- Logs contain ids, never personal data or tokens.

## Frontend (Angular)

- Standalone components, signals for component state, new control flow (`@if`, `@for`).
- **Feature folders** (`features/my-requests`, `features/approvals`, …) plus `core/` for
  app-wide services (auth, HTTP interceptors, layout) and `shared/` for reusable UI.
- Components are presentational where possible; HTTP calls live in services.
- Strict TypeScript (`strict: true`), no `any`.
- API types are defined once per feature and match the backend DTOs.
- ESLint and Prettier; the CI fails on lint errors.
- All UI texts in German, kept in one place per feature.

## Tests

- **Domain:** unit tests for every business rule (xUnit), no mocks needed.
- **API:** integration tests with `WebApplicationFactory` against a real PostgreSQL – one
  database per test run, reset with Respawn before each test (ADR 0005). No EF Core
  in-memory provider.
- **Frontend:** tests for services and components with logic.
- Arrange / Act / Assert; test names `Method_State_ExpectedResult`
  (e.g. `Approve_RequestAlreadyRejected_Throws`).
- A bug fix comes with a test that fails without the fix.

## Git

- Branches: work happens on `development` (or short-lived feature branches from it);
  `main` only receives merges via pull request.
- One logical change per commit; imperative English messages
  (`Add leave request approval`).
- No force-push, no rewriting of pushed history.
