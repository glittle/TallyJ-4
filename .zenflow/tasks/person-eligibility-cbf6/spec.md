# Person Eligibility - Technical Specification

## Technical Context

- **Backend**: .NET 10, ASP.NET Core Web API, Entity Framework Core, AutoMapper, FluentValidation
- **Frontend**: Vue 3 (Composition API), TypeScript, Pinia, Element Plus, vue-i18n
- **Pattern**: Static enumeration class in `Backend.Domain/Enumerations/` (see `ElectionTypeEnum.cs`, `ElectionModeEnum.cs` for conventions)
- **API pattern**: Controllers → Services → EF DbContext, DTOs with AutoMapper profiles, FluentValidation validators
- **Frontend pattern**: Generated SDK → service layer → Pinia store → Vue components
- **i18n**: Flat-key JSON files per domain in `frontend/src/locales/{en,fr}/`, auto-merged by `locales/index.ts`

## Implementation Approach

### 1. Static Class `IneligibleReasonEnum` (Backend Domain Layer)

Create `Backend.Domain/Enumerations/IneligibleReasonEnum.cs` following the pattern of `ElectionTypeEnum.cs`.

#### Data Model

```csharp
public class IneligibleReason
{
    public Guid ReasonGuid { get; }
    public string Code { get; }         // e.g. "X01", "V03", "U02"
    public string Description { get; }  // English description (for v3 compat lookup)
    public bool CanVote { get; }
    public bool CanReceiveVotes { get; }
    public bool InternalOnly { get; }   // true for U01, U02
}
```

#### Static Class

```csharp
public static class IneligibleReasonEnum
{
    // Group X: CanVote=false, CanReceiveVotes=false
    public static readonly IneligibleReason X01 = new(...); // Deceased
    public static readonly IneligibleReason X02 = new(...); // Moved elsewhere recently
    // ... X03-X09

    // Group V: CanVote=true, CanReceiveVotes=false
    public static readonly IneligibleReason V01 = new(...); // Youth aged 18/19/20
    // ... V02-V06

    // Group R: CanVote=false, CanReceiveVotes=true
    public static readonly IneligibleReason R01 = new(...); // Not a delegate in this election
    // ... R02-R03

    // Group U: Internal only (CanVote=false, CanReceiveVotes=false, InternalOnly=true)
    public static readonly IneligibleReason U01 = new(...); // Unidentifiable
    public static readonly IneligibleReason U02 = new(...); // Unreadable

    public static readonly IReadOnlyList<IneligibleReason> All = [...];
    public static readonly IReadOnlyList<IneligibleReason> PersonReasons = All.Where(r => !r.InternalOnly).ToList();

    // Lookup methods
    public static IneligibleReason? GetByGuid(Guid? guid);
    public static IneligibleReason? GetByCode(string code);
    public static IneligibleReason? GetByDescription(string description); // case-insensitive

    // Legacy GUID mapping: v3 had multiple sub-GUIDs for Unidentifiable/Unreadable
    // Map those old GUIDs to U01/U02
    private static readonly Dictionary<Guid, IneligibleReason> LegacyGuidMap = ...;
}
```

All GUIDs reused exactly from v3 `IneligibleReasonEnum.cs`. See requirements.md R3 for the full table.

#### Legacy GUID Mapping

v3 had 4 Unidentifiable sub-reasons and 3 Unreadable sub-reasons. In v4 these collapse into U01 and U02. The `GetByGuid` method checks the legacy map first, so old data resolves correctly.

| Legacy v3 GUID | v3 Description                      | Maps to |
| -------------- | ----------------------------------- | ------- |
| D927534D-...   | Could refer to more than one person | U01     |
| D727534D-...   | Multiple people with identical name | U01     |
| D827534D-...   | Name is a mix of multiple people    | U01     |
| D627534D-...   | In an unknown language              | U02     |
| 86DDBE4A-...   | Not a complete name                 | U02     |

U01 keeps `CE27534D-D7E8-E011-A095-002269C41D11` (the "Unknown person" GUID from v3) as its canonical GUID.
U02 keeps `CD27534D-D7E8-E011-A095-002269C41D11` (the "Writing is illegible" GUID from v3) as its canonical GUID.

