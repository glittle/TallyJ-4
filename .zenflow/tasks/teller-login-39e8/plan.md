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

If you are blocked and need user clarification, mark the current step with `[!]` in plan.md before stopping.

---

## Workflow Steps

### [x] Step: Technical Specification

Spec saved to `.zenflow/tasks/teller-login-39e8/spec.md`. Difficulty: **Hard**.

---

### [x] Step 1: Backend DTOs & Election Service — Teller Access Fields
<!-- chat-id: 95a5c597-2ba9-48d4-9d87-bceced4c580b -->

Add `IsTellerAccessOpen`, `TellerAccessOpenedAt` to `ElectionDto` and `IsTellerAccessOpen`, `IsOnlineVotingEnabled`, `ShowAsTest` to `ElectionSummaryDto`. Update `ElectionProfile` mappings, `ElectionService` projections, and `IElectionService` interface. Add `ToggleTellerAccessAsync` method to service and controller endpoint `PUT /api/elections/{guid}/teller-access`.

- [x] Modify `ElectionSummaryDto.cs`, `ElectionDto.cs`
- [x] Modify `ElectionProfile.cs` mapping
- [x] Modify `ElectionService.cs` — populate new fields + add toggle method
- [x] Modify `IElectionService.cs` — add toggle method signature
- [x] Modify `ElectionsController.cs` — add toggle endpoint
- [x] Run `dotnet build` and `dotnet test`

---

### [x] Step 2: Backend Teller Login Endpoint
<!-- chat-id: 099c0b5a-a821-424c-9200-39ce0448cd16 -->

Add `[AllowAnonymous] POST /api/auth/teller-login` endpoint that validates election GUID + access code against `ElectionPasscode`, checks `ListedForPublicAsOf` is non-null, and returns a limited JWT with teller claims.

- [x] Add teller login request/response DTOs
- [x] Add endpoint in `AuthController.cs`
- [x] Run `dotnet build` and `dotnet test`

---

### [ ] Step 3: Frontend Types, Services & Store Updates
<!-- chat-id: a47e2b88-584c-4a9c-a684-872cf5db987e -->

Update frontend TypeScript types, services, and Pinia store for the new backend fields and endpoints.

- [ ] Update `Election.ts` types (`isTellerAccessOpen`, `tellerAccessOpenedAt`, etc.)
- [ ] Update `electionService.ts` — map new fields in `getAll()`
- [ ] Update `authService.ts` — add `tellerLogin()` method
- [ ] Update `tellerService.ts` — add `toggleTellerAccess()` method
- [ ] Update `electionStore.ts` — add toggle action
- [ ] Run `npx vue-tsc --noEmit` and `npm run test:run`

---

### [ ] Step 4: Dashboard — Remove Teller Count Calls, Use `isTellerAccessOpen`
<!-- chat-id: f3a085df-49f4-4721-b6f4-85e02283a79c -->

Remove the N+1 teller count API calls from `DashboardPage.vue`. Use `isTellerAccessOpen` from the election summary data instead.

- [ ] Remove `loadTellerCounts()`, `ElectionWithDetails`, and teller service import
- [ ] Update teller status column and expanded row section
- [ ] Run `npx vue-tsc --noEmit`

---

### [ ] Step 5: Election Detail Page — Teller Information Section + QR Code
<!-- chat-id: 859df183-9e89-4bde-85a5-c264a64ef10c -->

Add a "Teller Access" card after Quick Actions showing: toggle, access code, shareable URL, QR code. Also show online voting status. Install `qrcode` npm dependency.

- [ ] `npm install qrcode @types/qrcode`
- [ ] Add teller info section to `ElectionDetailPage.vue`
- [ ] Add i18n keys to `en/elections.json` and `en/dashboard.json`
- [ ] Add French i18n keys
- [ ] Run `npx vue-tsc --noEmit`

---

### [ ] Step 6: Teller Join Route & Page
<!-- chat-id: 34b200f0-2354-4b38-8a80-536f250e9f30 -->

Create `TellerJoinPage.vue` (public route `/teller-join/:electionGuid?code=...`) that reads params, calls the teller-login endpoint, and redirects on success.

- [ ] Create `TellerJoinPage.vue`
- [ ] Add route to `router.ts` (public, no auth required)
- [ ] Add i18n keys for teller join page
- [ ] Run `npx vue-tsc --noEmit` and `npm run test:run`

---

### [ ] Step 7: Final Verification & Report

- [ ] Run full backend build and tests
- [ ] Run full frontend type-check and tests
- [ ] Write report to `.zenflow/tasks/teller-login-39e8/report.md`
