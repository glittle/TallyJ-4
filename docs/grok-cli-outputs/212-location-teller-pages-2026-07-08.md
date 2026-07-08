# Issue 212 — Improve Location and Teller Edit Pages

**Date:** 2026-07-08  
**Scope:** Apply the People Management drawer pattern to Location and Teller list pages.

## Summary

Updated the Voting Locations and Tellers management pages to match the People Management UX:

- The primary identifier in the first column (location name / teller name) is now a clickable link.
- Clicking opens an Element Plus side drawer (`direction="rtl"`, `size="50%"`) containing the edit form.
- The Actions column has been removed from both tables.
- Delete has been moved into the drawer form, separated at the bottom with a divider.
- Dialog-based forms were replaced with drawer-ready form components.

## Changes

### 1. Location management (`LocationsListPage.vue`)

- Replaced `LocationFormDialog` with `LocationForm` inside an `el-drawer`.
- Location name column uses `el-button type="primary" link` to open edit drawer.
- Removed the fixed-right Actions column (Edit / Delete buttons).
- Add Location opens the same drawer in create mode.
- Drawer title uses `locations.editDrawerTitle` for edit mode.

### 2. Teller management (`TellersListPage.vue`)

- Replaced `TellerFormDialog` with `TellerForm` inside an `el-drawer`.
- Teller name column uses `el-button type="primary" link` to open edit drawer.
- Removed the Actions column.
- Replaced hardcoded English strings with `$t()` keys.
- Drawer title uses `teller.editDrawerTitle` for edit mode.

### 3. New form components

| Component                                            | Purpose                                                        |
| ---------------------------------------------------- | -------------------------------------------------------------- |
| `frontend/src/components/locations/LocationForm.vue` | Drawer form with save/cancel actions and bottom delete section |
| `frontend/src/components/tellers/TellerForm.vue`     | Drawer form with save/cancel actions and bottom delete section |

Both forms follow `PersonForm.vue` conventions:

- Props: `electionGuid`, entity (`location` / `teller`), `isEdit`, `showDelete`
- Emits: `success`, `deleted`, `cancel`
- Delete uses `ElMessageBox.confirm` before calling the store

### 4. Removed dialog components

- `frontend/src/components/locations/LocationFormDialog.vue`
- `frontend/src/components/tellers/TellerFormDialog.vue`

### 5. Locale updates (English only)

**`frontend/src/locales/en/locations.json`**

- `locations.editDrawerTitle`: "Edit {name}"

**`frontend/src/locales/en/teller.json`**

- `teller.editDrawerTitle`: "Edit {name}"
- `teller.form.delete`: "Delete"
- `teller.confirm.deleteTellerTitle`: "Warning"
- `teller.confirm.deleteTellerMessage`: "Are you sure you want to delete teller \"{name}\"?"
- `teller.confirm.delete`: "Delete"
- `teller.confirm.cancel`: "Cancel"
- `teller.success.tellerDeleted`: "Teller deleted successfully"

## Files touched

| File                                                       | Change                  |
| ---------------------------------------------------------- | ----------------------- |
| `frontend/src/pages/locations/LocationsListPage.vue`       | Drawer listing page     |
| `frontend/src/pages/tellers/TellersListPage.vue`           | Drawer listing page     |
| `frontend/src/components/locations/LocationForm.vue`       | **New** — drawer form   |
| `frontend/src/components/tellers/TellerForm.vue`           | **New** — drawer form   |
| `frontend/src/components/locations/LocationFormDialog.vue` | **Deleted**             |
| `frontend/src/components/tellers/TellerFormDialog.vue`     | **Deleted**             |
| `frontend/src/locales/en/locations.json`                   | Drawer title string     |
| `frontend/src/locales/en/teller.json`                      | Drawer + delete strings |

## Validation

| Command                 | Result                                                                            |
| ----------------------- | --------------------------------------------------------------------------------- |
| `npm run check`         | Pass (tsc + eslint)                                                               |
| `npm run lint -- --fix` | Pass                                                                              |
| `npm run validate:i18n` | Expected warnings for new en-only keys (other locales updated in periodic review) |
| `npm run test:run`      | 381/383 pass; 2 pre-existing failures in `ballotStore.spec.ts` (unrelated)        |

Backend build/tests were not run — contributor has `dotnet watch` running.

## Reference implementation

Pattern copied from:

- `frontend/src/pages/people/PeopleManagementPage.vue` — drawer orchestration
- `frontend/src/components/people/PersonForm.vue` — form actions + bottom delete section
- `frontend/src/components/people/PeopleTable.vue` — clickable name link button

───

On your prompt question: the level you had was enough to implement this. The only additions that would have saved a minute or two were explicit file paths (LocationsListPage.vue, TellersListPage.vue, PersonForm.vue as reference) — but those are easy to discover from the People Management pattern. No need to make it more detailed unless you're handing it to something that won't explore the repo first.
