# TallyJ 4

TallyJ 4 is a full-stack election management and ballot tallying system for Bahá’í communities. The current repository contains a .NET 10 backend API, a Vue 3 + Vite frontend, shared localization assets, and xUnit/Vitest test suites.

## Repository layout

- `backend/` - ASP.NET Core Web API and web host
- `Backend.Application/` - application services and shared business logic
- `Backend.Domain/` - entities, DbContext, identity models, and domain contracts
- `Backend.Tests/` - xUnit unit and integration tests
- `frontend/` - Vue 3 + TypeScript SPA
- `docs/` - deployment documentation only
- `.zenflow/tasks/` - historical planning and reverse-engineering artifacts; useful for background, but not the source of truth for current commands or runtime configuration

## Local development

### Prerequisites

- .NET SDK 10
- Node.js 20+
- SQL Server Express, SQL Server Developer Edition, or Docker SQL Server

### Start the backend

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

Default development URLs:

- API: `http://localhost:5016`
- Swagger: `http://localhost:5016/swagger`
- HTTPS profile: `https://localhost:7262`

### Start the frontend

```bash
cd frontend
npm install
npm run dev
```

Default frontend URL:

- App: `http://localhost:8095`

The frontend reads `VITE_API_URL` from `.env.development` or falls back to `http://localhost:5016`.

## Seeded development accounts

With the default development backend configuration, the database is seeded on startup.

| Email | Password | Role |
| --- | --- | --- |
| `admin@tallyj.test` | `TestPass123!` | Admin |
| `teller@tallyj.test` | `TestPass123!` | Teller |
| `voter@tallyj.test` | `TestPass123!` | Voter |

## Common commands

### Backend

```bash
cd backend
dotnet build
```

```bash
cd backend
dotnet ef migrations list
```

```bash
cd ..
dotnet test Backend.Tests/Backend.Tests.csproj
```

### Frontend

```bash
cd frontend
npm run check
```

```bash
cd frontend
npm run test:run
```

```bash
cd frontend
npm run validate:i18n
```

Use `npm run start` when you need to regenerate the OpenAPI client before starting Vite.

## Documentation map

These are the canonical docs to keep current:

- `backend/README.md` - backend setup and local configuration
- `frontend/README.md` - frontend development workflow
- `docs/DEPLOYMENT.md` - deployment notes and production checklist
- `DEPLOYING_LOCALLY.md` - local deployment setup (non-development)
- `E2E_TESTING_GUIDE.md` - smoke testing and validation workflow
- `AGENTS.md` - repo-specific instructions for AI agents and contributors

## API documentation

The source of truth for API routes and schemas is the running Swagger/OpenAPI document exposed by the backend at `/swagger`.

Do not rely on older hand-written endpoint summaries when the running API disagrees with them.
