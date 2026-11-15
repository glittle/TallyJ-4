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
**Evidence:** inferred (issue #172 names; implementation in `ElectionService.ChangeElectionStageAsync`); people/ballot write gate and online-submit refusal confirmed by issue #308; tie-break count saves confirmed by issue #432

There is no separate `Locked` flag. After analysis is complete and counts reconcile, advancing to **Finalized** is the lock:

- Finalization is rejected until `ElectionStageFinalizationReadiness` is ready (analysis present and reportable, no blocking ballots, no unresolved ties, counts reconcile, and the online voting window is not currently open).
- When online voting is enabled and the window is currently open (same rule as voter submit / available-elections: `UseOnlineVoting` plus open/close vs now; a null open or close does not close the window), stage change **to** Finalized is refused (`elections.stageChangeError.onlineVotingStillOpen`). Tellers must close the window first. Finalize does not auto-close it. Elections with online voting off, a future-only window, or an already-closed window can still Finalize.
- Leaving Finalized requires `ConfirmLeavingFinalized`. StageControl asks a FullTeller to confirm (`useConfirmDialog`) and only then calls `setStage` with the flag. Cancel leaves the election Finalized (no API call). Guests never see the stage switcher (`AppSidebar` `v-if="!isGuest"`); the stage API still requires `FullTellerAccess`.

**Rejected alternative:** treat the API 409 (`elections.stageChangeError.confirmLeaveFinalized`) as the unlock UX. Rejected — that is an error toast, not a confirm, and StageControl never sent the flag so a click away stayed locked (#309).

**Rejected alternative:** reuse the unused `elections.confirmRevert` string, or auto-send `ConfirmLeavingFinalized` without a dialog. Rejected — leaving Finalized reopens people/ballot writes (#308); the dialog copy must say that, and the API flag must stay a deliberate confirm.
- Accept-all online ballots is refused while Finalized (`monitoring.acceptAll.finalized`).
- People, ballot, vote, Front Desk roll, people-import execute / delete-all, ballot/CDN import mutations, tie-break count saves, and online voter submit (create or update a pending online ballot) are refused while Finalized. Tellers see `elections.finalizedWriteBlocked`; voters see `voting.submit.finalized`. Reads stay open. Unlocking those writes is the confirmed leave-Finalized stage change (#309). Tie-break saves were still accepted after Finalized (#432) because `SaveTieCountsAsync` never called `ElectionFinalizedWriteGuard`. Counts saved before that stage, including an all-elected tie, stay and can still be edited until the election is Finalized.

**Rejected alternative (not implemented):** a dedicated lock bit plus a “Move all tellers” command. Stage change + SignalR `statusChanged` is the coordination path.

**Rejected alternative:** invent a second lock type or middleware. The existing `ElectionStage.Finalized` check (same as Accept-all) is the lock; `ElectionFinalizedWriteGuard` is only a shared helper.

**What stays writable on purpose:** election settings, locations, teller names, computers, re-running analysis and reading results, test-election reset, and people-import file upload/mapping (those do not change the roll until execute). Saving tie-break counts is a result mutation and uses the same teller refusal as people and ballots. Person-detail reads never mint a kiosk code (including after Finalized). Generate/renew is a write and is blocked after Finalized. Online-window open/close still gates submit when the election is not Finalized.

**Rejected alternative:** leave online voter submit writable while Finalized because the online window (not stage) already gates it. Rejected — Finalized is the lock for people/ballot mutations, including creating or updating a pending online ballot.

**Rejected alternative:** auto-close the online window as part of advancing to Finalized. Rejected — the teller must close the window first; Finalize only refuses while it is still open.

## “Move all tellers to this state” is the stage broadcast

**Status:** active  
**Evidence:** inferred (issue #172 names; no separate move-tellers API); FullTeller opt-in confirmed by issue #310

Changing stage is the move. `ChangeElectionStageAsync` persists the stage and broadcasts `statusChanged` on MainHub. Remote `electionStore` clients update `currentStage`.

- **GuestTellers** who are on a page the new stage does not allow are sent to that stage’s primary work page (`useGuestTellerStageRedirect`). A page the stage allows, including Front Desk during Processing, is left alone.
- **FullTellers** stay on their current page and get a toast. The toast includes a **Go there** action that opens the same work page guests land on. They are not auto-navigated: a FullTeller may be mid-ballot or mid-edit.

Work pages (`getStageWorkPagePath`): GatheringBallots → Front Desk; ProcessingBallots → Enter Ballots; SettingUp / Finalized → election landing.

v3 `statusChanged` updated status in place (`site.broadcast`); there is no documented v3 auto-move for known tellers. Issue #310 left the product choice open; v4 keeps stay-put + opt-in.

**Rejected alternative:** auto-navigate FullTellers the same as guests. Rejected — FullTellers often have unsaved ballot/person edits; yanking the route would lose work. Guests have a narrower page set and are meant to follow the stage.

**Rejected alternative:** toast-only with no action (pre-#310). Rejected — FullTellers had no way to follow the stage from the notice itself.

There is no separate “Move all tellers to this state” button or endpoint.

## GuestTeller page on stage change

**Status:** active  
**Evidence:** confirmed (issue #242; Processing Front Desk confirmed by issue #429)

When election stage changes, a GuestTeller who is on a page that stage does not allow is sent to that stage’s primary work page. A page the stage does allow is left alone.

| Stage | GuestTeller pages | Primary destination |
|-------|-------------------|---------------------|
| GatheringBallots | Front Desk only | Front Desk |
| ProcessingBallots | Enter Ballots (`/ballots`, including the open-ballot and entry routes) and Front Desk (`/frontdesk`) | Enter Ballots |
| SettingUp / Finalized | election landing (and results links when Finalized) | election landing |

A guest already on Front Desk stays there when the stage becomes Processing. Gathering still sends a guest who is on Enter Ballots back to Front Desk. FullTellers are unchanged: Front Desk stays in the Gathering group and is not added to the Processing group.

> Superseded 2026-11: ProcessingBallots guests saw only Enter Ballots, and `/frontdesk` redirected to `/ballots`.

**Rejected alternative:** keep ProcessingBallots GuestTellers on election landing only, with ballot entry only via per-ballot deep links (phase-2 restriction). That left guests stuck on Front Desk after a stage advance (or with no stage work page), while Gathering already auto-moved them to Front Desk.

**Rejected alternative:** add Front Desk to `STAGE_PAGES.ProcessingBallots`. That would show it in the owner/admin Processing group. Owners already open Front Desk from the Gathering group at any stage; the change is guest access during Processing only.

**Rejected alternative:** make Front Desk the Processing primary landing. Enter Ballots stays the page a guest is sent to when they are on a page Processing does not allow.

Front Desk list, check-in, stats, and roll call, and ballot and vote reads, stay on `ElectionAccess` with no stage gate. A guest teller can use those in Gathering and in Processing.

Guest paper-entry writes are different. A guest-teller token may create, update, delete, or reorder a ballot or vote only while the election is Processing Ballots. That includes CDN ballot import. Any other stage returns 403 `{ error: "elections.guestBallotEntryOnlyDuringProcessing" }`. Owners and admins are not subject to the check. The check is `RequireElectionAccessAttribute.GuestPaperEntry` in `RequireElectionAccessFilter`, the same filter that already resolves the election.

**Rejected alternative:** leave paper entry to the guest menu. The page redirects away from `/ballots` during Gathering, but the guest token could still call createBallot and createVote.

**Rejected alternative:** a stage policy on Front Desk. List and check-in stay available in Gathering and in Processing.

> Superseded 2026-11: an earlier note rejected any second backend stage policy. That rejection still holds for Front Desk and for reads. It does not hold for guest paper-entry writes.

**Reason:** during Processing, guest tellers check people in and enter paper ballots. During Gathering, no paper entry is done. Enter Ballots remains the Processing counterpart to Front Desk. Tally/monitor/results stay FullTeller-only (`adminOnly`). Per-ballot `/ballots/:id/entry` routes remain allowed for guests during Processing. Gathering stays Front Desk only in the menu.

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

## Concurrent tellers: automated coverage is SQLite, not a browser driver

**Status:** active  
**Evidence:** inferred (issue #191 leftover; tests named under #191)  
**Source:** issue #191; same SQLite two-context approach as #336 / Accept-all CAS  
**Revisit when:** a Playwright (or other browser) path is adopted as standard, or paper numbering grows a unique/CAS constraint

Two tellers on two computers is the normal election, not an edge case. Multi-teller SignalR visibility is already shipped. #191 leftover is proving that concurrent **writes** stay consistent: paper ballot rows, Accept-all, and Front Desk.

**Chosen:** SQLite file + two `MainDbContext` instances (`Issue191ConcurrentTellerTests`). In-memory EF cannot prove row locks or `ExecuteUpdate` claims. HTTP coverage on the shared `CustomWebApplicationFactory` is an extra slice (`Issue191ConcurrentTellerHttpTests`) when two authenticated clients are enough. Playwright is not in this repo’s standard path (`E2E_TESTING_GUIDE.md` is smoke/manual).

Paper numbering is `Max(BallotNumAtComputer)+1` per `(location, computer code)`. Product-normal concurrent entry is **two computer codes** (A and B), so the sequences do not share a max. There is no unique index on `(LocationGuid, ComputerCode, BallotNumAtComputer)` and no CAS on teller create — this slice does not invent one. Same-computer concurrent create is not the product-normal path and is not asserted here.

Accept-all vs Front Desk **same person** is still one counted vote (desk method **or** accepted online). Ordered interleavings and stale-context CAS stay in `Issue336AcceptAllFrontDeskRaceTests`. Two overlapping Accept-alls stay in `OnlineVotingServiceAcceptAllConcurrencyTests`. #191 adds a true `Task.WhenAll` of Accept-all + check-in, plus paper entry overlapping Accept-all on a **different** pending voter, plus Unregister + re-check-in overlapping paper entry on another computer (the supported method-change path).

**Rejected alternative:** Playwright two-browser E2E as the first #191 slice. Rejected — frontend tests stay Vitest + jsdom; the leftover is an automated concurrent write proof, not a UI rewrite.

**Rejected alternative:** treat two browsers on the same computer code as the numbering invariant. Rejected — each teller workstation has its own code; colliding Max+1 on one code would be a different product change.

**Reason:** lock the already-shipped multi-teller write contract locally, without Azure SQL and without inventing a new lock.

## Private SQLite files turn pooling off before delete

**Status:** active  
**Evidence:** inferred (Windows `File.Delete` of a pooled Microsoft.Data.Sqlite file throws a sharing violation; Linux unlink of that same open file succeeds. #401’s `EvaluateAsync_WorksOnSqliteWithoutThrowing` started deleting a temp file and did not release the handle. Sibling file tests already catch `IOException` on delete. The Azure exception text was not in this checkout.)  
**Verification:** corroborated — with pooling off for that file, `windows-latest` passed `EvaluateAsync_WorksOnSqliteWithoutThrowing` and the other 1635 Backend.Tests  
**Revisit when:** Microsoft.Data.Sqlite drops the file handle on dispose while pooling stays on, or Backend.Tests stops using file-backed SQLite

`Microsoft.Data.Sqlite` pools by default. Disposing the context returns the connection to the pool, so the file stays open. `EvaluateAsync_WorksOnSqliteWithoutThrowing` deletes that temp database in a `finally`, with `Pooling = false` on that connection string only. The finalization queries still run on a real SQLite file.

**Rejected alternative:** `SqliteConnection.ClearAllPools()` before delete. Rejected — it closes every pooled connection in the process, and Backend.Tests keeps parallelization on (`GlobalSettings.cs`).

**Rejected alternative:** catch `IOException` and leave the file, as the phone-prefix and paid-send tests do. Rejected — those tests tolerate a leftover file. This test is supposed to remove its file, and a caught sharing violation still leaves the handle open.

**Reason:** the SQLite finalization check has to be deletable on Windows without disturbing other tests.

## Dashboard and super-admin counts follow the stage

**Status:** active  
**Evidence:** confirmed

Completed is Finalized only. Active (teller dashboard) and open (super admin) are GatheringBallots and ProcessingBallots. SettingUp is not yet open: it is the super-admin upcoming count, and it is neither active nor completed on the teller dashboard. The election date does not move an election between those counts. A past date does not make SettingUp open, and a future date does not take GatheringBallots or ProcessingBallots out of the open count.

There is no archived stage and no archive flag on the election. The super-admin summary does not report an archived count.

**Rejected alternative:** classify open versus upcoming from `DateOfElection`. A Finalized election with a past date, and a SettingUp election with a past date, were counted as open.
