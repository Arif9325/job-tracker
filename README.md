# Job Application Tracker

A full-stack app for tracking job applications — company, role, status,
dates, and notes — with authentication, filtering/sorting, and a live
stats dashboard.

## Why

Job hunting generates a lot of scattered state: which companies you've
applied to, where each one stands, and when to follow up. This replaces
a spreadsheet with a proper app, and doubles as a demonstration of a
complete full-stack workflow: REST API, database, authentication,
tested business logic, and a typed frontend.

## Stack

- **Backend**: ASP.NET Core 8 Web API, Entity Framework Core, SQLite, JWT authentication
- **Frontend**: React + TypeScript (Vite), no UI framework — plain CSS
- **Testing**: xUnit with EF Core's InMemory provider
- **CI**: GitHub Actions — builds and tests both the backend and frontend on every push/PR

## Architecture

```
Browser (React) → REST API (ASP.NET Core Controllers)
                        ↓
              IJobApplicationService / ITokenService
                        ↓
                  EF Core (AppDbContext)
                        ↓
                   SQLite (jobtracker.db)
```

- **Controllers** (`AuthController`, `ApplicationsController`) only handle
  HTTP concerns — status codes, routing, reading the logged-in user's ID
  from the JWT. All real logic lives one layer down.
- **`JobApplicationService`** contains the actual business logic (create,
  update, delete, stats) and is unit tested directly, without needing to
  spin up a real HTTP server.
- **JWT authentication**: on login/register, the API returns a signed
  token; the frontend stores it and attaches it to every subsequent
  request. `[Authorize]` on `ApplicationsController` rejects any request
  without a valid token, and every service method filters by the
  logged-in user's ID — so one user can never see or edit another user's
  applications, even by guessing an ID.
- **Passwords** are hashed with BCrypt (with a per-password random salt)
  before ever touching the database — plaintext passwords are never
  stored.

## Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

### Backend

```bash
cd backend/JobTracker.Api
dotnet restore
dotnet run
```

The API starts at `http://localhost:5203` (see `Properties/launchSettings.json`).
On first run it creates `jobtracker.db` (SQLite) automatically — no
separate database server needed. Swagger UI is available at
`http://localhost:5203/swagger` in development.

**Before deploying anywhere real**, change `Jwt:Key` in `appsettings.json`
to a long random secret — the checked-in value is a development placeholder only.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Opens at `http://localhost:5173` and talks to the API at `http://localhost:5203`.

### Running the tests

```bash
cd backend
dotnet test
```

7 tests cover the service layer: creating/updating/deleting applications,
computing stats, and — importantly — confirming that one user's
applications are never visible or editable by another user's ID.

## API endpoints

| Method | Route | Auth required | Description |
|--------|-------|:---:|-------------|
| POST | `/api/auth/register` | | Create an account |
| POST | `/api/auth/login` | | Log in, get a JWT |
| GET | `/api/applications` | ✓ | List the logged-in user's applications |
| GET | `/api/applications/{id}` | ✓ | Get one application |
| POST | `/api/applications` | ✓ | Create an application |
| PUT | `/api/applications/{id}` | ✓ | Update an application |
| DELETE | `/api/applications/{id}` | ✓ | Delete an application |
| GET | `/api/applications/stats` | ✓ | Counts by status |

## Known tradeoffs / possible extensions

- **`EnsureCreated()` instead of EF migrations** — simpler to run for a
  project this size, but can't evolve an existing schema without losing
  data. A production app would use `dotnet ef migrations add ...`
  instead, precisely to support schema changes over time.
- **SQLite instead of a full database server** — no separate service to
  install locally; would swap the EF Core provider for PostgreSQL/SQL
  Server under real concurrent load.
- No password reset flow, no email verification.
- No pagination on the applications list — fine at personal-use scale,
  would matter at real volume.
- Possible next features: reminder notifications for stale applications,
  a Kanban-style drag-and-drop board, CSV export, deployment to Azure
  App Service with a live demo link.
