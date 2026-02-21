# Technical Specification: People File Upload & Import

## Technical Context

### Language & Runtime
- **Backend**: C# / .NET 10, ASP.NET Core Web API, Entity Framework Core, SignalR
- **Frontend**: TypeScript, Vue 3 (Composition API), Vite, Pinia, Element Plus, vue-i18n

### Key Dependencies Already Available
- **ClosedXML 0.105.0** (backend) — XLSX reading/writing
- **CsvHelper 33.0.1** (backend) — CSV parsing
- **@microsoft/signalr** (frontend) — real-time progress
- **element-plus** (frontend) — UI components (el-steps, el-upload, el-table, el-select, etc.)

### Existing Code to Reuse
| Component | Location | Reuse Strategy |
|---|---|---|
| `ImportFile` entity | `Backend.Domain/Entities/ImportFile.cs` | Reuse as-is — has all needed columns (Contents, ColumnsToRead, ProcessingStatus, FileType, CodePage, FirstDataRow, OriginalFileName) |
| `MainDbContext.ImportFiles` DbSet | `Backend.Domain/Context/MainDbContext.cs:23` | Reuse — already configured with computed columns (FileSize, HasContent) |
| `IneligibleReasonEnum` | `Backend.Domain/Enumerations/IneligibleReasonEnum.cs` | Reuse `GetByDescription()` and `GetByCode()` for eligibility matching during import |
| `CreatePersonDto` | `backend/DTOs/People/CreatePersonDto.cs` | Reuse for bulk person creation |
| `PeopleService.CreatePersonAsync` | `backend/Services/PeopleService.cs:129` | Reference `SyncEligibility()` logic; bulk import bypasses single-person creation (too slow) |
| `BallotImportHub` | `backend/Hubs/BallotImportHub.cs` | Pattern reference for new `PeopleImportHub` |
| `BallotImportPage.vue` | `frontend/src/pages/ballots/BallotImportPage.vue` | Pattern reference for 3-step wizard UI |
| `importStore.ts` | `frontend/src/stores/importStore.ts` | Pattern reference for SignalR integration in store |
| `signalrService.ts` | `frontend/src/services/signalrService.ts` | Extend with people import hub connection methods |
| `EligibilityController` | `backend/Controllers/EligibilityController.cs` | Already provides GET endpoint for eligibility reasons list |

---

## Implementation Approach

### Overview

Create a dedicated page at `/elections/:id/people/import` with a 3-step wizard (Upload -> Map Columns -> Import). Files are uploaded as binary to the existing `ImportFile` table. Parsing happens server-side using ClosedXML (XLSX) and CsvHelper (CSV/TAB). A new `PeopleImportService` handles file parsing and bulk person creation. A new `PeopleImportHub` provides real-time progress via SignalR.

### Architecture Decisions

1. **Server-side file storage & parsing**: Files are uploaded as multipart/form-data and stored in `ImportFile.Contents` (image binary column). All parsing happens server-side so users can return to complete mapping/import later without re-uploading.

2. **New controller (`PeopleImportController`)** rather than extending existing `ImportController`: The people import workflow is significantly different from ballot import (file-based vs content-based, multi-step, stored files). A dedicated controller keeps things clean.

3. **New service (`PeopleImportService`)** rather than extending `ImportService`: Ballot import parses CSV from request body; people import reads from stored binary, supports XLSX/CSV/TAB, and does deduplication. Separate service avoids bloating the existing one.

4. **New SignalR hub (`PeopleImportHub`)** rather than reusing `BallotImportHub`: Different group naming, different event payloads. Keeps ballot and people import concerns separate.

5. **Bulk insert strategy**: Instead of calling `PeopleService.CreatePersonAsync` per row (which does individual SaveChanges, duplicate checks, SignalR notifications), the import service directly creates `Person` entities in batches (100 at a time) with a single `SaveChangesAsync()` per batch. This avoids N+1 saves and unnecessary per-row SignalR notifications.

6. **Column mapping UI**: Unlike the ballot import which maps TallyJ fields -> file columns, the people import maps file columns -> TallyJ fields (matching the v3 pattern shown in screenshots). Each file column header gets a dropdown to select the target TallyJ field.

---

## Source Code Structure Changes

### New Files

#### Backend
| File | Purpose |
|---|---|
| `backend/Controllers/PeopleImportController.cs` | REST endpoints for file upload, file list, file operations, parsing, column mapping, import execution, and delete-all-people |
| `backend/Services/PeopleImportService.cs` | Business logic: file storage, XLSX/CSV/TAB parsing, auto-mapping, deduplication, bulk person creation |
| `backend/Services/IPeopleImportService.cs` | Interface for PeopleImportService |
| `backend/DTOs/Import/PeopleImportDtos.cs` | All DTOs for people import (ImportFileDto, UploadFileResponse, ParseFileResponse, ColumnMappingDto, ImportPeopleRequest, ImportPeopleResult, DeleteAllPeopleResult) |
| `backend/Hubs/PeopleImportHub.cs` | SignalR hub for real-time import progress |

