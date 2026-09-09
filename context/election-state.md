# Election State Management & Teller Coordination

## Status: active
## Evidence: confirmed (issue #172)

“Move all tellers to this state” and related coordination must be reliable. Multi-teller environments are the normal real-world case, not an edge case.

### Why it matters
State transitions affect what every teller can see and do. Silent or partial failures create inconsistent views across machines and can lead to divergent ballot sets.

### Design posture
Treat state changes as high-consequence operations. Prefer clear, atomic transitions and strong feedback over optimistic updates.

## Lock after analysis is the Finalized stage

**Status:** active  
**Evidence:** inferred (issue #172 names; implementation in `ElectionService.ChangeElectionStageAsync`); people/ballot write gate confirmed by issue #308

There is no separate `Locked` flag. After analysis is complete and counts reconcile, advancing to **Finalized** is the lock:

- Finalization is rejected until `ElectionStageFinalizationReadiness` is ready (analysis present and reportable, no blocking ballots, no unresolved ties, counts reconcile).
- Leaving Finalized requires `ConfirmLeavingFinalized`. The StageControl UI does not send that flag, so a FullTeller click away from Finalized stays locked and shows the confirmation error.
- Accept-all online ballots is refused while Finalized (`monitoring.acceptAll.finalized`).
- People, ballot, vote, Front Desk roll, people-import execute / delete-all, and ballot/CDN import mutations are refused while Finalized (`elections.finalizedWriteBlocked`). Reads stay open. Leaving Finalized still requires `ConfirmLeavingFinalized` (#309 is the in-UI unlock confirm; not this gate).

**Rejected alternative (not implemented):** a dedicated lock bit plus a “Move all tellers” command. Stage change + SignalR `statusChanged` is the coordination path.

**Rejected alternative:** invent a second lock type or middleware. The existing `ElectionStage.Finalized` check (same as Accept-all) is the lock; `ElectionFinalizedWriteGuard` is only a shared helper.

**What stays writable on purpose:** election settings, locations, teller names, computers, analysis/results, online voter submit (window-based, not stage-based), test-election reset, and people-import file upload/mapping (those do not change the roll until execute). GetPersonDetails does not mint a new kiosk code after Finalized.

## “Move all tellers to this state” is the stage broadcast

**Status:** active  
**Evidence:** inferred (issue #172 names; no separate move-tellers API)

Changing stage is the move. `ChangeElectionStageAsync` persists the stage and broadcasts `statusChanged` on MainHub. Remote `electionStore` clients update `currentStage`. GuestTellers are redirected to that stage’s work page; FullTellers are notified and stay on their current page (they can still open other stage groups).

There is no separate “Move all tellers to this state” button or endpoint.

## GuestTeller page on stage change

**Status:** active  
**Evidence:** confirmed (issue #242)

When election stage changes, GuestTellers are redirected to the stage’s primary work page (same idea as “move all tellers to this state” for navigation):

| Stage | GuestTeller destination |
|-------|-------------------------|
| GatheringBallots | Front Desk |
| ProcessingBallots | Enter Ballots (`/ballots`) |
| SettingUp / Finalized | election landing (and results links when Finalized) |

**Rejected alternative:** keep ProcessingBallots GuestTellers on election landing only, with ballot entry only via per-ballot deep links (phase-2 restriction). That left guests stuck on Front Desk after a stage advance (or with no stage work page), while Gathering already auto-moved them to Front Desk.

**Reason:** symmetric stage landing for guests; Enter Ballots is the Processing counterpart to Front Desk. Tally/monitor/results stay FullTeller-only (`adminOnly`). Per-ballot `/ballots/:id/entry` routes remain allowed.

Implementation:
- Rules: `guestTellerAccess.ts`
- Live redirect: `useGuestTellerStageRedirect` in `MainLayout` (not only the lazy sidebar menu)
- Secondary: sidebar watch + router `beforeEach`
- Stage source: MainHub `statusChanged` → `electionStore.currentStage`
- Main hub membership must stay for the whole election session (`electionStore`); ballots/people pages only join/leave FrontDesk (see `context/realtime.md`)

## Teller 1/2 names vs the election teller list

**Status:** active  
**Evidence:** confirmed (issue #287)

Teller 1 and Teller 2 on the ballot listing and an open ballot are **browser-session** selections (shipped in #290). The **election teller list** is separate: typing a name adds it to `Teller` for that election; both dropdowns show that list alphabetically; SignalR `tellersChanged` (MainHub) updates other teller computers; clearing a dropdown does not remove the name. Only the admin Tellers page deletes a name.

**Rejected alternative:** treat clear as delete. Rejected — operators reuse the same names across computers and ballots; clear only means “this workstation is not attributing that person right now.”

## Session Teller 1/2 on an open ballot

**Status:** active  
**Evidence:** confirmed (issue #287)

Teller 1 and Teller 2 shown while a ballot is open are the same browser-session inputs as on the ballot listing (localStorage via `useActiveTellers`). Changing them on the ballot updates those session globals; they are not editors of that ballot’s stored `teller1`/`teller2` fields.

**Rejected alternative:** treat the ballot metadata names as per-ballot fields to save on that record. The listing already uses session-global tellers (who is at this keyboard now). Opening a ballot already stamps the current session tellers onto the record; the names on screen need to stay that same session setting so the teller can change them without closing the ballot.

Location stays read-only on the open ballot in this slice. Adding a name to the election-wide teller list, SignalR to other computers, and admin delete on the Tellers page are a later #287 slice.

Implementation:
- `useActiveTellers` — shared reactive session state over `activeTellerStorage`
- `ActiveTellerSelector` on the listing and as the Teller 1/2 cells in `BallotEntryPanel`