### 2. DTO for Eligibility Reason

Create `backend/DTOs/EligibilityReasonDto.cs`:

```csharp
public class EligibilityReasonDto
{
    public Guid ReasonGuid { get; set; }
    public string Code { get; set; }
    public string Description { get; set; } // English (for backward compat)
    public bool CanVote { get; set; }
    public bool CanReceiveVotes { get; set; }
    public bool InternalOnly { get; set; }
}
```

### 3. API Endpoint

Add to a new `EligibilityController.cs` (or could be static endpoint on `PeopleController`):

```
GET /api/eligibility-reasons
```

Returns `ApiResponse<List<EligibilityReasonDto>>` with all reasons from `IneligibleReasonEnum.All`. No DB query needed — it maps static data to DTOs. Requires `[Authorize]`.

This endpoint enables the frontend to build dropdowns without hardcoding reason data.

### 4. Backend Consistency Enforcement

#### In `PeopleService.CreatePersonAsync` and `UpdatePersonAsync`:

After mapping the DTO to entity (or before save), add logic to sync `CanVote`/`CanReceiveVotes` from the `IneligibleReasonGuid`:

```
if (person.IneligibleReasonGuid != null)
    var reason = IneligibleReasonEnum.GetByGuid(person.IneligibleReasonGuid);
    if (reason != null)
        person.CanVote = reason.CanVote;
        person.CanReceiveVotes = reason.CanReceiveVotes;
    else
        // Unknown GUID: treat as fully ineligible, log warning
        person.CanVote = false;
        person.CanReceiveVotes = false;
else
    // No reason = fully eligible
    person.CanVote = true;
    person.CanReceiveVotes = true;
```

This replaces the current behavior where `CanVote` and `CanReceiveVotes` are set independently by the client. The DTOs (`CreatePersonDto`, `UpdatePersonDto`) will be updated:

- **Remove** `CanVote` and `CanReceiveVotes` fields from `CreatePersonDto` and `UpdatePersonDto` (they become server-computed)
- **Keep** `IneligibleReasonGuid` as the single input field for eligibility
- `PersonDto` (response) **keeps** `CanVote`, `CanReceiveVotes`, and `IneligibleReasonGuid` for display

#### Validator Updates

Add a validation rule to `CreatePersonDtoValidator` and `UpdatePersonDtoValidator`:

- If `IneligibleReasonGuid` is provided, it must match a known reason in `IneligibleReasonEnum`
- U-group reasons (InternalOnly) must be rejected for person records

### 5. PersonDto Response Enhancement

Add `ineligibleReasonCode` (string, e.g. "X01") to `PersonDto` so the frontend can display the code without needing a separate lookup:

```csharp
public string? IneligibleReasonCode { get; set; }
```

Set via AutoMapper custom mapping or in service layer when mapping entity → DTO.

### 6. Frontend Types

Update `frontend/src/types/Person.ts`:

```typescript
// Add to PersonDto
export interface PersonDto {
  // ... existing fields ...
  ineligibleReasonGuid?: string;
  ineligibleReasonCode?: string; // new
}

// Remove canVote/canReceiveVotes from CreatePersonDto/UpdatePersonDto
// Add/keep ineligibleReasonGuid

// New type
export interface EligibilityReasonDto {
  reasonGuid: string;
  code: string;
  description: string;
  canVote: boolean;
  canReceiveVotes: boolean;
  internalOnly: boolean;
}
```

### 7. Frontend Service & Store

#### `frontend/src/services/eligibilityService.ts` (new)

```typescript
export const eligibilityService = {
  async getAll(): Promise<EligibilityReasonDto[]> { ... }
}
```

Calls `GET /api/eligibility-reasons`. Caches result in memory (data is static).

#### `frontend/src/stores/eligibilityStore.ts` (new)

Pinia store that:

- Fetches and caches all reasons on first access
- Provides computed getters: `personReasons` (excluding U group), `groupedReasons` (by prefix)
- Provides lookup helpers: `getByGuid(guid)`, `getByCode(code)`

