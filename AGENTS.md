# TallyJ 4 agent notes

This file contains the repo-specific guidance that should stay current for AI agents and human contributors.

## Canonical docs

Use these as the primary sources of current project guidance:

- `README.md`
- `backend/SETUP.md`
- `frontend/README.md`
- `docs/DEPLOYMENT.md`
- `E2E_TESTING_GUIDE.md`

Treat `.zenflow/tasks/**` and `.zencoder/**` as historical planning and research artifacts unless a task explicitly tells you to consult them.

## Repository shape

- `backend/` - ASP.NET Core API host
- `Backend.Application/` - application services
- `Backend.Domain/` - domain and persistence layer
- `Backend.Tests/` - xUnit tests
- `frontend/` - Vue 3 SPA

## Core conventions

### Frontend

- Vue SFC order is always:
  1. `<script setup lang="ts">`
  2. `<template>`
  3. `<style lang="less">`
- Do not use `<style scoped>`
- Nest style rules under the component root selector
- Use Pinia stores for state management
- Use `$t()` for all user-facing strings
- Standard flow: `component -> store -> service -> generated API client -> backend`

### Backend

- Use DTOs for API input/output
- Use AutoMapper for entity-to-DTO mapping
- Use FluentValidation for request validation
- Keep controller logic thin and use services for business rules
- Default API auth is JWT unless an endpoint is explicitly anonymous

## Important architecture details

### Auth claim lookup

User IDs are stored in JWT `sub` claims. On .NET 10, code that reads the current user ID should check both:

```csharp
User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
```

### API response wrappers

Two response patterns are in active use:

1. `ApiResponse<T>` returns payload in `.data`
2. `PaginatedResponse<T>` returns `.items` at the root

Do not assume every backend response uses the same wrapper.

### SignalR group naming

Election-scoped hub groups should use:

```text
election-{electionGuid}
```

## Validation commands

### Backend

If the build fails with a NuGet restore error like `Value cannot be null. (Parameter 'path1')`, delete the `obj` folders and retry:

```bat
rmdir /s /q backend\obj
dotnet build
```

```bash
dotnet build backend/Backend.csproj
dotnet test Backend.Tests/Backend.Tests.csproj
```

### Frontend

```bash
cd frontend
npm run check
npm run test:run
```

Run `npm run validate:i18n` when you touch locale files.

When adding new user-facing strings, **only add them to the English locale** (`src/locales/en/`). Other languages are updated separately in periodic review cycles — do not add placeholder or machine-translated strings to non-English locales.

## Local development assumptions

- backend default dev URL: `http://localhost:5016`
- frontend default dev URL: `http://localhost:8095`
- backend development config seeds the database on startup
- Swagger is the source of truth for current routes and schemas

## Windows CMD file operations

When deleting repo files from the Windows CMD-based shell tooling in this workspace:

- run from the workspace root and use repo-relative paths
- for a single file, prefer `if exist "docs\OLD.md" del /q "docs\OLD.md"`
- for multiple files, prefer a guarded loop instead of one long `del` command:

```bat
for %f in ("API_DOCUMENTATION.md" "docs\ADMIN_GUIDE.md" "backend\API_EXAMPLES.md") do @if exist %f del /q %f
```

- verify deletions from the same worktree context with `if exist ...` and `git status --short`
- if `git status` does not show a tracked file as deleted, the removal did not affect the active worktree

## Frontend workflow note

For routine local development, prefer `npm run dev`. Only use `npm run build` when you explicitly need a production build or are validating production output.

## Documentation hygiene

When features change, update the canonical docs above instead of adding one-off summary files.

Avoid leaving behind implementation summaries, phase reports, or temporary investigation notes as long-term guidance unless the repository explicitly wants them preserved as archives.
