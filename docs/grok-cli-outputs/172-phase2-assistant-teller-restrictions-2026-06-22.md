# Issue 172 Phase 2 — Assistant Teller Page Restrictions

**Branch:** `cli5`  
**Date:** 2026-06-22  
**Scope:** Frontend navigation filtering + route guards for assistant tellers by `ElectionStage`. No backend authorization, SignalR, or styling changes.

## Summary

Introduced a single source of truth for assistant-teller access rules and applied them to the sidebar menu, in-component redirect guard, and router `beforeEach`. Full tellers and election officials are unaffected — they continue to see all four stage groups and every page.

## Access Rules (assistant tellers only)

| Stage | Menu items | Allowed routes |
|-------|------------|----------------|
| **SettingUp** | None (restricted) | `/elections/:id` (landing only) |
| **GatheringBallots** | Front Desk | `/elections/:id/frontdesk` |
| **ProcessingBallots** | None (entry is per-ballot) | `/elections/:id/ballots/:ballotId/entry`, plus `/elections/:id` as a coordinator-link fallback |
| **Finalized** | Election Details, Show Final Results | `/elections/:id`, `/elections/:id/results`, `/elections/:id/presentation` |

**Full-teller-only pages** (hidden from assistant tellers at all stages): Ballot Management (`/ballots`), tally/monitor/reporting admin tools, Setting Up configuration pages, and all other election routes not listed above.

## New / Modified Files

| File | Change |
|------|--------|
| `frontend/src/domain/assistantTellerAccess.ts` | **New** — `isAssistantTeller`, `getAssistantTellerMenuPages`, `isAssistantTellerRouteAllowed`, `getAssistantTellerRedirectPath` |
| `frontend/src/domain/electionStages.ts` | Added `Finalized` to `STAGES` / `STAGE_META` / `STAGE_PAGES`; marked `ballots` as `adminOnly` |
| `frontend/src/components/nav/StageGroupedSidebarMenu.vue` | Uses access helpers for menu + redirect guard |
| `frontend/src/components/AppSidebar.vue` | Uses `isAssistantTeller()` |
| `frontend/src/router/router.ts` | Route-level guard fetches election stage and blocks restricted paths |
| `frontend/src/pages/TellerJoinPage.vue` | Post-login redirect uses stage-appropriate landing path |
| `frontend/src/locales/en/results.json` | Added `results.showFinalResults` |
| `frontend/src/locales/en/elections.json` | Added `Finalized_short`, `stageNav.group.Finalized` |

## Architecture

```
assistantTellerAccess.ts  ← single rule set
        ↓
StageGroupedSidebarMenu   ← menu filtering + client redirect
router.beforeEach         ← route-level enforcement
TellerJoinPage            ← correct initial landing per stage
```

Detection: `name === "Teller" && authMethod === "AccessCode"` (unchanged from Phase 1 patterns).

Full tellers: `StageGroupedSidebarMenu` admin branch still iterates all `STAGES` and renders full `STAGE_PAGES` per group — no filtering applied.

## Tests

**41 tests passed** (filter: `assistantTellerAccess`, `electionStages`, `StageGroupedSidebarMenu`):

- Stage-to-menu mapping for all four stages
- Route allow/deny matrix (SettingUp, GatheringBallots, ProcessingBallots, Finalized)
- Redirect targets for restricted navigation
- Ballot Management marked `adminOnly`
- Admin sidebar still renders 4 stage groups
- ProcessingBallots assistant menu is empty; Finalized shows 2 items

Also ran `npm run validate:i18n` and `npm run check` — clean.

## Known Limitations / Deferred

- **ProcessingBallots entry landing:** No fixed `/ballot-entry` route; assistant tellers reach entry via per-ballot URLs. Election landing is allowed as a temporary fallback while awaiting a ballot link.
- **Backend authorization:** API endpoints remain unrestricted; frontend guards are UX safety nets only.
- **SignalR stage propagation:** Existing `electionStore.currentStage` reactivity already updates menus when stage changes (no changes made this phase).
- **Assistant teller stage indicator:** Sidebar stage header remains hidden for tellers (`AppSidebar` `v-if="!isTeller"`).