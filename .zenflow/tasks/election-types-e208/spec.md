# Technical Specification: Fix Election Types and Modes

## Difficulty: Medium
Multiple files across backend, frontend, and tests need coordinated changes, but the changes are mechanical (find/replace values and labels). No architectural changes needed.

## Problem Summary

The election type and mode values were incorrectly defined during v4 development. The correct values from TallyJ v3 are:

### Correct Election Types
| Code | Display Name |
|------|-------------|
| LSA | Local Spiritual Assembly |
| LSA1 | Local Spiritual Assembly (Two-Stage) Local Unit |
| LSA2 | Local Spiritual Assembly (Two-Stage) Final |
| NSA | National Spiritual Assembly |
| Con | Unit Convention |
| Reg | Regional Council |
| Oth | Other |

### Correct Election Modes
| Code | Display Name |
|------|-------------|
| N | Normal Election |
| T | Tie-Break |
| B | By-election |

### Current (Wrong) Values

**Election Types** used in v4 code:
- `"STV"`, `"Cond"`, `"Multi"` — in validators (CreateElectionDtoValidator, UpdateElectionDtoValidator)
- `"STV"`, `"STVn"` — in ElectionStep2DtoValidator
- `"STV"` — in SetupService default, integration tests
- `"Standard"` — in unit tests (ElectionServiceTests)
- `"LSA"` — in DbSeeder election 1 and TallyServiceTests (correct)
- `"Conv"` — in DbSeeder election 2 (should be `"Con"`)
- `"FPTP"` — in IntegrationTestBase election 2
- `"SingleName"` — in TallyServiceTests
- `"Normal"` — in electionStore.test.ts

**Election Modes** used in v4 code:
- `"N"`, `"I"` — in validators (N is correct, I is wrong)
- `"I"` — in DbSeeder election 1 (wrong — should be N, T, or B)
- `"D"` — in DbSeeder election 2 (wrong)
- `"N"` — in SetupService default (correct)
- `"Normal"` — in electionStore.test.ts (wrong)

**Frontend labels:**
- ElectionFormTabs.vue: "STV - Single Transferable Vote", "Condorcet", "Multi-Winner" (all wrong)
- ElectionFormTabs.vue modes: "Normal", "International" (International is wrong)
- ElectionListPage.vue filter: "STV", "Condorcet", "Multi-Winner" (all wrong)

## Design Decision: Enum Pattern

Use a simple static class with constants — the modern C# idiomatic approach. No need for the complex `BaseEnumeration<TSelf, TValue>` pattern from v3. A static class with string constants and a lookup dictionary provides type safety, discoverability, and easy validation without the overhead.

## Implementation Approach

### 1. Create shared enum constants (backend)

Create `backend/TallyJ4.Domain/Enumerations/ElectionTypeEnum.cs` and `ElectionModeEnum.cs` as static classes with:
- String constants for codes
- Display name lookup dictionary
- Static `AllCodes` list for validation
- Static `IsValid(string)` method

### 2. Update backend validators

Update all three validators to use the new enum classes for validation:
- `CreateElectionDtoValidator.cs`: Replace `{"STV", "Cond", "Multi"}` with `ElectionTypeEnum.AllCodes`; replace `{"N", "I"}` with `ElectionModeEnum.AllCodes`
- `UpdateElectionDtoValidator.cs`: Same changes
- `ElectionStep2DtoValidator.cs`: Same changes

### 3. Update backend services

- `SetupService.cs:48`: Change default `ElectionType = "STV"` to `ElectionType = ElectionTypeEnum.LSA`
- `SetupService.cs:49`: Keep `ElectionMode = "N"` (correct) but use `ElectionModeEnum.Normal`

### 4. Update DbSeeder

- Election 1: `ElectionMode = "I"` → `"N"` (normal election for an LSA)
- Election 2: `ElectionType = "Conv"` → `"Con"`; `ElectionMode = "D"` → `"N"`

### 5. Update DTO comments

Fix misleading comments in DTOs:
- `ElectionDto.cs:25`: `"normal", "single-name"` → `"LSA", "NSA", "Con", etc.`
- `CreateElectionDto.cs:19`: Same
- `UpdateElectionDto.cs:64`: `"STV", "Cond"` → `"LSA", "NSA", "Con", etc.`
- `UpdateElectionDto.cs:70`: `"N" for normal, "I" for international` → `"N" for Normal, "T" for Tie-Break, "B" for By-election`
- `ElectionStep2Dto.cs:20,25`: Fix comments