#### Frontend
| File | Purpose |
|---|---|
| `frontend/src/pages/people/PeopleImportPage.vue` | Main 3-step import wizard page |
| `frontend/src/stores/peopleImportStore.ts` | Pinia store for import state, file list, SignalR events |
| `frontend/src/services/peopleImportService.ts` | API service wrapping people import endpoints |
| `frontend/src/types/PeopleImport.ts` | TypeScript types for people import |

### Modified Files

| File | Change |
|---|---|
| `backend/Program.cs` | Register `IPeopleImportService`/`PeopleImportService`, map `PeopleImportHub` |
| `frontend/src/router/router.ts` | Add route `elections/:id/people/import` -> `PeopleImportPage.vue` |
| `frontend/src/pages/people/PeopleManagementPage.vue` | Change "Import People" action from dialog to `router.push` to new page |
| `frontend/src/services/signalrService.ts` | Add `connectToPeopleImportHub()`, `joinPeopleImportSession()`, `leavePeopleImportSession()` methods |
| `frontend/src/locales/en/people.json` (or equivalent) | Add i18n keys for import page labels |

---

## Data Model

### Existing `ImportFile` Entity (No Changes Needed)

The existing entity already has all required columns:

| Column | Type | Purpose |
|---|---|---|
| `RowId` | int (PK, identity) | Primary key |
| `ElectionGuid` | Guid (FK) | Links to election |
| `UploadTime` | DateTime? | When file was uploaded |
| `ImportTime` | DateTime? | When import was executed |
| `FileSize` | int? (computed) | `datalength(Contents)` |
| `HasContent` | bool? (computed) | Whether Contents is non-null |
| `FirstDataRow` | int? | 1-based row number of first data row (header row number) |
| `ColumnsToRead` | string? | JSON column mapping configuration |
| `OriginalFileName` | string?(50) | Original file name |
| `ProcessingStatus` | string?(20) | `Uploaded`, `Mapped`, `Imported` |
| `FileType` | string?(10) | `csv`, `tab`, `xlsx` |
| `CodePage` | int? | Encoding code page for text files |
| `Messages` | string? | Status/error messages |
| `Contents` | byte[]? (image) | File binary content |

### ColumnsToRead JSON Format

Stored in `ImportFile.ColumnsToRead` as a JSON array:

```json
[
  { "fileColumn": "Bahá'í ID", "targetField": "BahaiId" },
  { "fileColumn": "LastName", "targetField": "LastName" },
  { "fileColumn": "FirstName", "targetField": "FirstName" },
  { "fileColumn": "G", "targetField": null }
]
```

Each entry maps one file column to a TallyJ target field (or `null` to ignore).

---

## API Endpoints

All endpoints under `api/PeopleImport` with `[Authorize]`.

### POST `api/PeopleImport/{electionGuid}/upload`
Upload a file. Accepts `multipart/form-data` with single file.
- Validates file extension (.csv, .tsv, .tab, .txt, .xlsx), size (<=10MB)
- Detects file type from extension
- Stores binary in `ImportFile.Contents`
- Sets `ProcessingStatus = "Uploaded"`, `FileType`, `OriginalFileName`, `UploadTime`, `FirstDataRow = 1`, `CodePage = 65001` (UTF-8 default)
- Returns: `ImportFileDto`

### GET `api/PeopleImport/{electionGuid}/files`
List all import files for this election.
- Returns: `List<ImportFileDto>` (without Contents binary)

### GET `api/PeopleImport/{electionGuid}/files/{rowId}/parse`
Parse the specified file and return headers + preview rows.
- Query params: `codePage` (optional, override encoding), `firstDataRow` (optional, override header row)
- Reads binary from `ImportFile.Contents`
- Parses based on `FileType` (ClosedXML for xlsx, CsvHelper for csv/tab)
- Returns: `ParseFileResponse { headers: string[], previewRows: string[][], totalDataRows: int }`

### PUT `api/PeopleImport/{electionGuid}/files/{rowId}/mapping`
Save column mapping for a file.
- Body: `List<ColumnMappingDto>` (array of `{ fileColumn, targetField }`)
- Serializes to JSON and saves in `ImportFile.ColumnsToRead`
- Sets `ProcessingStatus = "Mapped"`
- Returns: `ImportFileDto`

