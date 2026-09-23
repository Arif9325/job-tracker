# Job Application Tracker

🔗 **[Live demo](https://job-tracker-xi-lovat.vercel.app)** · **[API docs (Swagger)](https://job-tracker-api-2026-hsfqerabc7h5f3ce.westus3-01.azurewebsites.net/swagger)**

A full-stack app for tracking job applications — company, role, status,
dates, notes, and follow-up reminders — with authentication (including
login rate-limiting), pagination, an archive instead of permanent
delete, a light/dark theme, and a live stats dashboard.

## Why

Job hunting generates a lot of scattered state: which companies you've
applied to, where each one stands, and when to follow up. This replaces
a spreadsheet with a proper app, and doubles as a demonstration of a
complete full-stack workflow: REST API, database, authentication,
tested business logic, and a typed frontend.

## Stack

- **Backend**: ASP.NET Core 8 Web API, Entity Framework Core, SQLite, JWT authentication
- **Frontend**: React + TypeScript (Vite), no UI framework — plain CSS
- **Testing**: xUnit — unit tests against the service layer (EF Core InMemory) AND integration tests that spin up the real API in-process (`WebApplicationFactory`) and hit it over real HTTP
- **CI**: GitHub Actions — builds and tests the backend (both test projects) and frontend on every push/PR, and deploys the backend to Azure on push to `main`

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
- **Login rate-limiting**: after 5 failed login attempts for the same
  email within the lockout window, further attempts are rejected (HTTP
  429) for 15 minutes, regardless of whether the password given is now
  correct. This is per-email, not per-IP, since IPs are trivial to
  rotate but a target account isn't. Tracked in-memory
  (`LoginAttemptTracker`) — resets on app restart and isn't shared
  across multiple server instances; a production system under real
  traffic would back this with Redis or a database table instead.
- **Archive instead of delete**: the everyday "remove this" action sets
  `IsArchived = true` rather than deleting the row. Permanent deletion
  is only permitted on an application that's already archived — a
  deliberate extra step so data isn't lost to a misclick.
- **Remember me**: checked → 30-day token stored in `localStorage`
  (survives closing the browser). Unchecked → 12-hour token stored in
  `sessionStorage` (cleared when the tab/browser closes). Both the token's
  lifetime and where it's stored have to agree, or one setting would
  silently undercut the other.

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

**If you had a `jobtracker.db` from before the archive/follow-up-date
features were added**, delete it once (`rm backend/JobTracker.Api/jobtracker.db`)
before running again. This project uses `EnsureCreated()` rather than
versioned EF Core migrations (see "Known tradeoffs" below), so an
existing database file won't automatically pick up new columns — it'll
just be silently missing them. Deleting it lets `EnsureCreated()`
rebuild the schema from scratch on next run.

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

This runs both test projects:
- **`JobTracker.Api.Tests`** (unit tests) — the service layer in
  isolation: pagination math, archive/restore behavior, stats
  (including overdue follow-ups), and the login lockout logic with a
  controllable fake clock (`FakeTimeProvider`) so the 15-minute lockout
  expiry is tested deterministically, not by actually waiting 15
  minutes.
- **`JobTracker.Api.IntegrationTests`** — spins up the real app
  in-process (`WebApplicationFactory<Program>`) against an isolated
  in-memory database and hits real HTTP endpoints: full
  create/list/update/archive/restore/delete flows, confirming
  unauthenticated requests are rejected, confirming one user's data is
  invisible to another user over real HTTP (not just at the service
  layer), and confirming the login lockout actually engages after 5
  real failed HTTP login attempts.

## API endpoints

| Method | Route | Auth required | Description |
|--------|-------|:---:|-------------|
| POST | `/api/auth/register` | | Create an account |
| POST | `/api/auth/login` | | Log in, get a JWT (rate-limited after 5 failures) |
| GET | `/api/applications?page=&pageSize=&includeArchived=` | ✓ | Paginated list of the logged-in user's applications |
| GET | `/api/applications/{id}` | ✓ | Get one application |
| POST | `/api/applications` | ✓ | Create an application |
| PUT | `/api/applications/{id}` | ✓ | Update an application |
| POST | `/api/applications/{id}/archive` | ✓ | Archive (soft delete) an application |
| POST | `/api/applications/{id}/restore` | ✓ | Restore an archived application |
| DELETE | `/api/applications/{id}` | ✓ | Permanently delete — only succeeds if already archived |
| GET | `/api/applications/stats` | ✓ | Counts by status, plus overdue follow-ups |

## Known tradeoffs / possible extensions

- **`EnsureCreated()` instead of EF migrations** — simpler to run for a
  project this size, but can't evolve an existing schema without losing
  data. A production app would use `dotnet ef migrations add ...`
  instead, precisely to support schema changes over time.
- **SQLite instead of a full database server** — no separate service to
  install locally; would swap the EF Core provider for PostgreSQL/SQL
  Server under real concurrent load.
- No password reset flow, no email verification.
- Status filter and sort apply only within the current page of results,
  since pagination happens server-side — moving them server-side too
  would matter if this needed to scale well past personal-use volume.
- Possible next features: email/browser notifications for overdue
  follow-ups (rather than just a dashboard count), a Kanban-style
  drag-and-drop board, CSV export, search by company/role name.