### 6. Update frontend components

**ElectionFormTabs.vue** (lines 32-35): Replace election type options:
```
LSA - Local Spiritual Assembly
LSA1 - LSA (Two-Stage) Local Unit
LSA2 - LSA (Two-Stage) Final
NSA - National Spiritual Assembly
Con - Unit Convention
Reg - Regional Council
Oth - Other
```

**ElectionFormTabs.vue** (lines 43-44): Replace election mode options:
```
N - Normal Election
T - Tie-Break
B - By-election
```

**ElectionListPage.vue** (lines 49-51): Update type filter dropdown with correct types.

**CreateElectionPage.vue** (line 57): Change default `electionType: 'STV'` → `'LSA'`

### 7. Update unit tests

**ElectionServiceTests.cs**: Change all `ElectionType = "Standard"` to `"LSA"`

**TallyServiceTests.cs** (line 90): Change `"SingleName"` — this is used for the tally calculation type, not the election type. The `CalculateSingleNameElection` is a different concept (single-name counting method). The `electionType` parameter in `CreateTestElectionAsync` (line 450) defaults to `"LSA"` which is already correct. Line 90 passes `"SingleName"` as the election entity's type, but that's not a valid election type. Should be a valid type like `"Con"` (convention elections may use single-name counting).

**IntegrationTestBase.cs**: Change `ElectionType = "STV"` → `"LSA"` (line 201), `"FPTP"` → `"NSA"` (line 213)

**ResultsControllerTests.cs** (line 129): Change `ElectionType = "STV"` → `"LSA"`

**ElectionsControllerTests.cs**: Change all `ElectionType = "STV"` → `"LSA"`

**SuperAdminServiceTests.cs**: No election type changes needed (doesn't set ElectionType)

**electionStore.test.ts** (frontend): Change `electionType: 'Normal'` and `electionMode: 'Normal'` to valid codes

### 8. Update ResultsController

The `CalculateTally` endpoint (line 46) uses `electionType` query parameter with values `"normal"` and `"singlename"` — this is **not** the same as the election type enum. This is the **tally calculation method**. This should be renamed to avoid confusion, but that's a separate concern. For now, the parameter name is misleading but functionally separate from the election type stored on the entity.

## Files to Modify

### Backend - New Files
- `backend/TallyJ4.Domain/Enumerations/ElectionTypeEnum.cs`
- `backend/TallyJ4.Domain/Enumerations/ElectionModeEnum.cs`

### Backend - Modified Files
- `backend/Validators/CreateElectionDtoValidator.cs`
- `backend/Validators/UpdateElectionDtoValidator.cs`
- `backend/Validators/ElectionStep2DtoValidator.cs`
- `backend/Services/SetupService.cs`
- `backend/EF/Data/DbSeeder.cs`
- `backend/DTOs/Elections/ElectionDto.cs` (comments only)
- `backend/DTOs/Elections/CreateElectionDto.cs` (comments only)
- `backend/DTOs/Elections/UpdateElectionDto.cs` (comments only)
- `backend/DTOs/Setup/ElectionStep2Dto.cs` (comments only)

### Frontend - Modified Files
- `frontend/src/components/elections/ElectionFormTabs.vue`
- `frontend/src/pages/elections/ElectionListPage.vue`
- `frontend/src/pages/elections/CreateElectionPage.vue`
- `frontend/src/stores/electionStore.test.ts`

### Test Files - Modified
- `TallyJ4.Tests/UnitTests/ElectionServiceTests.cs`
- `TallyJ4.Tests/UnitTests/TallyServiceTests.cs`
- `TallyJ4.Tests/IntegrationTests/IntegrationTestBase.cs`
- `TallyJ4.Tests/IntegrationTests/ElectionsControllerTests.cs`
- `TallyJ4.Tests/IntegrationTests/ResultsControllerTests.cs`

## Data Model / API / Interface Changes

No schema changes needed. The `ElectionType` column is `varchar(5)` and `ElectionMode` is `varchar(1)` — all new values fit within these constraints (longest type code is `"LSA2"` at 4 chars, longest mode code is `"N"/"T"/"B"` at 1 char).

No API shape changes. The string fields remain the same; only the valid values change.

## Verification

1. `cd backend && dotnet build` — ensure compilation
2. `cd TallyJ4.Tests && dotnet test` — ensure all tests pass
3. `cd frontend && npx vue-tsc --noEmit` — ensure TypeScript compiles
4. `cd frontend && npm run test` — ensure frontend tests pass