### PUT `api/PeopleImport/{electionGuid}/files/{rowId}/settings`
Update file settings (firstDataRow, codePage).
- Body: `UpdateFileSettingsDto { firstDataRow?: int, codePage?: int }`
- Returns: `ImportFileDto`

### POST `api/PeopleImport/{electionGuid}/files/{rowId}/import`
Execute the import for the specified file.
- Reads stored binary and column mapping from `ImportFile`
- Validates: FirstName and LastName must be mapped
- Parses file rows using stored settings
- Deduplication logic:
  - If BahaiId mapped & present: match by BahaiId only
  - Else: match by exact FirstName+LastName (case-insensitive)
  - If match found: skip (do not update)
  - If no match: create new Person
- Eligibility status handling: match via `IneligibleReasonEnum.GetByDescription()` then `GetByCode()`; unrecognized = eligible with warning
- Batched saves (100 rows per batch)
- SignalR progress events via `PeopleImportHub`
- Sets `ProcessingStatus = "Imported"`, `ImportTime`
- Returns: `ImportPeopleResult { peopleAdded, peopleSkipped, warnings[], totalRows, timeElapsed }`

### DELETE `api/PeopleImport/{electionGuid}/files/{rowId}`
Delete an import file record.
- Returns: 204 No Content

### DELETE `api/PeopleImport/{electionGuid}/people`
Delete all people for an election.
- Validates: no ballots exist, no people have voting status (RegistrationTime set)
- Returns: `DeleteAllPeopleResult { deletedCount }`

### GET `api/PeopleImport/{electionGuid}/people-count`
Get current people count for the election.
- Returns: `{ count: int }`

---

## DTOs

### ImportFileDto
```csharp
public class ImportFileDto
{
    public int RowId { get; set; }
    public Guid ElectionGuid { get; set; }
    public DateTime? UploadTime { get; set; }
    public DateTime? ImportTime { get; set; }
    public int? FileSize { get; set; }
    public bool? HasContent { get; set; }
    public int? FirstDataRow { get; set; }
    public string? ColumnsToRead { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ProcessingStatus { get; set; }
    public string? FileType { get; set; }
    public int? CodePage { get; set; }
    public string? Messages { get; set; }
}
```

### ParseFileResponse
```csharp
public class ParseFileResponse
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> PreviewRows { get; set; } = new();
    public int TotalDataRows { get; set; }
}
```

### ColumnMappingDto
```csharp
public class ColumnMappingDto
{
    public string FileColumn { get; set; } = null!;
    public string? TargetField { get; set; }
}
```

### ImportPeopleResult
```csharp
public class ImportPeopleResult
{
    public bool Success { get; set; }
    public int PeopleAdded { get; set; }
    public int PeopleSkipped { get; set; }
    public int TotalRows { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public double TimeElapsedSeconds { get; set; }
}
```

### UpdateFileSettingsDto
```csharp
public class UpdateFileSettingsDto
{
    public int? FirstDataRow { get; set; }
    public int? CodePage { get; set; }
}
```

### DeleteAllPeopleResult
```csharp
public class DeleteAllPeopleResult
{
    public int DeletedCount { get; set; }
}
```

---

## Frontend Types

### `PeopleImport.ts`
```typescript
export interface ImportFileInfo {
  rowId: number
  electionGuid: string
  uploadTime: string | null
  importTime: string | null
  fileSize: number | null
  hasContent: boolean | null
  firstDataRow: number | null
  columnsToRead: string | null
  originalFileName: string | null
  processingStatus: string | null
  fileType: string | null
  codePage: number | null
  messages: string | null
}

export interface ParseFileResult {
  headers: string[]
  previewRows: string[][]
  totalDataRows: number
}

export interface ColumnMapping {
  fileColumn: string
  targetField: string | null
}

export interface ImportPeopleResult {
  success: boolean
  peopleAdded: number
  peopleSkipped: number
  totalRows: number
  warnings: string[]
  errors: string[]
  timeElapsedSeconds: number
}

export const PEOPLE_TARGET_FIELDS = [
  { value: 'FirstName', label: 'First Name', required: true },
  { value: 'LastName', label: 'Last Name', required: true },
  { value: 'BahaiId', label: "Baha'i ID" },
  { value: 'IneligibleReasonDescription', label: 'Eligibility Status' },
  { value: 'Area', label: 'Area' },
  { value: 'Email', label: 'Email' },
  { value: 'Phone', label: 'Phone' },
  { value: 'OtherNames', label: 'Other Names' },
  { value: 'OtherLastNames', label: 'Other Last Names' },
  { value: 'OtherInfo', label: 'Other Info' },
] as const
```

---

## SignalR Hub

### PeopleImportHub (`/hubs/people-import`)

