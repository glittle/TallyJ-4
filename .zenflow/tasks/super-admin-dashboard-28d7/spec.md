# Technical Specification: Super Admin Dashboard

## Difficulty: Hard

Complex feature spanning backend config, authorization, new API endpoints, new service layer, new Vue page, store, sidebar/header integration, and frontend routing. Touches many layers of the stack.

---

## Technical Context

- **Backend**: ASP.NET Core Web API (.NET 10), EF Core, JWT auth (httpOnly cookies), Identity
- **Frontend**: Vue 3 (Composition API), TypeScript, Pinia, Element Plus, Vue Router, vue-i18n
- **Auth flow**: JWT in httpOnly cookies, user info in readable cookies (`user_email`, `user_name`, `auth_method`). `JwtTokenService` creates tokens with claims: `sub`, `email`, `authMethod`, `jti`.
- **Existing dashboard**: `DashboardPage.vue` shows stats from the user's own elections via `electionStore`. `DashboardController.cs` / `DashboardService.cs` provide summary/list endpoints.

---

## Implementation Approach

### 1. Super Admin Configuration (Backend)

Add a `SuperAdmin` section to `appsettings.json` containing an array of email addresses. This is the sole mechanism for granting SA access.

```json
{
  "SuperAdmin": {
    "Emails": ["admin@tallyj.test"]
  }
}
```

Create a `SuperAdminSettings` options class bound via `IOptions<SuperAdminSettings>`. Register in `Program.cs` with `services.Configure<SuperAdminSettings>(...)`.

### 2. Super Admin Authorization (Backend)

Create a custom authorization policy `"SuperAdmin"` with a corresponding `SuperAdminRequirement` and `SuperAdminHandler`. The handler:
1. Extracts the user's email from JWT claims (`email` claim).
2. Checks if that email appears in `SuperAdminSettings.Emails` (case-insensitive).
3. Succeeds or fails accordingly.

Register in `Program.cs`:
- `options.AddPolicy("SuperAdmin", policy => policy.Requirements.Add(new SuperAdminRequirement()));`
- `services.AddScoped<IAuthorizationHandler, SuperAdminHandler>();`

### 3. Super Admin API Endpoint to Check SA Status

Add a lightweight endpoint so the frontend can determine if the logged-in user is a super admin:

`GET /api/auth/me` or similar — this may already return user info. Alternative: add a dedicated `GET /api/superadmin/status` that returns `{ isSuperAdmin: true/false }`. The simplest approach is to add an `isSuperAdmin` field to the existing auth response (login, token refresh) or expose it through a new lightweight endpoint.

**Chosen approach**: Add `GET /api/superadmin/check` endpoint (protected by `[Authorize]` only) that returns `{ isSuperAdmin: bool }`. This keeps SA logic self-contained.

### 4. Super Admin Dashboard API (Backend)

New controller: `SuperAdminController.cs` at `api/superadmin`, all endpoints protected by `[Authorize(Policy = "SuperAdmin")]`.

**Endpoints:**

| Method | Route | Description | Response |
|--------|-------|-------------|----------|
| `GET` | `/api/superadmin/check` | Check if current user is SA | `ApiResponse<SuperAdminCheckDto>` |
| `GET` | `/api/superadmin/dashboard/summary` | Summary stats (counts by status) | `ApiResponse<SuperAdminSummaryDto>` |
| `GET` | `/api/superadmin/dashboard/elections` | Paginated/filterable election list | `ApiResponse<PaginatedResponse<SuperAdminElectionDto>>` |
| `GET` | `/api/superadmin/dashboard/elections/{guid}` | Election detail for side panel | `ApiResponse<SuperAdminElectionDetailDto>` |

**Note**: The `check` endpoint uses `[Authorize]` only (not the SuperAdmin policy), so any authenticated user can call it. All other SA endpoints use the `SuperAdmin` policy.

### 5. Super Admin Service (Backend)

