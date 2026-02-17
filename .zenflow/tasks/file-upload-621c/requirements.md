# PRD: People File Upload & Import

## Overview

Replace the current simple file-upload dialog in PeopleManagementPage with a dedicated multi-step import page. The new flow lets users upload a file (CSV, TAB-delimited, or XLSX), map columns from the file to TallyJ person fields, preview data, and then import people into the election — all without re-uploading the file between steps. This is a modernised version of the v3 `/Setup/ImportCsv` page.

## Goals

- Support XLSX and TAB-delimited files in addition to CSV.
- Provide a clear 3-step workflow: Upload → Map Columns → Import.
- Store uploaded files server-side (in the `ImportFile` database table) so users can return days later to finish mapping/importing.
- Support multiple uploaded files per election; user selects which file to work with.
- Auto-detect column mappings where possible; allow manual override.
- Deduplicate on import: match by Bahá'í ID if present, otherwise by First+Last name exact match.
- Provide progress feedback and a summary of results after import.

## User Flow

### Page Location

Dedicated page at route `/elections/:id/people/import`. Accessible from PeopleManagementPage via the existing "Import People" bulk action (which will navigate to this page instead of opening a dialog).

### Step 1: Upload File

- User sees a drag-and-drop upload zone (or file picker button).
- Accepted formats: `.csv`, `.tsv`/`.tab`/`.txt` (tab-delimited), `.xlsx`.
- Max file size: 10 MB.
- On upload, file is sent to the backend and stored in the `ImportFile` table (binary content, metadata).
- **Files on Server** table shows all previously uploaded files for this election:
  - Columns: Action (Select/Selected), Status, File Name, Upload Time, Headers on Line (editable, default 1), Content Encoding (dropdown for text files, hidden for XLSX), Size, Other Actions (re-parse, delete).
  - User can select which file to work with by clicking "Select".
  - The selected file's headers and preview data are loaded for Step 2.
- Content encoding for text files defaults to UTF-8 with auto-detection. User can override to other encodings (e.g., Windows-1252) if characters look wrong.

### Step 2: Map Columns

- Displayed once a file is selected and parsed.
- Shows a mapping table:
  - **Header row**: file column headers from the uploaded file.
  - **Mapping row**: a dropdown per column letting the user select which TallyJ field to map to, or "(ignore)".
  - **Preview rows**: first 5 data rows from the file (read-only) so the user can verify the mapping makes sense.
- Auto-mapping: on initial load, the system attempts to match file column headers to TallyJ fields by name similarity (case-insensitive, ignoring spaces/underscores). User can override any mapping.
- Column mappings are saved to the `ImportFile.ColumnsToRead` field (JSON) so they persist if the user leaves and returns.

#### TallyJ Target Fields

| Field | Required | Description |
|---|---|---|
| First Name | Required | The individual's first/given name. |
| Last Name | Required | The individual's last name/surname. |
| Bahá'í ID | | Useful for reporting and deduplication. |
| Eligibility Status | | Must exactly match a known reason description or code. See below. |
| Area | | Geographical area. Useful for reporting. |
| Email | | Email address. Needed for Online Voting. |
| Phone | | Phone number. Needed for Online Voting. |
| Other Names | | Other names this person may be known by. |
| Other Last Names | | Other last names this person may be known by. |
| Other Info | | Other distinguishing information to identify this person. |

#### Eligibility Status Reference

The page displays (collapsible) the list of valid Eligibility Status values grouped by category, matching the `IneligibleReasonEnum` definitions:

- **Can Vote and be Voted For**: Eligible (blank/empty)
- **Cannot Vote, Cannot be Voted For**: Under 18 years old, Resides elsewhere, Moved elsewhere recently, Not in this local unit, Deceased, Not a delegate and on other Institution, Not a registered Bahá'í, Rights removed (entirely), Other (cannot vote or be voted for)
- **Can Vote (but cannot be voted for)**: Youth aged 18/19/20, On other Institution (e.g. Counsellor), By-election: On Institution already, Tie-break election: Not tied, Rights removed (cannot be voted for), Other (can vote but not be voted for)
- **Cannot Vote (but can be voted for)**: Not a delegate in this election, Rights removed (cannot vote), Other (cannot vote but can be voted for)

