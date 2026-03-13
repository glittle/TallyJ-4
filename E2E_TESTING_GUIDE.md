# Validation and smoke-testing guide

Use this guide after meaningful backend or frontend changes.

## Local test environment

### Start backend

```bash
cd backend
dotnet run
```

Expected local API URL: `http://localhost:5016`

### Start frontend

```bash
cd frontend
npm run dev
```

Expected local app URL: `http://localhost:8095`

## Automated checks

### Backend tests

```bash
dotnet test Backend.Tests/Backend.Tests.csproj
```

### Frontend typecheck and lint

```bash
cd frontend
npm run check
```

### Frontend unit tests

```bash
cd frontend
npm run test:run
```

### Optional API smoke script

The repo includes `test-e2e.ps1` for API-style smoke testing.

```powershell
powershell -ExecutionPolicy Bypass -File test-e2e.ps1
```

Use it as a convenience script, not as a replacement for targeted testing of the feature you changed.

## Manual smoke checklist

### Authentication

- user can log in
- protected routes redirect when unauthenticated
- logout clears access and returns to a public route

### Elections

- election list loads
- create or edit election works
- election detail page loads without console errors

### People and ballots

- people list loads for an election
- add or edit a person works
- ballot list loads for an election
- ballot entry or ballot import flow still works if your change touched those areas

### Results and reporting

- tally page loads
- results page loads
- reporting and presentation pages still render if the change touched results or reporting

### Feature-specific areas

Test these only when relevant to your change:

- front desk check-in workflow
- teller assignment and teller join flow
- location management
- online voting authentication and ballot submission
- public display pages
- audit log views

## Seeded development accounts

| Email | Password | Role |
| --- | --- | --- |
| `admin@tallyj.test` | `TestPass123!` | Admin |
| `teller@tallyj.test` | `TestPass123!` | Teller |
| `voter@tallyj.test` | `TestPass123!` | Voter |

## When to expand testing

Do more than the smoke checklist when you change:

- authentication or authorization
- database schema or seeding
- generated API client contracts
- SignalR events or group naming
- tallying, results, or ballot validation logic
- localization loading or translation keys

## Testing notes

- Swagger is the quickest way to confirm current backend routes and request/response shapes
- Prefer feature-focused validation over large generic test passes
- If a test script or guide disagrees with the running app, update the guide to match the codebase
