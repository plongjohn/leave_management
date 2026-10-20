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

4. Set the two other local secrets ([ADR 0004](../docs/decisions/0004-self-issued-jwt-authentication.md)):

   - `Jwt__SigningKey` – a random key that signs the access tokens. The API does not start
     without it.
   - `Seed__DevelopmentPassword` – the password of all demo logins, at least 12 characters.
     Needed only when the demo data is seeded.

   ```powershell
   $bytes = [byte[]]::new(32)
   $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   $rng.GetBytes($bytes)
   $rng.Dispose()
   [Environment]::SetEnvironmentVariable("Jwt__SigningKey", [Convert]::ToBase64String($bytes), "User")
   [Environment]::SetEnvironmentVariable("Seed__DevelopmentPassword", "<a password of your choice>", "User")
   ```

   Restart Visual Studio and open terminals afterwards so they see the variables.

5. Apply the migrations (from `backend/`). In the Development environment this also seeds an
   empty database with demo data:

   ```powershell
   dotnet tool restore
   dotnet ef database update --project src/LeaveManagement.Infrastructure --startup-project src/LeaveManagement.Api
   ```

## Demo logins

Every demo employee has a login with the email as user name and the password from
`Seed__DevelopmentPassword`:

| Email | Role | Manager |
|---|---|---|
| `andrea.steiner@example.at` | Manager (managing director) | – (HR decides on her requests) |
| `sabine.huber@example.at` | Hr | Andrea Steiner |
| `markus.gruber@example.at` | Manager | Andrea Steiner |
| `lena.wagner@example.at` | Employee | Markus Gruber |
| `thomas.bauer@example.at` | Employee | Markus Gruber |
| `julia.pichler@example.at` | Employee | Markus Gruber |
| `stefan.moser@example.at` | Employee | Sabine Huber |

The integration tests use the same server and create a separate database per test run.