Also displays **Recommended Status Settings** guidance for different election types (local Assembly, tie-break, by-election, National Convention).

### Step 3: Import People

- User reviews a summary: number of data rows to process, mapped fields.
- Validation before import:
  - At least First Name and Last Name must be mapped.
  - Warn if no file is selected.
- **"Import now"** button starts the import.
- Progress indicator during import (can use SignalR for real-time updates on large files).
- **Deduplication logic**:
  - If a Bahá'í ID column is mapped and a row has a Bahá'í ID value: match against existing people by Bahá'í ID only. If found, skip (do not update). If not found, create new person.
  - If no Bahá'í ID is mapped or the row's Bahá'í ID is empty: match by exact First Name + Last Name (case-insensitive). If found, skip. If not found, create new person.
- **Eligibility Status handling**:
  - If an Eligibility Status column is mapped, the value must exactly match a known `IneligibleReasonEnum` description or code. Unrecognized values are treated as eligible (blank) with a warning.
- After import, display results summary:
  - People added (count)
  - People skipped as duplicates (count)
  - Rows with warnings (list)
  - Total data lines processed
  - Time taken
- The `ImportFile` status updates: Uploaded → Mapped → Imported.
- Current people count displayed at bottom of page.

### Delete All People Records

- Button available on the import page.
- **Disabled** if any ballots exist in the election.
- **Disabled** if any people have a voting status set (i.e., have been checked in at Front Desk).
- When enabled, shows a confirmation dialog before proceeding.
- On confirmation, deletes all Person records for the election.
- Updates the displayed people count.

## Non-Functional Requirements

- File parsing (XLSX, CSV, TAB) must happen server-side to support the stored-file workflow.
- XLSX parsing requires a server-side library (e.g., EPPlus, ClosedXML, or similar).
- Large files (thousands of rows) should import without timeout — use batched database writes and SignalR progress updates.
- The page should be responsive and work on tablet-sized screens.

## Out of Scope

- Ballot import (already handled by existing `ImportController` / `BallotImportPage`).
- Updating existing people on re-import (v3 also doesn't do this — duplicates are skipped).
- Export functionality (already exists in PeopleManagementPage).

## Existing Code to Reuse or Replace

| Component | Current State | Action |
|---|---|---|
| `ImportFile` entity | Exists with correct schema | Reuse |
| `ImportController` | Currently handles ballot imports only | Extend with people import endpoints |
| `ImportService` | Ballot-specific CSV parsing | New `PeopleImportService` needed |
| `PeopleManagementPage` import dialog | Simple file upload dialog | Replace with navigation to new page |
| `importStore` (Pinia) | Ballot import SignalR state | Extend or create `peopleImportStore` |
| `IneligibleReasonEnum` | Complete with all codes | Reuse — `GetByDescription` and `GetByCode` for eligibility matching |
| `CreatePersonDto` | All person fields | Reuse for creating people during import |
| `IPeopleService` | CRUD operations | Extend with bulk create / duplicate check |

## Assumptions

- The `ImportFile.Contents` column (`image` binary type) is adequate for storing files up to 10 MB.
- `ImportFile.ColumnsToRead` (string) will store JSON mapping configuration.
- `ImportFile.ProcessingStatus` will track: `Uploaded`, `Mapped`, `Imported`.
- `ImportFile.FileType` will store: `csv`, `tab`, `xlsx`.
- `ImportFile.CodePage` stores the selected encoding code page number for text files.
- `ImportFile.FirstDataRow` stores which row (1-based) contains the first data row (header row number).
- Auto-mapping uses case-insensitive substring/alias matching (e.g., "FirstName", "First Name", "First_Name", "Given Name" all map to First Name).
