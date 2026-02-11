---
description: Architecture details and patterns discovered through debugging
alwaysApply: true
---

# Architecture Details

## Authentication & Identity

- **Identity provider**: ASP.NET Core Identity with `AppUser : IdentityUser` (string-based `Id` that stores GUIDs)
- **JWT generation**: `backend/TallyJ4.Application/Services/Auth/JwtTokenService.cs` uses `JwtSecurityTokenHandler`
- **JWT claims**: User ID is stored in the `sub` claim (`JwtRegisteredClaimNames.Sub`) with value from `AppUser.Id`
- **.NET 10 claim mapping**: `JsonWebTokenHandler` is the default validator (not `JwtSecurityTokenHandler`). It does NOT map `sub` to `ClaimTypes.NameIdentifier`. The claim stays as `"sub"`. Code that reads the user ID must check both:
  ```csharp
  User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value
  ```
- **JoinElectionUser.UserId**: `Guid` type. Parsed from `AppUser.Id` (which is a GUID-formatted string).
- **Auth config**: `backend/Program.cs` — `AddIdentity` is called first, then `AddAuthentication` overrides default scheme to `JwtBearerDefaults.AuthenticationScheme`.

## API Response Patterns

Two different response wrappers are used — be careful which one an endpoint returns:

1. **`ApiResponse<T>`** — wraps data in a `.data` property. Used by endpoints like `GetElection`, `CreateElection`, `UpdateElection`.
   - Frontend access: `response.data?.data`
2. **`PaginatedResponse<T>`** — has `.items` directly at root level (no `.data` wrapper). Used by `GetElections`.
   - Frontend access: `response.data?.items`

When writing frontend service code, check the controller's return type to determine the correct access pattern.

## Generated API Client

- **Location**: `frontend/src/api/gen/configService/`
- **Files**: `sdk.gen.ts` (call functions), `types.gen.ts` (TypeScript types), `transformers.gen.ts` (response transformers)
- **Generated from**: OpenAPI spec at `frontend/openapi/tallyj.json` (auto-generated on dev startup via `Program.cs`)
- **Response shape**: SDK calls return `{ data: <ResponseBodyType> }` — so `response.data` is the deserialized HTTP body.

## Frontend Service Layer

- **Location**: `frontend/src/services/`
- **Pattern**: Each service wraps generated SDK calls and transforms data for store consumption.
- **Stores**: Pinia stores in `frontend/src/stores/` call services and manage state.
- **Flow**: `Vue component` -> `Pinia store` -> `service` -> `generated SDK` -> backend API

## Database Seeding

- **Seeder**: `backend/EF/Data/DbSeeder.cs`
- **Idempotent**: Skips if `Elections` table already has data.
- **Test users**: `admin@tallyj.test`, `teller@tallyj.test`, `voter@tallyj.test` (password: `TestPass123!`)
- **GUID generation**: Uses MD5 hash of seed strings for deterministic GUIDs (`CreateGuid` method).
- **JoinElectionUser records**: Created per user per election. Links users to elections with roles (Owner, Teller, etc.).

## Key Backend Files

| Purpose | File |
|---------|------|
| App startup & middleware | `backend/Program.cs` |
| JWT token creation | `backend/TallyJ4.Application/Services/Auth/JwtTokenService.cs` |
| Election CRUD | `backend/Controllers/ElectionsController.cs` |
| Election business logic | `backend/Services/ElectionService.cs` |
| User-election link entity | `backend/TallyJ4.Domain/Entities/JoinElectionUser.cs` |
| Identity user model | `backend/TallyJ4.Domain/Identity/AppUser.cs` |
| DB context | `backend/TallyJ4.Domain/Context/MainDbContext.cs` |
| Response models | `backend/Models/PaginatedResponse.cs`, `backend/Models/ApiResponse.cs` |

## Key Frontend Files

| Purpose | File |
|---------|------|
| Election service | `frontend/src/services/electionService.ts` |
| Election store (Pinia) | `frontend/src/stores/electionStore.ts` |
| Election list page | `frontend/src/pages/elections/ElectionListPage.vue` |
| Auth store | `frontend/src/stores/authStore.ts` |
| Router | `frontend/src/router/router.ts` |
| Generated SDK | `frontend/src/api/gen/configService/sdk.gen.ts` |
