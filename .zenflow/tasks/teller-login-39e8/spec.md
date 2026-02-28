# Technical Specification: Teller Login

## Difficulty: Hard

Multiple cross-cutting changes across backend and frontend: new API endpoint for teller access-code auth, changes to existing election DTOs/APIs, new frontend route for URL-based teller join, dashboard/detail page UI changes, QR code generation, and security considerations around access-code validation.

## Technical Context

- **Backend**: .NET 10, ASP.NET Core Web API, EF Core, AutoMapper, JWT auth
- **Frontend**: Vue 3 (Composition API), TypeScript, Pinia, Element Plus, Vite
- **No QR code library** currently in `package.json` — need to add one (e.g., `qrcode.vue` or `qrcode`)

## Key Domain Concepts

- **Main/Head tellers**: Login with email+password or Google OAuth (existing flow)
- **Assistant tellers**: Login with an access code (the `ElectionPasscode` on the Election entity). They get limited access to a specific election.
- `Election.ListedForPublicAsOf` (DateTime?, DB column `[ListedForPublicAsOf]`): Despite its name, this field solely indicates whether the election is **open for assistant tellers to join**. Non-null = open (timestamp of when it was opened). Null = closed.
- `Election.ElectionPasscode` (string?, max 50): The access code shared with assistant tellers.
- The access code can only be used if:
  1. `ListedForPublicAsOf` is non-null (election is open for tellers)
  2. A main teller has been logged in within the last hour (future enforcement — may defer to a later task or implement with a simple check)

## Implementation Approach

### 1. Backend: Add `isTellerAccessOpen` to Election APIs

**Goal**: The `getElections` (summary list) and `getElection` (detail) APIs should include whether the election is open for tellers, so the frontend doesn't need a separate teller-count API call.

**Changes**:
- `ElectionSummaryDto`: Add `bool IsTellerAccessOpen` (computed from `ListedForPublicAsOf != null`), `bool IsOnlineVotingEnabled` (computed from `OnlineWhenOpen`/`OnlineWhenClose`), and `bool? ShowAsTest`.
- `ElectionDto`: Already has `ElectionPasscode` and online voting fields. Add `bool IsTellerAccessOpen` and `DateTime? TellerAccessOpenedAt` (mapped from `ListedForPublicAsOf`).
- `ElectionService.GetElectionsAsync`: Populate `IsTellerAccessOpen` in the projection.
- `ElectionService.GetElectionByGuidAsync`: Populate `IsTellerAccessOpen` and `TellerAccessOpenedAt` from the entity.
- `ElectionProfile`: Map `ListedForPublicAsOf` → `TellerAccessOpenedAt`, and add custom mapping for `IsTellerAccessOpen`.

### 2. Backend: Teller Access Code Login Endpoint

**Goal**: Public endpoint that validates an election's access code and returns a limited-scope JWT or session token for assistant tellers.

**New endpoint**: `POST /api/auth/teller-login`
- Request body: `{ electionGuid: Guid, accessCode: string }`
- Validates:
  - Election exists
  - `ListedForPublicAsOf` is not null (election open for tellers)
  - `ElectionPasscode` matches `accessCode`
  - A main teller (JoinElectionUser with admin/owner role) has logged in within the last hour (stretch goal — may require tracking last login time; defer if not easily available)
- On success: Returns a limited JWT with claims: `electionGuid`, `role=Teller`, `isTeller=true`
- The teller JWT should grant access to teller-scoped endpoints only (ballot entry, front desk, teller list for that election)

**New file**: `Backend.Application/DTOs/Auth/TellerLoginRequest.cs`
**Modified**: `AuthController.cs` — add `[AllowAnonymous] [HttpPost("teller-login")]`

### 3. Backend: Toggle Teller Access

**Goal**: Allow main tellers to open/close teller access for an election.

**New endpoint**: `PUT /api/elections/{guid}/teller-access`
- Request body: `{ isOpen: bool }`
- Sets `ListedForPublicAsOf = DateTime.UtcNow` when opening, `null` when closing
- Requires `[Authorize]` with election access

**Modified**: `ElectionsController.cs`, `ElectionService.cs`, `IElectionService.cs`

### 4. Frontend: Update Types & Services

**Modified files**:
- `frontend/src/types/Election.ts`: Add `isTellerAccessOpen?: boolean`, `tellerAccessOpenedAt?: string`, `isOnlineVotingEnabled?: boolean` to `ElectionDto` and `ElectionSummaryDto`
- `frontend/src/services/electionService.ts`: Map new fields in `getAll()`
- `frontend/src/services/authService.ts`: Add `tellerLogin(electionGuid, accessCode)` method
- `frontend/src/services/tellerService.ts`: Add `toggleTellerAccess(electionGuid, isOpen)` method

### 5. Frontend: Dashboard — Remove Teller Count, Show Teller Access Status