### 8. Frontend - Person Form

Replace the two `el-switch` controls for `canVote`/`canReceiveVotes` in `PersonFormDialog.vue` with a single `el-select` dropdown:

- Default option: "Eligible" (sends `ineligibleReasonGuid: null`)
- Grouped options (X, V, R) using `el-option-group`
- Labels from i18n keys: `eligibility.{code}` (e.g. `eligibility.X01`)
- Group labels from i18n: `eligibility.groupX`, `eligibility.groupV`, `eligibility.groupR`
- U reasons excluded from dropdown
- The form's reactive data changes from `{ canVote, canReceiveVotes }` to `{ ineligibleReasonGuid }`

### 9. i18n Entries

#### New file: `frontend/src/locales/en/eligibility.json`

```json
{
  "eligibility.label": "Eligibility",
  "eligibility.eligible": "Eligible",
  "eligibility.groupX": "Cannot vote, cannot receive votes",
  "eligibility.groupV": "Can vote only",
  "eligibility.groupR": "Can receive votes only",
  "eligibility.X01": "Deceased",
  "eligibility.X02": "Moved elsewhere recently",
  "eligibility.X03": "Not in this local unit",
  "eligibility.X04": "Not a registered Bahá’í",
  "eligibility.X05": "Under 18 years old",
  "eligibility.X06": "Resides elsewhere",
  "eligibility.X07": "Rights removed (entirely)",
  "eligibility.X08": "Not a delegate and on other Institution",
  "eligibility.X09": "Other (cannot vote or be voted for)",
  "eligibility.V01": "Youth aged 18/19/20",
  "eligibility.V02": "By-election: On Institution already",
  "eligibility.V03": "On other Institution (e.g. Counsellor)",
  "eligibility.V04": "Rights removed (cannot be voted for)",
  "eligibility.V05": "Tie-break election: Not tied",
  "eligibility.V06": "Other (can vote but not be voted for)",
  "eligibility.R01": "Not a delegate in this election",
  "eligibility.R02": "Rights removed (cannot vote)",
  "eligibility.R03": "Other (cannot vote but can be voted for)"
}
```

#### New file: `frontend/src/locales/fr/eligibility.json`

Same structure with French translations. Placeholder translations initially, to be reviewed.

These files are auto-discovered by the existing glob import in `locales/index.ts`.

### 10. Name Import Integration

Update import processing (when a people/names import is implemented) to:

- Accept an `Eligibility` or `IneligibleReason` column
- Match by code (`X01`, `V03`, etc.) first
- Fall back to case-insensitive English description match via `IneligibleReasonEnum.GetByDescription()`
- Reject U-group codes with error
- Reject unknown values with error (skip row, continue import)

**Note**: The current `ImportController` handles ballot imports only. People/names import is not yet implemented. This spec defines the eligibility column handling for when it is built. The `IneligibleReasonEnum` static class provides the `GetByCode` and `GetByDescription` methods needed.

## Source Code Structure Changes

### New Files

| File                                                                | Purpose                                    |
| ------------------------------------------------------------------- | ------------------------------------------ |
| `Backend.Domain/Enumerations/IneligibleReasonEnum.cs`               | Static reason definitions + lookup methods |
| `backend/DTOs/EligibilityReasonDto.cs`                              | DTO for API response                       |
| `backend/Controllers/EligibilityController.cs`                      | GET endpoint for reasons list              |
| `frontend/src/types/Eligibility.ts`                                 | TypeScript types                           |
| `frontend/src/services/eligibilityService.ts`                       | API service                                |
| `frontend/src/stores/eligibilityStore.ts`                           | Pinia store                                |
| `frontend/src/locales/en/eligibility.json`                          | English i18n                               |
| `frontend/src/locales/fr/eligibility.json`                          | French i18n                                |
| `Backend.Tests/UnitTests/Enumerations/IneligibleReasonEnumTests.cs` | Unit tests for static class                |
| `Backend.Tests/UnitTests/Services/EligibilityConsistencyTests.cs`   | Tests for CanVote/CanReceiveVotes sync     |

### Modified Files

