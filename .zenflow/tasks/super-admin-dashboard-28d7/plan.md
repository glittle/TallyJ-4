# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: c47ce794-77d5-4526-b677-3d98834d4957 -->

Completed. See `spec.md` for full specification. Difficulty: Hard. Key decisions:
- SA determined by email list in `appsettings.json` (`SuperAdmin.Emails`)
- Custom authorization policy + handler for SA endpoints
- New `SuperAdminController` with 4 endpoints (check, summary, elections list, election detail)
- New Vue page at `/super-admin` with conditional sidebar navigation
- No database changes required

---

### [x] Step: Backend Configuration and Authorization
<!-- chat-id: 266efb71-aed5-4eee-a631-e541cd79bebc -->
<!-- Implements: SuperAdmin config, authorization requirement/handler, Program.cs registration -->

- Add `SuperAdmin.Emails` to `appsettings.json` and `appsettings.Development.json`
- Create `SuperAdminSettings` options class
- Create `SuperAdminRequirement.cs` and `SuperAdminHandler.cs` in `backend/Authorization/`
- Register settings, policy, and handler in `Program.cs`
- Run `dotnet build` to verify

---

### [x] Step: Backend DTOs, Service, and Controller
<!-- chat-id: 32608bfa-574d-459d-a597-45eb51e215a4 -->

- Create DTOs in `backend/DTOs/SuperAdmin/`: `SuperAdminCheckDto`, `SuperAdminSummaryDto`, `SuperAdminElectionDto`, `SuperAdminElectionDetailDto`, `SuperAdminElectionFilterDto`
- Create `ISuperAdminService` and `SuperAdminService` in `backend/Services/`
- Create `SuperAdminController` in `backend/Controllers/`
- Register service in `Program.cs`
- Write unit tests for `SuperAdminHandler` and `SuperAdminService`
- Run `dotnet build` and `dotnet test`

---

### [x] Step: Frontend Service, Store, and SA Check Integration
<!-- chat-id: 4e07a9cb-2c61-4431-8bd2-ce835cea7790 -->

- Create `frontend/src/services/superAdminService.ts`
- Create `frontend/src/stores/superAdminStore.ts`
- Integrate SA check on app startup (call check endpoint after auth, store `isSuperAdmin`)
- Add i18n keys (`superAdmin.json` for en/fr, update `nav.json`)
- Update `frontend/src/locales/index.ts` to import new locale files
- Run `npx vue-tsc --noEmit`

---

### [ ] Step: Frontend Super Admin Dashboard Page, Routing, and Navigation

- Create `frontend/src/pages/SuperAdminDashboardPage.vue` with:
  - Summary stat cards (total, open, upcoming, completed elections)
  - Filterable/sortable election table
  - Side drawer for election detail (owner info)
- Add `/super-admin` route in `router.ts` with SA guard
- Add conditional "Super Admin" menu item in `AppSidebar.vue`
- Run `npx vue-tsc --noEmit`
- Write `{@artifacts_path}/report.md`
