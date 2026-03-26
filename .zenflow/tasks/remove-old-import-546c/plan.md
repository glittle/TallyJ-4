# Spec and build

## Configuration
- **Artifacts Path**: `.zenflow/tasks/remove-old-import-546c`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: 1a3e1b8b-6842-4715-a79e-0f9e7388a1f3 -->

Spec saved to `.zenflow/tasks/remove-old-import-546c/spec.md`.

---

### [x] Step: Remove CSV Import Feature
<!-- chat-id: de2b804b-2893-4a84-b4a7-51888655c90e -->

Remove all CSV ballot import code:
- Delete `src/pages/ballots/BallotImportPage.vue`
- Delete `src/services/importService.ts`
- Delete `src/stores/importStore.ts`
- Delete `src/types/Import.ts`
- Remove the `ballots/import` route from `src/router/router.ts`
- Remove the "Import Ballots" button and `handleImport` function from `src/pages/ballots/BallotManagementPage.vue`
- Remove all `ballots.import.*` locale keys from `src/locales/en/ballots.json`
- Run `npm run check` and `npm run validate:i18n` to verify

---

### [x] Step: Create CDN Ballot Import Page and Wire It Up
<!-- chat-id: a482db7c-de8c-4d15-8865-445b5ef7d508 -->

- Add `ballots.cdnImport.*` locale keys to `src/locales/en/ballots.json`
- Create `src/pages/ballots/CdnBallotImportPage.vue`:
  - Back-navigation header to Ballot Management
  - Explanation section describing what the XML CDN ballot import does
  - `el-upload` accepting `.xml` only (auto-submit on file selection)
  - Loading state while importing
  - Result display: ballots created, votes created, warnings, errors
- Add route `elections/:id/ballots/cdn-import` → `CdnBallotImportPage.vue` in `router.ts`
- Add "Import CDN Ballots" button in `BallotManagementPage.vue` navigating to that route
- Remove `importCdnBallots` function and Quick Actions button from `ElectionDetailPage.vue`
- Run `npm run check`, `npm run test:run`, `npm run validate:i18n`
- Write report to `.zenflow/tasks/remove-old-import-546c/report.md`
