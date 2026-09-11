# People import workflow

## Three explicit actions, not a wizard

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #241; maintainer review of the Import People page  
**Revisit when:** import gains a fourth action (e.g. dry-run / merge policy) or file reuse is dropped

People import is three server operations on a persisted file: upload/parse, save column mapping (`ColumnsToRead`), then execute the load. The page shows those as a **single-page pipeline** (choose file → map columns → load people), each with one primary verb.

**Rejected alternative:** a Next/Previous wizard that hides earlier stages. It implied a one-shot interview, while files stay on the server and can be remapped or reloaded. Extra chrome (**Select**, **Save Mapping**, **Next**) also let users reach “Import Now” without persisting the mapping, which fails with `import.errors.noMappings`.

**Rejected alternative:** one-click upload-and-import. Encoding, header row, and column matching still need a human check.

Upload auto-selects the new file and scrolls to mapping. Mapping is **file columns as rows** (each source column → one TallyJ field); already-used TallyJ fields are disabled in the other dropdowns. Required First Name and Last Name are a checklist above the table, not asterisks on a full destination list. Load people is enabled only after mapping is saved. **Delete all people** lives on People Management, not beside the load button.

**Rejected alternative:** TallyJ fields as rows (destination-first). Rejected after using it — users think in terms of the file they just uploaded, and most TallyJ fields stay unused.

## Parse preview is per-column samples, not the first N rows

**Status:** active  
**Evidence:** confirmed  
**Source:** mapping UI review; `2021-04-22-with units.csv` has empty MiddleName / FormerName / Nickname on the first rows  
**Revisit when:** preview needs more than three samples, or parse should stream without loading `ImportFile.Contents`

The file bytes are already stored on the import row. The parse endpoint still counts every data row, but it only returns up to three **non-empty** samples **per column**, taken from whichever later rows have values. Once every column already has three samples, remaining rows are counted without reading their XLSX cells or splitting their CSV/TSV lines. Those samples are packed into `PreviewRows` as virtual rows (same column index, not the same source row). Import still parses the full file separately.

**Rejected alternative:** send every data row in the parse response. Unnecessary payload, and the first three rows hide sparse columns.

## Header auto-match ignores punctuation and accents

**Status:** active  
**Evidence:** confirmed  
**Source:** `"Baha'i ID"` failed to map to TallyJ Baha'i ID because aliases were compared as raw strings (`baha'iid` ≠ `bahaiid` / `baha'i id`)

Matching strips letters/digits only after Unicode decomposition, then compares aliases the same way. Apostrophes, curly quotes, and accents (`Bahá'í ID`) all collapse to `bahaiid`.

Each TallyJ field is assigned to at most one file column. If two headers are valid matches, the more specific one wins (`Baha'i ID` over `ID`); the other stays unmapped. Equal scores keep the earlier column.

## Mapping must be saved before load

**Status:** active  
**Evidence:** confirmed  
**Source:** `PeopleImportService.ImportPeopleAsync` requires `ColumnsToRead`

The execute endpoint reads mappings from the file row, not from the browser. Confirm mapping is the step that writes that JSON. Changing mapping, encoding, or header row clears the confirmed state so load cannot run on stale server data.

**Email**, **phone**, and **Baha'i ID** must be unique. Duplicate names are allowed so two people with the same name can be told apart with Other Info. A skipped unique-field row reports the file line it matches, or that it matches an existing person.

Skip messages use the spreadsheet's own row number (Excel row 12 after headers on row 6), not a count of data rows. `FirstDataRow` is the header row only; parse already excludes it, so import must not skip that many rows again.

## Hard spaces from Excel/Word are regular spaces

**Status:** active  
**Evidence:** inferred (v3 import bug class named on #170; Excel/Word emit U+00A0)  
**Source:** issue #170  
**Revisit when:** another Unicode space (e.g. ideographic) shows up in real files

Import cells replace NBSP (`U+00A0`) and narrow NBSP (`U+202F`) with a normal space, then trim. A cell that is only hard spaces is empty — missing First/Last Name skips the row; empty eligibility stays fully eligible. Mid-name hard spaces become a real space (`Mary Jane`) so search and uniqueness match what the teller sees.

**Rejected alternative:** leave hard spaces in the stored name. They look identical in the UI and then fail Front Desk search and unique-field matching.

**Rejected alternative:** strip every Unicode space category. That would also delete intended separators in some names; only the Excel/Word hard-space pair is rewritten.

## Empty files and invalid lines do not abort the load

**Status:** active  
**Evidence:** confirmed (controller already refused 0-byte upload; import skips validation failures without `errorsFound`)  
**Source:** issue #170; `PeopleImportController.UploadFile`; `PeopleImportService.ImportPeopleAsync`  
**Revisit when:** import gains a fail-fast / all-or-nothing option

A 0-byte upload is refused (`No file provided`) in both the controller and `UploadFileAsync`. A file that parses to no rows (empty bytes, whitespace, or headers only) can still be mapped; execute succeeds with zero people added.

Invalid data rows (blank names, hard-space-only names, jagged/unquoted junk) increment `PeopleSkipped`, log a line error, and leave valid rows in the same file imported. Unexpected exceptions still roll back the batch (`errorsFound`).

**Rejected alternative:** treat any skipped row as a failed import and roll back. Duplicate unique fields and unrecognized eligibility already commit the good rows; missing-name and empty-line cases follow that.

## People list export is the package + voter reports

**Status:** active  
**Evidence:** inferred (no People-page CSV download in v4; #170 asked to test export, not add one)  
**Source:** issue #170  
**Revisit when:** tellers need a CSV that round-trips through Import People

v4 does not download a people CSV from People Management. The people list leaves the system as:

- Election JSON package (`ExportElectionToJsonAsync` `people` array) — full records for backup / move
- Voter reports (`Voters`, `AllCanReceive`, `VoterEmails`, `ChangedPeople`, …) — on-screen + browser print
- Tally report export API (`/api/report-exports`) — PDF / Excel / CSV of results (elected names, overview), not the roll

The Reporting page only offers Print; the unused `reporting.exportCSV` strings are leftovers.

**Rejected alternative:** add a People-page CSV export in this #170 slice. Import already accepts many column layouts; a new download would be a product feature, not the remaining test gap.

## Eligibility import uses person-form codes, not four invented statuses

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #262  
**Revisit when:** eligibility reasons gain a new group, or import starts accepting aliases / case-insensitive codes

The Import People guide used to list Eligible / Ineligible / Under Age / Duplicate. Those values are not the person-form reasons and are not stored.

The file may include an eligibility column mapped to Eligibility Status. Accepted values are the same person-form statuses: a code (`V04`, `X02`) or the exact description in the current UI language (English descriptions also match). Empty cells mean fully eligible. Only the reason short code is stored.

The page shows those statuses in a copyable reference popup (code and description each have a copy icon; text stays selectable). Internal vote-only reasons (`U01`, `U02`) are not listed and are not accepted on a person row.

**Rejected alternative:** treat an unrecognized value as eligible and only warn. That silently imported the wrong rights. Unrecognized text now skips the row and logs it, same as a duplicate unique field.

**Rejected alternative:** match descriptions case-insensitively or accept `v04`. The popup copies the exact code and localized text; fuzzy matching would hide typos.