New service: `ISuperAdminService` / `SuperAdminService`.

- `GetSummaryAsync()` → queries all elections, returns counts grouped by status (open, upcoming, completed, etc.)
- `GetElectionsAsync(filter, sort, page, pageSize)` → paginated election list with filter/sort support. Each election includes: name, convenor, status, # people, # ballots, date, election type.
- `GetElectionDetailAsync(Guid electionGuid)` → detailed info including owner names/emails (via `JoinElectionUsers` → `AspNetUsers`).

### 6. DTOs (Backend)

New DTOs in `backend/DTOs/SuperAdmin/`:

- **`SuperAdminCheckDto`**: `{ isSuperAdmin: bool }`
- **`SuperAdminSummaryDto`**: `{ totalElections, openElections, upcomingElections, completedElections, archivedElections }`
- **`SuperAdminElectionDto`**: `{ electionGuid, name, convenor, dateOfElection, tallyStatus, electionType, voterCount, ballotCount, locationCount, ownerEmail }`
- **`SuperAdminElectionDetailDto`**: extends above + `{ owners: [{ email, displayName, role }], numberToElect, electionMode, percentComplete, createdDate }`
- **`SuperAdminElectionFilterDto`**: `{ search?, status?, electionType?, sortBy?, sortDirection?, page, pageSize }`

### 7. Frontend: Super Admin Store

New Pinia store: `frontend/src/stores/superAdminStore.ts`

- State: `isSuperAdmin`, `summary`, `elections`, `selectedElection`, `loading`, `filter`
- Actions: `checkSuperAdminStatus()`, `fetchSummary()`, `fetchElections(filter)`, `fetchElectionDetail(guid)`
- Called once after login (or on app init) to set `isSuperAdmin`.

### 8. Frontend: Super Admin Service

New service: `frontend/src/services/superAdminService.ts`

Wraps API calls to `/api/superadmin/*` endpoints using the existing Axios-based `api.ts` (since these are new endpoints not in the generated SDK initially).

### 9. Frontend: Super Admin Dashboard Page

New page: `frontend/src/pages/SuperAdminDashboardPage.vue`

Structure:
- **Summary cards** at top: Total elections, Open, Upcoming, Completed (similar stat card layout to existing dashboard)
- **Filterable election list** below: search input, status filter dropdown, sortable table columns
- **Table columns**: Election Name, Convenor, Date, Status, Type, # People, # Ballots, Owner Email
- **Row click / detail button**: Opens an `el-drawer` (side panel) with election detail including owner info
- **No edit/modify capability** — read-only view

### 10. Frontend: Routing & Navigation

- Add route: `/super-admin` → `SuperAdminDashboardPage.vue` (under MainLayout, `requiresAuth: true`)
- Add route guard: redirect non-SA users away from `/super-admin`
- **Sidebar**: Add "Super Admin" menu item in `AppSidebar.vue`, conditionally shown when `isSuperAdmin` is true
- **Alternative**: Add a button/icon in `AppHeader.vue` (e.g., a shield icon) — sidebar is simpler and consistent

### 11. Frontend: Auth Integration

- On login / app init, call `GET /api/superadmin/check` and store result in `superAdminStore.isSuperAdmin`
- Add `is_super_admin` cookie (non-httpOnly) set by backend during login, OR call the check endpoint on app startup. **Chosen**: call the check endpoint on app startup (avoids modifying auth cookie flow).
- `secureTokenService` or `authStore` can trigger the check.

### 12. Localization

Add i18n keys in `frontend/src/locales/en/` (new file `superAdmin.json` or extend `dashboard.json`).

---

## Source Code Changes

### New Files

