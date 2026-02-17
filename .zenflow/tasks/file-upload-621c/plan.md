# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: bd21bfea-8ecf-41aa-8a6f-81087bb042e1 -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 4dbac88b-bd83-410f-84c8-a2a3b245f245 -->

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
<!-- chat-id: e0b931b5-1109-45ab-bab7-86ce993d0846 -->

Broke down the spec into 7 implementation steps below.

### [ ] Step 1: Backend DTOs and PeopleImportService interface

Create all DTOs and the service interface needed by the people import feature.

- Create `backend/DTOs/Import/PeopleImportDtos.cs` with:
  - `ImportFileDto` — maps from `ImportFile` entity (all fields except `Contents`)
  - `ParseFileResponse` — `Headers: List<string>`, `PreviewRows: List<List<string>>`, `TotalDataRows: int`, `AutoMappings: List<ColumnMappingDto>`
  - `ColumnMappingDto` — `FileColumn: string`, `TargetField: string?`
  - `UpdateFileSettingsDto` — `FirstDataRow: int?`, `CodePage: int?`
  - `ImportPeopleResult` — `Success`, `PeopleAdded`, `PeopleSkipped`, `TotalRows`, `Warnings`, `Errors`, `TimeElapsedSeconds`
  - `DeleteAllPeopleResult` — `DeletedCount: int`
- Create `backend/Services/IPeopleImportService.cs` with method signatures for: upload, list files, parse, save mapping, update settings, import, delete file, delete all people, get people count
- Verify: `cd backend && dotnet build`

### [ ] Step 2: Backend PeopleImportService implementation

Implement `backend/Services/PeopleImportService.cs` with all business logic.

- File upload: validate extension/size, store in `ImportFile.Contents`, set metadata
- File listing: return all `ImportFile` records for an election (without `Contents` binary)
- File parsing: read from `ImportFile.Contents`, parse XLSX via ClosedXML, CSV/TAB via CsvHelper, respecting `CodePage` and `FirstDataRow`
- Auto-mapping: normalize file column headers (strip spaces/underscores/hyphens, lowercase) and match against alias table per spec
- Save column mapping: serialize `List<ColumnMappingDto>` to JSON in `ImportFile.ColumnsToRead`
- Update file settings: update `FirstDataRow`, `CodePage`
- Import execution:
  - Validate FirstName + LastName mapped
  - Load existing people for dedup (BahaiId dictionary + FirstName+LastName dictionary)
  - Parse all rows, create `Person` entities in batches of 100
  - Eligibility matching via `IneligibleReasonEnum.GetByDescription()` then `GetByCode()`
  - SignalR progress via `IHubContext<PeopleImportHub>`
  - Set `ProcessingStatus = "Imported"`, `ImportTime`
- Delete file: remove `ImportFile` record
- Delete all people: guard against existing ballots and people with `RegistrationTime` set; delete all `Person` records for election
- People count: `_context.People.CountAsync(p => p.ElectionGuid == electionGuid)`
- Register `IPeopleImportService` / `PeopleImportService` in `backend/Program.cs`
- Write unit tests in `Backend.Tests/UnitTests/PeopleImportServiceTests.cs`:
  - CSV parsing (comma, tab delimiters)
  - XLSX parsing
  - Auto-mapping logic (various header name variations)
  - Deduplication by BahaiId and by FirstName+LastName
  - Eligibility matching (by description, by code, unrecognized)
  - Delete-all-people guards (blocked when ballots exist, blocked when people have RegistrationTime)
- Verify: `cd backend && dotnet build && dotnet test`

### [ ] Step 3: Backend PeopleImportHub and PeopleImportController

Create the SignalR hub and REST controller.

- Create `backend/Hubs/PeopleImportHub.cs` following `BallotImportHub` pattern:
  - `JoinImportSession(Guid electionGuid)` — group `PeopleImport{electionGuid}`
  - `LeaveImportSession(Guid electionGuid)`
  - `OnDisconnectedAsync`
- Map hub in `backend/Program.cs`: `app.MapHub<PeopleImportHub>("/hubs/people-import")`
- Create `backend/Controllers/PeopleImportController.cs` with `[Authorize]`, `[Route("api/[controller]")]`:
  - `POST {electionGuid}/upload` — multipart/form-data file upload
  - `GET {electionGuid}/files` — list files
  - `GET {electionGuid}/files/{rowId}/parse` — parse file (query params: codePage, firstDataRow)
  - `PUT {electionGuid}/files/{rowId}/mapping` — save column mapping
  - `PUT {electionGuid}/files/{rowId}/settings` — update file settings
  - `POST {electionGuid}/files/{rowId}/import` — execute import
  - `DELETE {electionGuid}/files/{rowId}` — delete file
  - `DELETE {electionGuid}/people` — delete all people
  - `GET {electionGuid}/people-count` — get people count
- All endpoints delegate to `IPeopleImportService`
- Verify: `cd backend && dotnet build && dotnet test`

