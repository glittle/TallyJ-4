# Copilot instructions for TallyJ 4

## Start here

Use these files as the primary sources of current guidance:

- `README.md`
- `backend/SETUP.md`
- `frontend/README.md`
- `docs/DEPLOYMENT.md`
- `AGENTS.md`

Do not treat older implementation summaries or phase reports as the source of truth when the codebase or the canonical docs disagree.

## Project overview

TallyJ 4 is a .NET 10 + Vue 3 election management system.

- backend: ASP.NET Core API
- frontend: Vue 3, TypeScript, Vite, Pinia, Element Plus
- tests: xUnit and Vitest

## Key repo conventions

### Frontend

- use Composition API with `<script setup lang="ts">`
- Vue file order is script, template, style
- use `<style lang="less">`
- do not use `<style scoped>`
- all user-facing strings should use `$t()`
- prefer existing stores and services over ad-hoc API calls in components

### Backend

- use DTOs for API contracts
- use AutoMapper and FluentValidation
- keep business logic in services
- most endpoints require JWT auth

## Important implementation notes

- JWT user IDs may need to be read from `sub`, not only `ClaimTypes.NameIdentifier`
- frontend services must account for both `ApiResponse<T>` and `PaginatedResponse<T>` response shapes
- election SignalR groups should use `election-{electionGuid}`

## Validation commands

### Backend

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

Run `npm run validate:i18n` after locale changes.

## Local defaults

- backend: `http://localhost:5016`
- frontend: `http://localhost:8095`
- Swagger: `http://localhost:5016/swagger`

## Windows CMD file operations

When removing repo files from the Windows CMD shell, use repo-relative paths from the workspace root.

- single file: `if exist "docs\OLD.md" del /q "docs\OLD.md"`
- multiple files: `for %f in ("API_DOCUMENTATION.md" "docs\ADMIN_GUIDE.md" "backend\API_EXAMPLES.md") do @if exist %f del /q %f`
- verify from the same worktree context with `if exist ...` and `git status --short`

## Documentation policy

If a change affects developer workflow, setup, deployment, or validation, update one of the canonical docs instead of creating a new summary file.
