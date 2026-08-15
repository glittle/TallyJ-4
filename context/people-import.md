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
