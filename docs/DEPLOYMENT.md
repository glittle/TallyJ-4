# Deployment guide

This is the canonical deployment document for the current repository.

## Supported deployment shape

TallyJ 4 is deployed as three pieces:

1. SQL Server database
2. ASP.NET Core backend API
3. Static frontend build served by a web server

## Pre-deployment checklist

### Backend

- `dotnet build` passes for `backend/Backend.csproj`
- database migrations are reviewed and ready to apply
- production secrets are stored outside source control
- CORS and frontend base URL are configured for the target domain
- optional integrations are configured only if needed

### Frontend

- `npm run check` passes
- `npm run test:run` passes
- production environment variables are set
- the built app points at the production backend URL

### Infrastructure

- SQL Server is reachable from the backend
- TLS termination is in place
- logs are collected
- database backup and restore procedures are tested

## Required backend configuration

The backend reads standard ASP.NET Core configuration sources. At minimum, set:

- `ConnectionStrings__TallyJ4`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Frontend__BaseUrl`

Common optional settings:

- `Google__ClientId`
- `Google__ClientSecret`
- `Email__*`
- `Twilio__*`
- `GreenApi__*`
- `Telegram__*`
- `SuperAdmin__Emails`

Prefer environment variables, secret stores, or deployment-platform secret management.

## Required frontend configuration

At minimum, configure:

```env
VITE_API_URL=https://api.example.com
```

Optional variables such as `VITE_SENTRY_DSN` and analytics flags should be set only when those services are active.

## Backend deployment

### Publish

```bash
dotnet publish backend/Backend.csproj -c Release -o backend/publish
```

### Apply migrations

Run migrations against the production database before or during deployment:

```bash
dotnet ef database update --project backend/Backend.csproj
```

### Host behind a reverse proxy

The backend is a standard ASP.NET Core application and can be hosted behind IIS, nginx, or another reverse proxy.

The important requirement is that the externally visible frontend origin and the backend CORS configuration agree.

## Frontend deployment

### Build

```bash
cd frontend
npm ci
npm run build
```

Deploy the contents of `frontend/dist/` to your static web host or reverse-proxied web server.

Your web server must route unknown paths back to `index.html` so Vue Router can handle client-side navigation.

## Smoke tests after deployment

### Backend

- Open Swagger at `/swagger`
- verify login via `POST /api/Auth/login`
- verify an authenticated request such as election listing

### Frontend

- load the landing page
- log in with a valid account
- open an election
- verify at least one CRUD workflow relevant to your deployment

## Docker status

The repository includes Dockerfiles and compose files, but they should be validated before relying on them for production deployment.

At the time of this cleanup, the backend Docker assets still reference legacy `TallyJ4` project filenames instead of the current `Backend` project naming, so treat them as starting points rather than canonical production instructions.

## Operational notes

- The backend currently exposes Swagger, but no dedicated health-check route is documented in the active API host
- Development seeding should remain disabled in production
- The frontend and backend share localization assets during development; production deployments should ensure the backend has access to the required locale files if server-side localization is used

## Keep this file current when

Update this document whenever any of the following changes:

- publish command or project names
- required environment variables
- CORS or frontend base URL behavior
- deployment topology
- Docker and compose support status
