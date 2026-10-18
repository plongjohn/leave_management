# 0002 – PostgreSQL, run in Docker

- **Status:** Accepted
- **Date:** 2026-10-18

## Context

The application needs a relational database that EF Core supports well. Developers must be
able to start it with one command, and the integration tests need a database that behaves
exactly like the one used when the application runs – including unique constraints, foreign
keys, transactions and concurrency tokens.

## Decision

- **PostgreSQL** with the Npgsql EF Core provider.
- **Local development:** `docker-compose.yml` in the repository root starts PostgreSQL with a
  named volume. The connection string for this development database is committed in
  `appsettings.Development.json`; it contains only the local development password of the
  container.
- **Integration tests:** Testcontainers starts a fresh PostgreSQL container per test run;
  migrations are applied before the tests, and each test cleans up its own data.
- The PostgreSQL major version is pinned in `docker-compose.yml` and in the tests, so both use
  the same engine.

### Alternatives considered

- **SQL Server:** very common in .NET companies and equally well supported by EF Core. The
  container image is large, needs more memory, and the Developer edition licence is limited
  to non-production use. Both would work; PostgreSQL is lighter for local development and CI.
- **MariaDB** (as in the previous project): the EF Core provider (Pomelo) is maintained by the
  community and has historically lagged behind new EF Core releases.
- **EF Core InMemory provider or SQLite for tests:** fast, but they do not enforce the same
  constraints, do not support the same SQL and behave differently with transactions and
  concurrency tokens. Tests could pass while the real database fails.
- **Locally installed PostgreSQL:** no Docker needed, but every developer has to install and
  configure it, and versions drift apart.

## Consequences

- Docker is required for development and for running the integration tests (locally and in
  CI; GitHub's Linux runners provide it).
- Tests run against the real database engine, so constraint and concurrency behaviour is
  covered by tests.
- PostgreSQL-specific features (e.g. `daterange` or exclusion constraints) are available, but
  are only used where they clearly simplify a business rule – and recorded in an ADR.
