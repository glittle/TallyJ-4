# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: 83bb7077-4131-44b3-80f9-f6fe5af42b58 -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 9c7fb2d9-c342-4d12-bc6a-e941fec1dc26 -->

Create a technical specification based on the PRD in `{@artifacts_path}/requirements.md`.

1. Review existing codebase architecture and identify reusable components
2. Define the implementation approach

Save to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach referencing existing code patterns
- Source code structure changes
- Data model / API / interface changes
- Delivery phases (incremental, testable milestones)
- Verification approach using project lint/test commands

### [x] Step: Planning
<!-- chat-id: 06f70f86-c747-4822-8803-bb800597b1e1 -->

Created a detailed implementation plan. The Implementation step below has been replaced with 6 concrete tasks.

### [x] Step 1: Backend Static Class + DTO + API Endpoint
<!-- chat-id: c46cd478-c718-4d88-8b3f-b486c58c4ad1 -->

Create the `IneligibleReasonEnum` static class, the `EligibilityReasonDto`, and the `EligibilityController` API endpoint. Include unit tests for all lookup methods and legacy GUID mapping.

**New files:**
- `Backend.Domain/Enumerations/IneligibleReasonEnum.cs` — `IneligibleReason` data class + `IneligibleReasonEnum` static class with all 20 reasons (X01–X09, V01–V06, R01–R03, U01–U02), legacy GUID map, and lookup methods (`GetByGuid`, `GetByCode`, `GetByDescription`). Follow the pattern in `Backend.Domain/Enumerations/ElectionTypeEnum.cs`.
- `backend/DTOs/EligibilityReasonDto.cs` — DTO with `ReasonGuid`, `Code`, `Description`, `CanVote`, `CanReceiveVotes`, `InternalOnly`.
- `backend/Controllers/EligibilityController.cs` — `GET /api/eligibility-reasons` returning `ApiResponse<List<EligibilityReasonDto>>`. Maps from `IneligibleReasonEnum.All`. Requires `[Authorize]`.
- `Backend.Tests/UnitTests/Enumerations/IneligibleReasonEnumTests.cs` — Tests:
  - [x] All 20 reasons present in `All` list
  - [x] GUIDs match v3 exactly (verify each GUID from requirements.md R3)
  - [x] `GetByGuid` returns correct reason for each canonical GUID
  - [x] `GetByGuid` resolves legacy sub-GUIDs (5 legacy GUIDs → U01/U02)
  - [x] `GetByGuid(null)` returns null
  - [x] `GetByCode("X01")` returns correct reason; unknown code returns null
  - [x] `GetByDescription("Deceased")` returns X01; case-insensitive match works
  - [x] `PersonReasons` excludes U01 and U02
  - [x] CanVote/CanReceiveVotes flags correct per group (X=false/false, V=true/false, R=false/true, U=false/false)
  - [x] InternalOnly is true only for U01, U02

**Verification:** `dotnet test --filter "IneligibleReasonEnumTests"` passes. `dotnet build` succeeds.

### [x] Step 2: Backend Eligibility Enforcement in PeopleService
<!-- chat-id: 4943cf1d-aa97-435c-98aa-cacf902f899d -->

Update `PeopleService`, DTOs, validators, and AutoMapper profile so that `CanVote`/`CanReceiveVotes` are server-computed from `IneligibleReasonGuid`. Include unit tests.

**Modified files:**
- `backend/DTOs/People/CreatePersonDto.cs` — Remove `CanVote` and `CanReceiveVotes` properties.
- `backend/DTOs/People/UpdatePersonDto.cs` — Remove `CanVote` and `CanReceiveVotes` properties.
- `backend/DTOs/People/PersonDto.cs` — Add `IneligibleReasonCode` (`string?`) property.
- `backend/Mappings/PersonProfile.cs` — In `Person → PersonDto` map, add custom mapping for `IneligibleReasonCode` using `IneligibleReasonEnum.GetByGuid(src.IneligibleReasonGuid)?.Code`. In `CreatePersonDto → Person` and `UpdatePersonDto → Person` maps, add `.ForMember(dest => dest.CanVote, opt => opt.Ignore())` and `.ForMember(dest => dest.CanReceiveVotes, opt => opt.Ignore())` since these are now server-computed.
- `backend/Services/PeopleService.cs` — In `CreatePersonAsync` and `UpdatePersonAsync`, after mapping DTO → entity (and before `SaveChangesAsync`), add eligibility sync logic:
  - If `IneligibleReasonGuid` is not null: look up reason via `IneligibleReasonEnum.GetByGuid`; set `CanVote`/`CanReceiveVotes` from reason; if GUID unknown, set both to `false` and log warning.
  - If `IneligibleReasonGuid` is null: set `CanVote = true`, `CanReceiveVotes = true`.
- `backend/Validators/CreatePersonDtoValidator.cs` — Add rule: if `IneligibleReasonGuid` is provided, it must exist in `IneligibleReasonEnum.GetByGuid()` and must not be `InternalOnly`.
- `backend/Validators/UpdatePersonDtoValidator.cs` — Same validation rule.

**New/modified test files:**
- `Backend.Tests/UnitTests/Services/PeopleServiceTests.cs` — Add tests:
  - [ ] Create person with null `IneligibleReasonGuid` → `CanVote=true`, `CanReceiveVotes=true`
  - [ ] Create person with X01 GUID → `CanVote=false`, `CanReceiveVotes=false`
  - [ ] Create person with V01 GUID → `CanVote=true`, `CanReceiveVotes=false`
  - [ ] Create person with R01 GUID → `CanVote=false`, `CanReceiveVotes=true`
  - [ ] Update person: changing `IneligibleReasonGuid` from null to X01 updates `CanVote`/`CanReceiveVotes`
  - [ ] Update person: clearing `IneligibleReasonGuid` restores full eligibility
  - [ ] PersonDto response includes correct `IneligibleReasonCode`

