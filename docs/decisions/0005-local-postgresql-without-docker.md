# 0005 – Local PostgreSQL without Docker

- **Status:** Accepted
- **Date:** 2026-10-18
- **Supersedes:** the Docker parts of [ADR 0002](0002-postgresql-in-docker.md)

## Context

ADR 0002 chose PostgreSQL, run locally with `docker-compose` and in integration tests with
Testcontainers. Docker Desktop on Windows needs hardware virtualization for its WSL 2 VM.
On the development machine, enabling virtualization in the firmware prevents the laptop from
booting, so Docker cannot be used locally.

The choice of PostgreSQL itself is unaffected. What has to change is how the database is
provided for development and for the integration tests – without giving up tests against the
real database engine.

## Decision

### Development

- PostgreSQL is **installed natively** on Windows as a service (installer or
  `winget install PostgreSQL.PostgreSQL.18`).
- A setup script `database/setup-dev.sql`, run once as the `postgres` superuser, creates:
  - the role `leave_app` with the `CREATEDB` privilege (needed by the integration tests,
    see below),
  - the database `leave_management_dev` owned by `leave_app`.
- The password of `leave_app` is chosen by each developer and passed to the script as a
  `psql` variable (`psql -v app_password=… -f database/setup-dev.sql`); it never appears in
  the repository.
- The schema is created by EF Core migrations, not by the script.

### Secrets and connection strings

- **No password is committed** – not even for the local development database.
- The connection string is read from the environment variable
  `ConnectionStrings__LeaveManagement`, which ASP.NET Core maps to
  `ConnectionStrings:LeaveManagement`. `appsettings.Development.json` contains everything
  else, but no connection string.
- The same applies to the other local secret, the JWT signing key (`Jwt__SigningKey`,
  see ADR 0004).
- The application fails at start-up with a clear message naming the missing variable.

### Integration tests

- The tests use the same local PostgreSQL server. They read the connection string from
  `LEAVE_MANAGEMENT_TEST_DB` if set, otherwise from `ConnectionStrings__LeaveManagement`;
  only the database name is replaced by the per-run test database.
- **One database per test run:** a test fixture creates a database with a unique name
  (`leave_test_<guid>`), applies the migrations and drops the database when the run ends.
  Test runs never touch the development database and can run in parallel.
- **Reset between tests with Respawn:** before each test, Respawn deletes the data of all
  tables except the migrations history, so every test starts from a known state.
- `WebApplicationFactory` points the API at the test database.

### Continuous integration

- GitHub Actions starts PostgreSQL as a service container (`postgres:18`) on the Linux runner
  and sets `LEAVE_MANAGEMENT_TEST_DB`. The tests themselves do not know whether the server
  runs natively or in a container.
- The same PostgreSQL major version (18) is used locally and in CI.

### Alternatives considered

- **Committed development password** (as in the previous project): the application runs
  right after cloning, but a password in Git – even a local one – sets a bad example and is
  easily copied into other environments.
- **.NET user secrets:** the usual way to keep secrets out of a .NET project, but only the
  API would see them. The setup script, the integration tests and CI all read environment
  variables, so one mechanism covers everything.
- **Keep Testcontainers only in CI, skip integration tests locally:** the tests that matter
  most for EF Core mappings, constraints and concurrency would only run after a push.
- **Testcontainers Cloud or a remote Docker host:** works without local virtualization, but
  needs an external account and a network connection for every test run.
- **EF Core InMemory provider or SQLite:** rejected for the same reasons as in ADR 0002 –
  different constraint, SQL and transaction behaviour.
- **Transaction rollback per test instead of Respawn:** fast, but the API under test commits
  its own transactions through `SaveChangesAsync`, so the test cannot wrap them reliably.

## Consequences

- Developers install PostgreSQL once, run the setup script and set two user environment
  variables (connection string, JWT signing key); the README describes these steps. There is
  no `docker-compose.yml`.
- Visual Studio and terminals must be restarted after setting the variables.
- References to Testcontainers in ADR 0001 (project overview) are replaced by this setup.
- `leave_app` needs `CREATEDB` in development. This is acceptable for a local development
  role; production would use a role without it.
- If a test run is aborted, its database may remain; the fixture removes leftover
  `leave_test_*` databases older than one day at start-up.
- Should Docker become available later, switching back to Testcontainers only affects the
  test fixture.
