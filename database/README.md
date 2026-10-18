# Database

PostgreSQL 18, installed locally (see [ADR 0005](../docs/decisions/0005-local-postgresql-without-docker.md)).
The schema is created by EF Core migrations; this folder only contains the one-time setup of
the development role and database.

## Setup

1. Install PostgreSQL 18, e.g. with `winget install PostgreSQL.PostgreSQL.18 --interactive`
   (server and command line tools are enough).
2. Create the role `leave_app` and the database `leave_management_dev`. You are asked for the
   superuser password and then for a new password for `leave_app`:

   ```powershell
   & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -f database/setup-dev.sql
   ```

3. Store the connection string in a user environment variable (no password is committed):

   ```powershell
   [Environment]::SetEnvironmentVariable(
       "ConnectionStrings__LeaveManagement",
       "Host=localhost;Port=5432;Database=leave_management_dev;Username=leave_app;Password=<your password>",
       "User")
   ```

   Restart Visual Studio and open terminals afterwards so they see the variable.

4. Apply the migrations (from `backend/`):

   ```powershell
   dotnet tool restore
   dotnet ef database update --project src/LeaveManagement.Infrastructure --startup-project src/LeaveManagement.Api
   ```

The integration tests use the same server and create a separate database per test run.
