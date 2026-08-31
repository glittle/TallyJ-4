# Election State Management & Teller Coordination

## Status: active
## Evidence: confirmed (issue #172)

“Move all tellers to this state” and related coordination must be reliable. Multi-teller environments are the normal real-world case, not an edge case.

### Why it matters
State transitions affect what every teller can see and do. Silent or partial failures create inconsistent views across machines and can lead to divergent ballot sets.

### Design posture
Treat state changes as high-consequence operations. Prefer clear, atomic transitions and strong feedback over optimistic updates.

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

## Session Teller 1/2 on an open ballot

**Status:** active  
**Evidence:** confirmed (issue #287)

Teller 1 and Teller 2 shown while a ballot is open are the same browser-session inputs as on the ballot listing (localStorage via `useActiveTellers`). Changing them on the ballot updates those session globals; they are not editors of that ballot’s stored `teller1`/`teller2` fields.

**Rejected alternative:** treat the ballot metadata names as per-ballot fields to save on that record. The listing already uses session-global tellers (who is at this keyboard now). Opening a ballot already stamps the current session tellers onto the record; the names on screen need to stay that same session setting so the teller can change them without closing the ballot.

Location stays read-only on the open ballot in this slice. Adding a name to the election-wide teller list, SignalR to other computers, and admin delete on the Tellers page are a later #287 slice.

Implementation:
- `useActiveTellers` — shared reactive session state over `activeTellerStorage`
- `ActiveTellerSelector` on the listing and as the Teller 1/2 cells in `BallotEntryPanel`
