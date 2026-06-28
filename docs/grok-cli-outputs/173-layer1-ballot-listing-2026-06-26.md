# Issue 173 — Layer 1: Ballot Listing & Entry Page Structure

**Date:** 2026-06-26  
**Scope:** Layout and structure only (no drag reordering, spoiled vote indicators, or auto-save)

## Summary

Refactored the teller ballot listing page into an "Enter Ballots" workflow with a right-side drawer for entry, and moved CDN import to the Election Monitor page.

## Changes

### 1. Page rename: "Ballot Management" → "Enter Ballots"

- Updated `ballots.management` in `frontend/src/locales/en/ballots.json`
- Navigation title (router meta + election stage sidebar) now reads "Enter Ballots"

### 2. Ballot listing refactor (`BallotManagementPage.vue`)

- **Removed** the fixed-right Actions column (Enter Votes, View Votes, Delete)
- **Removed** the "Import CDN Ballots" button from this page
- **Made** the Ballot Code column a primary link button; clicking opens the ballot
- **Added** an Element Plus `el-drawer` (`direction="rtl"`, `size="70%"`) that keeps the ballot list visible behind the drawer
- **Add Ballot** now creates a ballot immediately (default computer code `A`, active tellers from storage) and opens the drawer — no separate form dialog

### 3. New vs existing ballot field visibility

- Introduced `BallotEntryPanel.vue` with a `showMetadata` prop
- **New ballot** (`showMetadata=false`): hides Location, Computer, Teller 1, Teller 2, and Votes summary
- **Existing ballot** (`showMetadata=true`): shows all metadata fields above the inline entry UI
- `BallotEntryPage.vue` simplified to a thin wrapper around `BallotEntryPanel` (standalone route preserved)

### 4. CDN Import moved to Election Monitor

- Added "Import CDN Ballots" button to `MonitoringDashboardPage.vue` header
- `CdnBallotImportPage.vue` success navigation now returns to Election Monitor (`/elections/:id/monitor`)

## Files touched

| File | Change |
|------|--------|
| `frontend/src/pages/ballots/BallotManagementPage.vue` | Drawer listing page |
| `frontend/src/components/ballots/BallotEntryPanel.vue` | **New** — shared entry panel |
| `frontend/src/pages/ballots/BallotEntryPage.vue` | Uses `BallotEntryPanel` |
| `frontend/src/pages/results/MonitoringDashboardPage.vue` | CDN import button |
| `frontend/src/pages/ballots/CdnBallotImportPage.vue` | Back link → monitor |
| `frontend/src/locales/en/ballots.json` | Page title string |
| `frontend/src/domain/__tests__/electionStages.test.ts` | Test label update |
| `frontend/src/pages/ballots/__tests__/BallotManagementPage.spec.ts` | **New** tests |
| `frontend/src/pages/ballots/__tests__/BallotEntryPage.spec.ts` | Updated for panel |

## Out of scope (deferred)

- Drag reordering of votes on the ballot
- Spoiled vote indicator UX improvements
- Auto-save behavior
- Delete / view-votes actions (removed with actions column; may return in a later layer)

## Verification

```bash
cd frontend
npm run check          # passed (vue-tsc + eslint)
npm run test:run -- src/pages/ballots/__tests__/BallotManagementPage.spec.ts \
                    src/pages/ballots/__tests__/BallotEntryPage.spec.ts \
                    src/domain/__tests__/electionStages.test.ts
# 12 tests passed
```

## Manual smoke test

1. Open `/elections/{guid}/ballots` — page title should read **Enter Ballots**
2. Click a ballot code — drawer slides in from the right with metadata + entry UI
3. Click **Add Ballot** — drawer opens without metadata fields
4. Open `/elections/{guid}/monitor` — **Import CDN Ballots** button present
5. Complete or cancel CDN import — returns to Election Monitor