**Goal**: Remove per-election teller count API calls. Instead use `isTellerAccessOpen` from the election summary.

**Modified**: `frontend/src/pages/DashboardPage.vue`
- Remove `loadTellerCounts()` function and the N+1 teller API calls
- Remove `ElectionWithDetails` interface (no longer needed)
- Use `isTellerAccessOpen` from the election data directly
- Update the "Tellers" column to show open/closed based on `isTellerAccessOpen`
- Update the expanded row teller section similarly

### 6. Frontend: Election Detail Page — Teller Information Section

**Goal**: After Quick Actions, add a "Teller Access" section showing:
- Whether teller access is open (with toggle)
- The access code (`electionPasscode`)
- A shareable URL: `{window.location.origin}/teller-join/{electionGuid}?code={accessCode}`
- A QR code encoding that URL

**Modified**: `frontend/src/pages/elections/ElectionDetailPage.vue`
- Add new `<el-card>` section after the Quick Actions card
- Toggle to open/close teller access (calls `PUT /api/elections/{guid}/teller-access`)
- Display access code
- Generate shareable link
- Display QR code (new npm dependency: `qrcode` — use it to generate a data URL)
- Also show online voting status (open/closed based on dates)

**New dependency**: `npm install qrcode` + `@types/qrcode` (or use `qrcode.vue`)

### 7. Frontend: Teller Join Route & Page

**Goal**: A public route that accepts a URL like `/teller-join/{electionGuid}?code={accessCode}` and auto-logs in the assistant teller.

**New file**: `frontend/src/pages/TellerJoinPage.vue`
- Reads `electionGuid` from route params and `code` from query string
- Calls `POST /api/auth/teller-login` with these values
- On success: stores the teller JWT, redirects to the election's front desk or ballot entry page
- On failure: shows error message (election not open, invalid code, etc.)

**Modified**: `frontend/src/router/router.ts`
- Add public route: `{ path: 'teller-join/:electionGuid', component: TellerJoinPage, meta: { requiresAuth: false } }`

### 8. Frontend: Localization

**Modified**:
- `frontend/src/locales/en/elections.json`: Add keys for teller access section
- `frontend/src/locales/en/dashboard.json`: Update teller-related keys
- `frontend/src/locales/fr/elections.json`: Add French translations
- `frontend/src/locales/fr/dashboard.json`: Update

## Files to Create

| File | Purpose |
|------|---------|
| `frontend/src/pages/TellerJoinPage.vue` | Public page for URL-based teller join |

## Files to Modify

| File | Purpose |
|------|---------|
| `Backend.Domain/Entities/Election.cs` | No changes needed (ListedForPublicAsOf already exists) |
| `backend/DTOs/Elections/ElectionSummaryDto.cs` | Add `IsTellerAccessOpen`, `IsOnlineVotingEnabled`, `ShowAsTest` |
| `backend/DTOs/Elections/ElectionDto.cs` | Add `IsTellerAccessOpen`, `TellerAccessOpenedAt` |
| `backend/Mappings/ElectionProfile.cs` | Map `ListedForPublicAsOf` → new DTO fields |
| `backend/Services/ElectionService.cs` | Populate new fields, add toggle method |
| `backend/Services/IElectionService.cs` | Add toggle method signature |
| `backend/Controllers/ElectionsController.cs` | Add teller-access toggle endpoint |
| `backend/Controllers/AuthController.cs` | Add teller-login endpoint |
| `frontend/src/types/Election.ts` | Add new fields to DTOs |
| `frontend/src/services/electionService.ts` | Map new fields |
| `frontend/src/services/authService.ts` | Add `tellerLogin()` |
| `frontend/src/services/tellerService.ts` | Add `toggleTellerAccess()` |
| `frontend/src/stores/electionStore.ts` | Add toggle action |
| `frontend/src/pages/DashboardPage.vue` | Remove teller count calls, use `isTellerAccessOpen` |
| `frontend/src/pages/elections/ElectionDetailPage.vue` | Add teller info section with QR code |
| `frontend/src/router/router.ts` | Add teller-join route |
| `frontend/src/locales/en/elections.json` | New i18n keys |
| `frontend/src/locales/en/dashboard.json` | Update teller keys |
| `frontend/src/locales/fr/elections.json` | French translations |
| `frontend/src/locales/fr/dashboard.json` | French translations |
| `frontend/package.json` | Add `qrcode` dependency |

## Verification

- `cd backend && dotnet build` — ensure backend compiles
- `cd Backend.Tests && dotnet test` — ensure existing tests pass
- `cd frontend && npx vue-tsc --noEmit` — type check
- `cd frontend && npm run test:run` — frontend tests
- Manual: visit `/teller-join/{guid}?code={code}` and verify it processes the login
- Manual: check dashboard shows teller access open/closed correctly
- Manual: check election detail page shows teller info section with QR code