| File                                                  | Change                                                                                              |
| ----------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `backend/DTOs/People/CreatePersonDto.cs`              | Remove `CanVote`, `CanReceiveVotes`                                                                 |
| `backend/DTOs/People/UpdatePersonDto.cs`              | Remove `CanVote`, `CanReceiveVotes`                                                                 |
| `backend/DTOs/People/PersonDto.cs`                    | Add `IneligibleReasonCode`                                                                          |
| `backend/Mappings/PersonProfile.cs`                   | Map `IneligibleReasonCode` via `IneligibleReasonEnum`                                               |
| `backend/Services/PeopleService.cs`                   | Add eligibility sync logic in create/update                                                         |
| `backend/Validators/CreatePersonDtoValidator.cs`      | Add `IneligibleReasonGuid` validation                                                               |
| `backend/Validators/UpdatePersonDtoValidator.cs`      | Add `IneligibleReasonGuid` validation                                                               |
| `backend/Program.cs`                                  | Register `EligibilityController` (auto if in same assembly)                                         |
| `frontend/src/types/Person.ts`                        | Remove `canVote`/`canReceiveVotes` from Create/Update DTOs, add `ineligibleReasonCode` to PersonDto |
| `frontend/src/types/index.ts`                         | Export new Eligibility types                                                                        |
| `frontend/src/components/people/PersonFormDialog.vue` | Replace switches with eligibility dropdown                                                          |
| `frontend/src/locales/en/people.json`                 | Update import instructions text                                                                     |

### No Schema Migration Required

The `Person` table already has `IneligibleReasonGuid`, `CanVote`, and `CanReceiveVotes` columns. No DB migration needed.

## Data Flow

### Person Create/Update

```
Frontend: user selects eligibility reason (or "Eligible")
  → sends { ineligibleReasonGuid: guid | null }
  → PeopleService validates GUID against IneligibleReasonEnum
  → PeopleService sets CanVote/CanReceiveVotes from reason
  → EF saves to DB
  → Response includes ineligibleReasonGuid, ineligibleReasonCode, canVote, canReceiveVotes
```

### Eligibility Reasons List

```
Frontend: eligibilityStore.fetchReasons()
  → GET /api/eligibility-reasons
  → Controller maps IneligibleReasonEnum.All to DTOs
  → Frontend caches, builds grouped dropdown
```

### Name Import (future)

```
Import file row has "Eligibility" column = "X01" or "Deceased"
  → IneligibleReasonEnum.GetByCode("X01") or GetByDescription("Deceased")
  → Set IneligibleReasonGuid on new Person
  → Same sync logic as above
```

## Delivery Phases

### Phase 1: Backend Static Class + API

1. Create `IneligibleReasonEnum` static class with all reasons, GUIDs, legacy mapping
2. Create `EligibilityReasonDto` and `EligibilityController`
3. Unit tests for static class (GUID correctness, lookup methods, legacy mapping)

### Phase 2: Backend Eligibility Enforcement

1. Update `CreatePersonDto`/`UpdatePersonDto` (remove CanVote/CanReceiveVotes)
2. Add eligibility sync logic to `PeopleService`
3. Add `IneligibleReasonCode` to `PersonDto` + AutoMapper config
4. Update validators
5. Unit tests for sync logic

### Phase 3: Frontend Types + Service + Store

1. Update `Person.ts` types
2. Create `Eligibility.ts` types
3. Create `eligibilityService.ts` and `eligibilityStore.ts`
4. Add i18n JSON files (en, fr)

### Phase 4: Frontend UI

1. Update `PersonFormDialog.vue` - replace switches with grouped dropdown
2. Update `PeopleTable.vue` if it displays eligibility info
3. Update import instructions text

## Verification

- **Backend unit tests**: `dotnet test` - IneligibleReasonEnum lookups, legacy GUID mapping, CanVote/CanReceiveVotes sync
- **Frontend type check**: `npx vue-tsc --noEmit`
- **Frontend tests**: `npm run test` - eligibility store tests
- **Manual**: Create/edit person with different eligibility reasons, verify CanVote/CanReceiveVotes set correctly
