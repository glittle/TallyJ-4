# Backend setup guide

This guide covers the current local-development workflow for the TallyJ 4 backend.

## What lives here

The backend project is the ASP.NET Core API host in `backend/`. It references:

- `Backend.Application/` for application services
- `Backend.Domain/` for entities, DbContext, and identity models
- `Backend.Tests/` for xUnit tests

## Prerequisites

- .NET SDK 10
- SQL Server Express or SQL Server Developer Edition
- Optional: Docker Desktop if you prefer running SQL Server in a container

## Local database options

### Option 1: SQL Server Express

The default development configuration expects:

- Server: `localhost\SQLEXPRESS`
- Database: `TallyJ4Dev`

The default development connection string key is `ConnectionStrings:TallyJ4`.

### Option 2: SQL Server in Docker

Example local SQL Server container:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 --name tallyj4-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

If you use Docker SQL Server, override `ConnectionStrings:TallyJ4` accordingly.

## Recommended configuration approach

Prefer user secrets or environment variables for anything sensitive.

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:TallyJ4" "Server=localhost\\SQLEXPRESS;Database=TallyJ4Dev;Trusted_Connection=True;TrustServerCertificate=True"
```

Optional providers such as Google sign-in, email, SMS, WhatsApp, Telegram, and social auth should also be configured through user secrets or environment variables rather than committed settings files.

## Start the backend

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

Development URLs from `Properties/launchSettings.json`:

- `http://localhost:5016`
- `https://localhost:7262`

Swagger is available at:

- `http://localhost:5016/swagger`

## Development defaults

The checked-in development settings currently assume:

- `Database:SeedOnStartup = true`
- frontend base URL at `http://localhost:8095`
- localization resources loaded from `../frontend/src/locales`

That means a fresh local database is seeded automatically when the backend starts in the Development environment.

## Seeded accounts

| Email | Password | Role |
| --- | --- | --- |
| `admin@tallyj.test` | `TestPass123!` | Admin |
| `teller@tallyj.test` | `TestPass123!` | Teller |
| `voter@tallyj.test` | `TestPass123!` | Voter |

## Useful commands

### Build

```bash
cd backend
dotnet build
```

### Run tests

```bash
cd ..
dotnet test Backend.Tests/Backend.Tests.csproj
```

### EF Core commands

```bash
cd backend
dotnet ef migrations list
```

```bash
cd backend
dotnet ef migrations add <MigrationName>
```

```bash
cd backend
dotnet ef database update
```

### Reset local database

Use the reset scripts under `backend/scripts/` when you need a clean local database.

## Routing notes

The backend uses controller-based routes. A few examples from the current codebase:

- `POST /api/Auth/login`
- `POST /api/Auth/registerAccount`
- `GET /api/Elections/getElections`
- `POST /api/online-voting/requestCode`

Use Swagger for the current route list rather than older handwritten API summaries.

## Auth and claims note

JWT user IDs are stored in the `sub` claim. On .NET 10, code that reads the current user ID should check both `ClaimTypes.NameIdentifier` and `sub`.

## Troubleshooting

### `dotnet ef` cannot connect to SQL Server

- Confirm the SQL Server instance is running
- Recheck `ConnectionStrings:TallyJ4`
- Verify you are using the Development environment when expecting seeded data

### Startup succeeds but the UI cannot log in

- Confirm the frontend is pointing at `http://localhost:5016`
- Confirm the seeded users exist in `TallyJ4Dev`
- Use Swagger to test `POST /api/Auth/login`

### Localization appears missing

- Confirm `frontend/src/locales/` exists
- Confirm `Localization:ResourcesPath` points to `../frontend/src/locales` in development