| File | Purpose |
|------|---------|
| `backend/DTOs/SuperAdmin/SuperAdminCheckDto.cs` | SA check response DTO |
| `backend/DTOs/SuperAdmin/SuperAdminSummaryDto.cs` | SA dashboard summary DTO |
| `backend/DTOs/SuperAdmin/SuperAdminElectionDto.cs` | SA election list item DTO |
| `backend/DTOs/SuperAdmin/SuperAdminElectionDetailDto.cs` | SA election detail DTO |
| `backend/DTOs/SuperAdmin/SuperAdminElectionFilterDto.cs` | SA filter/pagination DTO |
| `backend/Authorization/SuperAdminRequirement.cs` | Authorization requirement |
| `backend/Authorization/SuperAdminHandler.cs` | Authorization handler |
| `backend/Services/ISuperAdminService.cs` | Service interface |
| `backend/Services/SuperAdminService.cs` | Service implementation |
| `backend/Controllers/SuperAdminController.cs` | API controller |
| `frontend/src/pages/SuperAdminDashboardPage.vue` | SA dashboard page |
| `frontend/src/stores/superAdminStore.ts` | SA Pinia store |
| `frontend/src/services/superAdminService.ts` | SA API service |
| `frontend/src/locales/en/superAdmin.json` | English i18n keys |
| `frontend/src/locales/fr/superAdmin.json` | French i18n keys (stubs) |

### Modified Files

| File | Change |
|------|--------|
| `backend/appsettings.json` | Add `SuperAdmin.Emails` section |
| `backend/appsettings.Development.json` | Add `SuperAdmin.Emails` with dev admin |
| `backend/Program.cs` | Register `SuperAdminSettings`, authorization policy, handler, service |
| `frontend/src/router/router.ts` | Add `/super-admin` route with guard |
| `frontend/src/components/AppSidebar.vue` | Add conditional SA menu item |
| `frontend/src/locales/en/nav.json` | Add `nav.superAdmin` key |
| `frontend/src/locales/fr/nav.json` | Add `nav.superAdmin` key |
| `frontend/src/locales/index.ts` | Import new superAdmin locale files |

---

## Data Model / API / Interface Changes

### No database changes required
SA status is determined by config, not stored in the database. Election queries use existing entities.

### New API Endpoints

All under `/api/superadmin`:

1. **`GET /api/superadmin/check`** — `[Authorize]` (any authenticated user)
   - Response: `ApiResponse<SuperAdminCheckDto>` → `{ data: { isSuperAdmin: bool } }`

2. **`GET /api/superadmin/dashboard/summary`** — `[Authorize(Policy = "SuperAdmin")]`
   - Response: `ApiResponse<SuperAdminSummaryDto>`

3. **`GET /api/superadmin/dashboard/elections`** — `[Authorize(Policy = "SuperAdmin")]`
   - Query params: `search`, `status`, `electionType`, `sortBy` (default: `dateOfElection`), `sortDirection` (default: `desc`), `page` (default: 1), `pageSize` (default: 25)
   - Response: `ApiResponse<PaginatedResponse<SuperAdminElectionDto>>`

4. **`GET /api/superadmin/dashboard/elections/{guid}`** — `[Authorize(Policy = "SuperAdmin")]`
   - Response: `ApiResponse<SuperAdminElectionDetailDto>`

### New Configuration

```json
"SuperAdmin": {
  "Emails": []
}
```

---

## Verification Approach

1. **Backend unit tests**: Test `SuperAdminHandler` with mock `IOptions<SuperAdminSettings>` — verify SA users pass, non-SA users fail.
2. **Backend unit tests**: Test `SuperAdminService` methods with InMemory DB.
3. **Backend integration**: Test controller endpoints return 403 for non-SA users, 200 for SA users.
4. **Frontend**: Verify sidebar shows SA link only for SA users.
5. **Frontend**: Verify route guard blocks non-SA users from `/super-admin`.
6. **Linting/TypeCheck**: Run `npm run lint` (if available) and `npx vue-tsc --noEmit` for frontend, `dotnet build` for backend.
7. **Manual**: Login as `admin@tallyj.test` (configured as SA in dev), verify dashboard loads with all elections.
