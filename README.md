# Leave Management

[![CI](https://github.com/plongjohn/leave_management/actions/workflows/ci.yml/badge.svg?branch=development)](https://github.com/plongjohn/leave_management/actions/workflows/ci.yml)

Leave and absence management for a small company: employees request leave, managers approve
or reject it, and HR keeps track of entitlements and public holidays.

> Work in progress – currently a walking skeleton: the employee list runs end to end from
> PostgreSQL through the API to the Angular frontend.

## Tech Stack

| Component | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), Minimal APIs, EF Core |
| Database | PostgreSQL 18 |
| Frontend | Angular 22, Angular Material |
| Tests | xUnit v3 with integration tests against PostgreSQL (Respawn), Vitest |
| CI | GitHub Actions |

## Project Structure

```text
backend/    .NET solution (LeaveManagement.slnx): Domain, Infrastructure, Api + tests
frontend/   Angular application
database/   One-time setup of the development database
docs/       Coding guidelines and architecture decision records
```

## Getting Started

### Prerequisites

- .NET SDK 10
- Node.js 24 LTS
- PostgreSQL 18

### 1. Database

Follow [database/README.md](database/README.md): install PostgreSQL, run the setup script,
set the environment variable `ConnectionStrings__LeaveManagement` and apply the migrations.
No password is stored in the repository.

### 2. Backend

```powershell
cd backend
dotnet run --project src/LeaveManagement.Api --launch-profile http
```

The API runs on <http://localhost:5135>. In the Development environment it fills an empty
database with demo employees and offers an API reference at <http://localhost:5135/scalar>.

### 3. Frontend

```powershell
cd frontend
npm install
npm start
```

Open <http://localhost:4200>. Requests to `/api` are proxied to the backend.

## Tests

```powershell
cd backend
dotnet test          # integration tests – create and drop their own database

cd frontend
npm test             # Vitest
npm run lint
```

## Documentation

- [Coding guidelines](docs/coding-guidelines.md)
- [Decision records](docs/decisions/)
