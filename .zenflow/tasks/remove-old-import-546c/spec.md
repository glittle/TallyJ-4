# Technical Specification: Remove Old Import / Move CDN Ballot Import

## Complexity Assessment

**Medium** — involves removing an existing feature, creating a new dedicated page, updating routing, and updating locale strings.

---

## Technical Context

- **Language/Framework**: Vue 3 + TypeScript (frontend), ASP.NET Core (backend — no changes needed)
- **State management**: Pinia
- **UI library**: Element Plus
- **Routing**: Vue Router
- **i18n**: vue-i18n with English locale files in `src/locales/en/`

---

## Current State

### "Import Ballots" (CSV) — to be removed

- **Page**: `src/pages/ballots/BallotImportPage.vue` — full multi-step CSV import wizard
- **Route**: `elections/:id/ballots/import` → `BallotImportPage.vue` (in `router.ts`)
- **Button**: "Import Ballots" (`Upload` icon) in `BallotManagementPage.vue` → `handleImport()` → navigates to `/elections/:id/ballots/import`
- **Service**: `importService.ts` — `parseCsvHeaders`, `importBallots` (calls `/api/Import/parse-csv-headers` and `/api/Import/importBallots`)
- **Store**: `importStore.ts` — SignalR-based import progress tracking
- **Types**: `Import.ts` — `FieldMapping`, `ImportConfiguration`, `ImportBallotRequest`, `ParseCsvHeaders*`, `IMPORT_TARGET_FIELDS`
- **Locale keys in `ballots.json`**: All `ballots.import.*` keys
- **Backend endpoints**: `POST /api/Import/parse-csv-headers`, `POST /api/Import/importBallots` — **backend untouched**

### "Import CDN Ballots" (XML) — to be moved/expanded

- **Current location**: Quick Actions card in `ElectionDetailPage.vue` (`importCdnBallots()` function, inline file picker)
- **Service**: `electionService.importCdnBallots(electionGuid, file)` — calls `POST /api/Import/importCdnBallots/{electionGuid}`
- **Type**: `ImportResultDto` in `Election.ts` (`success`, `errors`, `warnings`, `ballotsCreated`, `votesCreated`, `totalRows`, `skippedRows`)
- **Locale keys**: `elections.importCdnBallots`, `elections.importCdnBallotsSuccess`, `elections.importCdnBallotsError`
- **Backend**: `BallotImportController.ImportCdnBallots` — fully implemented, no changes needed

---

## Implementation Approach

### 1. Remove CSV Import Feature

- Remove the "Import Ballots" button from `BallotManagementPage.vue` (the `handleImport` function and `<el-button>` with `Upload` icon)
- Remove the route `elections/:id/ballots/import` from `router.ts`
- Delete `src/pages/ballots/BallotImportPage.vue`
- Remove CSV-import-specific locale keys from `src/locales/en/ballots.json` (all `ballots.import.*` keys)
- Keep `importStore.ts`, `importService.ts`, and `Import.ts` **only if** they are used elsewhere. If not, remove them too.
  - `importStore.ts` — only used in `BallotImportPage.vue` → **remove**
  - `importService.ts` — only used in `BallotImportPage.vue` → **remove**
  - `Import.ts` types — only used in `BallotImportPage.vue` and `importService.ts` → **remove** (verify no other imports)
- Do **not** touch backend CSV import endpoints (they may be used by other callers)

### 2. Create New "Import CDN Ballots" Page

New file: `src/pages/ballots/CdnBallotImportPage.vue`

This page will:
- Show a header with back-navigation to `BallotManagementPage`
- Show an explanation section describing what the XML import does (what CDN ballots are, what the file contains, what will be created — ballots and votes)
- Show an Element Plus `el-upload` (drag-and-drop + button) accepting `.xml` files only
- On file selection, immediately submit to backend via `electionService.importCdnBallots()`
- Show import status/result:
  - Loading spinner while importing
  - Success panel: ballots created, votes created, any warnings
  - Error panel: error list
- "Back to Ballot Management" button after completion

### 3. Add Route

In `router.ts`, add:
```
path: "elections/:id/ballots/cdn-import"
component: () => import("../pages/ballots/CdnBallotImportPage.vue")
meta: { titleKey: "ballots.cdnImport.title" }
```

### 4. Move Button in BallotManagementPage

Replace the old "Import Ballots" button in `BallotManagementPage.vue` with an "Import CDN Ballots" button that navigates to `/elections/:id/ballots/cdn-import`.

### 5. Remove Button from ElectionDetailPage

Remove the `importCdnBallots()` function and its `<el-button>` from `ElectionDetailPage.vue` Quick Actions. Also remove the `Upload` icon import if it becomes unused.

### 6. Locale Updates

In `src/locales/en/ballots.json`:
- Remove all `ballots.import.*` keys
- Add new keys:
  - `ballots.cdnImport.title` — "Import CDN Ballots"
  - `ballots.cdnImport.button` — "Import CDN Ballots"
  - `ballots.cdnImport.description` — explanation text about what the CDN XML import does
  - `ballots.cdnImport.uploadPrompt` — "Upload XML File"
  - `ballots.cdnImport.xmlOnly` — "Only XML files are supported"
  - `ballots.cdnImport.importing` — "Importing..."
  - `ballots.cdnImport.success` — "Import completed successfully"
  - `ballots.cdnImport.ballotsCreated` — "Ballots created: {count}"
  - `ballots.cdnImport.votesCreated` — "Votes created: {count}"
  - `ballots.cdnImport.warnings` — "Warnings"
  - `ballots.cdnImport.errors` — "Errors"
  - `ballots.cdnImport.failed` — "Import failed"

---

## Source Code Changes

| File | Change |
|------|--------|
| `src/pages/ballots/BallotImportPage.vue` | **Delete** |
| `src/pages/ballots/BallotManagementPage.vue` | Replace "Import Ballots" button with "Import CDN Ballots" |
| `src/pages/ballots/CdnBallotImportPage.vue` | **Create** — new CDN import page |
| `src/pages/elections/ElectionDetailPage.vue` | Remove `importCdnBallots` function and Quick Actions button |
| `src/router/router.ts` | Remove `ballots/import` route, add `ballots/cdn-import` route |
| `src/services/importService.ts` | **Delete** (only used by removed feature) |
| `src/stores/importStore.ts` | **Delete** (only used by removed feature) |
| `src/types/Import.ts` | **Delete** (only used by removed feature) |
| `src/locales/en/ballots.json` | Remove `ballots.import.*`, add `ballots.cdnImport.*` keys |

**No backend changes required.**

---

## Data Model / API Changes

None — the existing `POST /api/Import/importCdnBallots/{electionGuid}` endpoint is reused as-is. The `ImportResultDto` type in `Election.ts` is kept (used by `electionService.ts`).

---

## Verification Approach

```bash
cd frontend
npm run check      # TypeScript type checking
npm run test:run   # Vitest unit tests
npm run validate:i18n  # i18n key validation
```

Key manual checks:
1. Ballot Management page no longer shows "Import Ballots" (CSV), shows "Import CDN Ballots" instead
2. Clicking "Import CDN Ballots" navigates to the new CDN import page
3. The new page shows explanation text, XML-only upload widget
4. Uploading a valid XML file triggers import and shows result
5. ElectionDetailPage Quick Actions no longer shows "Import CDN Ballots"
6. Route `/elections/:id/ballots/import` no longer exists (returns 404 / redirect)