### [ ] Step 4: Frontend types, service, and store

Create frontend TypeScript types, API service wrapper, and Pinia store.

- Create `frontend/src/types/PeopleImport.ts`:
  - `ImportFileInfo`, `ParseFileResult`, `ColumnMapping`, `ImportPeopleResult`
  - `PEOPLE_TARGET_FIELDS` constant array
- Export new types from `frontend/src/types/index.ts`
- Create `frontend/src/services/peopleImportService.ts`:
  - Axios-based calls for all 9 API endpoints (upload, list files, parse, save mapping, update settings, import, delete file, delete all people, get count)
  - Use `api.ts` base Axios instance pattern
- Create `frontend/src/stores/peopleImportStore.ts`:
  - State: `files`, `selectedFile`, `parsedResult`, `columnMappings`, `importing`, `importResult`, `peopleCount`, `importProgress`
  - Actions: `loadFiles`, `uploadFile`, `selectFile`, `parseFile`, `saveMapping`, `updateSettings`, `executeImport`, `deleteFile`, `deleteAllPeople`, `loadPeopleCount`
  - SignalR integration: `initializeSignalR`, `joinImportSession`, `leaveImportSession`, handle `importProgress`/`importComplete`/`importError` events
- Verify: `cd frontend && npx vue-tsc --noEmit`

### [ ] Step 5: Frontend PeopleImportPage — Step 1 (Upload) and Step 2 (Map Columns)

Build the main import page with the first two wizard steps.

- Create `frontend/src/pages/people/PeopleImportPage.vue`:
  - Use `el-steps` with 3 steps (Upload, Map Columns, Import) — same pattern as `BallotImportPage.vue`
  - `<el-page-header>` with back button navigating to people management
  - **Step 1 (Upload)**:
    - `el-upload` drag-and-drop zone accepting `.csv,.tsv,.tab,.txt,.xlsx`, 10MB limit
    - On upload success via `peopleImportStore.uploadFile()`, refresh file list
    - **Files on Server** table (`el-table`) with columns: Action (Select/Selected button), Status, File Name, Upload Time, Headers on Line (`el-input-number`), Content Encoding (dropdown, hidden for xlsx), Size, Other Actions (re-parse, delete)
    - Clicking "Select" calls `store.selectFile()` then `store.parseFile()`
  - **Step 2 (Map Columns)**:
    - Horizontal scrollable table layout:
      - Row 1: file column headers
      - Row 2: `el-select` dropdown per column with `PEOPLE_TARGET_FIELDS` + "(ignore)" option
      - Rows 3-7: first 5 preview data rows (read-only)
    - Green highlighting on mapped columns via dynamic CSS class
    - Auto-mapping pre-selects dropdowns from `parsedResult.autoMappings`
    - Collapsible reference sections for TallyJ fields and eligibility status values
    - "Save Mapping" button calls `store.saveMapping()`
- Style with `<style lang="less">` using nested CSS under component root class
- Verify: `cd frontend && npx vue-tsc --noEmit`

### [ ] Step 6: Frontend PeopleImportPage — Step 3 (Import) and wiring

Complete the import wizard step, add route, update PeopleManagementPage.

- **Step 3 (Import)** in `PeopleImportPage.vue`:
  - Summary panel: number of data rows, mapped fields list
  - Validation: warn if FirstName or LastName not mapped
  - "Import now" button calls `store.executeImport()`
  - Progress bar bound to `store.importProgress` (SignalR-driven)
  - Results summary after import: added, skipped, warnings, time elapsed
  - "Delete All People" button with guards (disabled if ballots exist or people have voting status), confirmation dialog
  - People count display at bottom of page
- Add SignalR methods in `frontend/src/services/signalrService.ts`:
  - `connectToPeopleImportHub()` — connects to `/hubs/people-import`
  - `joinPeopleImportSession(electionGuid)` — invokes `JoinImportSession`
  - `leavePeopleImportSession(electionGuid)` — invokes `LeaveImportSession`
- Add route in `frontend/src/router/router.ts`:
  - `elections/:id/people/import` → `PeopleImportPage.vue` (under MainLayout, requiresAuth)
- Modify `frontend/src/pages/people/PeopleManagementPage.vue`:
  - Change "Import People" dropdown action from opening `showImportDialog` to `router.push(\`/elections/${electionGuid}/people/import\`)`
  - Remove the import dialog template and related refs/methods (cleanup dead code)
- Add i18n keys in `frontend/src/locales/en/people.json` for all new import page labels
- Verify: `cd frontend && npx vue-tsc --noEmit`

### [ ] Step 7: Final verification and cleanup

Run full build, type checks, and tests to ensure everything works.

- `cd backend && dotnet build && dotnet test`
- `cd frontend && npx vue-tsc --noEmit`
- `cd frontend && npm run test:run` (if existing tests)
- Review for any dead code left from old import dialog
- Verify all new files follow existing code conventions (no scoped styles, script/template/style order per AGENTS.md)