**Verification:** `dotnet test --filter "PeopleServiceTests"` passes. `dotnet build` succeeds.

### [x] Step 3: Frontend Types + Eligibility Service + Store
<!-- chat-id: a2812d68-77ad-4806-9cc6-bd4e9b7755c0 -->

Create TypeScript types, API service, and Pinia store for eligibility reasons. No UI changes yet.

**New files:**
- `frontend/src/types/Eligibility.ts` — `EligibilityReasonDto` interface with `reasonGuid`, `code`, `description`, `canVote`, `canReceiveVotes`, `internalOnly`.
- `frontend/src/services/eligibilityService.ts` — Service with `getAll()` method calling `GET /api/eligibility-reasons`. Use `api.get()` from `services/api.ts` following the pattern in `peopleService.ts`. (Note: since the SDK is auto-generated, we may use a manual Axios call via `api.ts` or add to the SDK later.)
- `frontend/src/stores/eligibilityStore.ts` — Pinia store:
  - State: `reasons: EligibilityReasonDto[]`, `loaded: boolean`
  - Action: `fetchReasons()` — calls service, caches result, only fetches once.
  - Getters: `personReasons` (excludes `internalOnly`), `groupedReasons` (grouped by code prefix: X, V, R), `getByGuid(guid)`, `getByCode(code)`.

**Modified files:**
- `frontend/src/types/Person.ts` — Remove `canVote` and `canReceiveVotes` from `CreatePersonDto` and `UpdatePersonDto`. Add `ineligibleReasonCode?: string` to `PersonDto`. Keep `canVote`/`canReceiveVotes` in `PersonDto` (read-only from server).
- `frontend/src/types/index.ts` — Add `export * from './Eligibility';`

**Verification:** `npx vue-tsc --noEmit` passes from `frontend/`.

### [ ] Step 4: Frontend i18n for Eligibility Reasons

Add English and French locale files for all eligibility reason codes, group labels, and UI labels.

**New files:**
- `frontend/src/locales/en/eligibility.json` — Flat-key JSON with:
  - `eligibility.label`, `eligibility.eligible`, `eligibility.selectReason`
  - `eligibility.groupX`, `eligibility.groupV`, `eligibility.groupR` (group labels)
  - `eligibility.X01` through `eligibility.X09`, `eligibility.V01` through `eligibility.V06`, `eligibility.R01` through `eligibility.R03` (exact English descriptions from requirements.md R3)
- `frontend/src/locales/fr/eligibility.json` — Same structure with French translations (best-effort; placeholders for uncertain translations).

These files are auto-discovered by the glob import in `frontend/src/locales/index.ts`.

**Modified files:**
- `frontend/src/locales/en/people.json` — Update `people.importOptionalFields` to mention `Eligibility` column (replacing separate `Can Vote` / `Can Receive Votes` columns).

**Verification:** `npx vue-tsc --noEmit` passes. Dev server loads without i18n warnings.

### [ ] Step 5: Frontend Person Form — Eligibility Dropdown

Replace the two `CanVote`/`CanReceiveVotes` toggle switches in `PersonFormDialog.vue` with a single grouped `el-select` dropdown. Update `PeopleTable.vue` to show eligibility info.

**Modified files:**
- `frontend/src/components/people/PersonFormDialog.vue`:
  - Import `useEligibilityStore` and call `fetchReasons()` on mount.
  - Replace `form.canVote` and `form.canReceiveVotes` with `form.ineligibleReasonGuid` (string | null).
  - Replace the two `el-switch` items with a single `el-form-item` containing an `el-select`:
    - Default option: "Eligible" (value = empty string or null)
    - Options grouped by prefix using `el-option-group` with i18n group labels (`eligibility.groupX`, etc.)
    - Each option label from `$t('eligibility.' + reason.code)`
    - U reasons excluded (use `personReasons` getter from store)
  - In `watch(() => props.person, ...)`: set `form.ineligibleReasonGuid` from `person.ineligibleReasonGuid`.
  - In `handleSubmit()`: send `ineligibleReasonGuid` (or null) instead of `canVote`/`canReceiveVotes`.
- `frontend/src/components/people/PeopleTable.vue`:
  - Replace the two CanVote/CanReceiveVotes icon columns with a single "Eligibility" column.
  - Display: if `ineligibleReasonCode` is set, show `$t('eligibility.' + ineligibleReasonCode)`; otherwise show a green check icon or "Eligible" text.

**Verification:** `npx vue-tsc --noEmit` passes. Manual test: open person form, see grouped dropdown, create/edit person with different reasons, verify table shows reason.

### [ ] Step 6: Final Verification and Cleanup

Run all tests, linting, and type checks to ensure everything works together.

- [ ] `dotnet build` (backend) — no errors
- [ ] `dotnet test` (all backend tests) — all pass
- [ ] `npx vue-tsc --noEmit` (frontend type check) — no errors
- [ ] `npm run test` (frontend tests if any affected) — all pass
- [ ] Verify OpenAPI spec regenerates correctly on backend dev startup (the new `EligibilityController` endpoint should appear)
- [ ] Record results in this plan