**Server-to-client events:**
- `importProgress(processed: int, total: int, status: string)` — row-level progress
- `importError(message: string)` — non-fatal error/warning
- `importComplete(result: ImportPeopleResult)` — import finished

**Client-to-server methods:**
- `JoinImportSession(electionGuid: Guid)` — join group `PeopleImport{electionGuid}`
- `LeaveImportSession(electionGuid: Guid)` — leave group

---

## Auto-Mapping Logic

When a file is parsed, the backend attempts to auto-match file column headers to TallyJ target fields. The matching uses a configurable alias table:

| Target Field | Aliases (case-insensitive, spaces/underscores ignored) |
|---|---|
| FirstName | first name, firstname, first_name, given name, givenname |
| LastName | last name, lastname, last_name, surname, family name, familyname |
| BahaiId | bahai id, bahaiid, bahai_id, baha'i id, id |
| IneligibleReasonDescription | eligibility, eligibility status, status, ineligible reason |
| Area | area, region, locality, community |
| Email | email, email address, e-mail |
| Phone | phone, phone number, telephone, tel, mobile |
| OtherNames | other names, othernames, other_names, middle name, middlename |
| OtherLastNames | other last names, otherlastnames, maiden name, former name, formername |
| OtherInfo | other info, otherinfo, other_info, notes, comments |

Normalization: strip spaces, underscores, hyphens; lowercase; then compare.

The auto-mapped result is returned as part of the parse response and the client can display it as pre-selected dropdown values.

---

## Frontend Page Design

### PeopleImportPage.vue — 3-Step Wizard

Uses `el-steps` component (same pattern as `BallotImportPage.vue`).

#### Step 1: Upload File
- Drag-and-drop `el-upload` zone accepting `.csv,.tsv,.tab,.txt,.xlsx`
- On upload success, file appears in the **"Files on Server"** table
- Table columns: Action (Select button), Status, File Name, Upload Time, Headers on Line (editable `el-input-number`), Content Encoding (dropdown for text files; hidden for xlsx), Size, Other Actions (re-parse icon button, delete icon button)
- Clicking "Select" on a row sets it as the active file and triggers parse

#### Step 2: Map Columns
- Horizontal scrollable mapping table:
  - **Row 1 (header)**: file column names
  - **Row 2 (mapping)**: dropdown per column selecting TallyJ field or "(ignore)"
  - **Rows 3-7 (preview)**: first 5 data rows read-only
- Green highlighting on columns that have a mapping (matching v3 pattern)
- Below the table: collapsible "TallyJ Fields" reference showing required/optional fields
- Below that: collapsible "Valid Eligibility Status Reasons" reference and "Recommended Status Settings" (matching v3 screenshots)
- Mappings auto-save on change via PUT mapping endpoint

#### Step 3: Import People
- Summary: number of data rows, mapped fields list
- Validation: First Name and Last Name must be mapped
- "Import now" button — starts import, shows progress bar
- Progress bar updated via SignalR
- Results summary after completion: people added, skipped, warnings
- Current people count displayed at bottom
- "Delete all People Records" button with guards (disabled if ballots exist or people have voting status)

---

## Delivery Phases

### Phase 1: Backend — File Storage & Parsing
- `PeopleImportController` with upload, list, delete, parse, settings endpoints
- `PeopleImportService` with file storage, XLSX/CSV/TAB parsing, auto-mapping
- DTOs
- Registration in `Program.cs`
- Unit tests for parsing and auto-mapping logic

### Phase 2: Backend — Import Execution
- Import endpoint in controller
- Deduplication and bulk person creation logic in service
- `PeopleImportHub` SignalR hub
- Delete-all-people endpoint with safety guards
- Unit tests for deduplication and import logic

### Phase 3: Frontend — Import Page & Store
- `PeopleImportPage.vue` with all 3 steps
- `peopleImportStore.ts` with SignalR integration
- `peopleImportService.ts` API wrapper
- TypeScript types
- Route registration
- Modify `PeopleManagementPage.vue` to navigate to import page
- SignalR service extensions
- i18n keys

---

## Verification Approach

### Backend
```bash
cd backend && dotnet build
cd TallyJ4.Tests && dotnet test
```

### Frontend
```bash
cd frontend && npx vue-tsc --noEmit
cd frontend && npm run test:run
```

### Manual Testing
1. Upload CSV, TAB, XLSX files and verify parsing
2. Verify auto-mapping for common column names
3. Import with duplicates and verify skip logic
4. Import with eligibility status values and verify matching
5. Verify SignalR progress updates during large file import
6. Verify delete-all-people guards (disabled when ballots exist)
7. Verify file persistence (upload, leave page, return, file still listed)
