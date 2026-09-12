# Online Ballot Acceptance & Name Resolution

## Status: active

## Evidence: confirmed (issues #188 / #169 / #187 / #256; v3 BallotNormal Find flow)

Highest-risk functionality. Random name resolution and online acceptance introduce failure modes that did not exist (or were rare) in paper-only flows.

### Design posture

Treat online ballot paths with the same rigor as core analysis. Prefer explicit failure and recovery over silent best-effort behavior.

## Draft autosave vs Submitted

**Status:** active  
**Evidence:** confirmed (Glen, #303 UAT; voter ballot page)

While the voter fills names, the ballot page silently autosaves to `OnlineVotingInfo` as status **Draft** (payload in `ListPool`). Reload restores those votes. Draft is not Accept-all pending and does not inflate monitor Submitted counts. Clearing the last name overwrites that saved payload (empty Draft, or payload-only if already Submitted) so leave/return does not restore names the voter just removed. Empty `Votes` is allowed on submit validation for that overwrite. A first visit with no names still does not create a Draft. Monitor `TotalOnlineBallots` is Submitted + Processing + Processed — Draft is not in that total or in pending.

The explicit **Submit Ballot** action writes the same payload as **Submitted**. Accept-all still takes only `Submitted` + `Processing`.

Once a row is **Submitted**, later autosaves (reload restore, notify toggle, or a debounced save after Submit) update the payload only. They do not demote Status back to Draft. Allowed writes are Draft→Draft, Draft→Submitted, Submitted→Submitted. The client sends `isDraft: false` after Submit so it agrees with the server; the server also refuses demotion if a stale Draft flag arrives.

**Rejected alternative:** autosave as `Submitted`. Incomplete ballots would appear in pending counts and could be Accept-all’d while the voter was still editing.

**Rejected alternative:** follow `IsDraft` even after Submit. That dropped a finished ballot out of Accept-all pending / monitor Submitted counts on silent autosave.

**Reason:** survive reload without treating an in-progress ballot as ready for tellers, and keep a submitted ballot pending until Accept-all.

## Accept-all of pending online ballots

**Status:** active  
**Evidence:** confirmed (issue #188, Glen; v3 `ElectionHelper.ProcessOnlineBallots`)

A voter submit stores a pending payload on `OnlineVotingInfo` (status `Submitted`). It does not create a regular `Ballot`. Submit (create or update a pending online ballot) is refused while the election is Finalized (`voting.submit.finalized`), even if the online window is still open. Window-based open/close still applies when the election is not Finalized.

Advancing **to** Finalized is refused while that same window is currently open (`elections.stageChangeError.onlineVotingStillOpen`). Close the window first; Finalize does not close it. `UseOnlineVoting` plus a null open/close is treated as open (same as submit / available-elections).

A logged-in teller may Accept-all current pending online ballots while the online voting window is still open, and may do so more than once. Each run only accepts rows that are `Submitted` or already `Processing` at that moment.

An empty Submitted overwrite (voter cleared every name after Submit) stays `Submitted` — never demoted to Draft. It still counts as pending on the monitor. Accept-all does not claim that payload and does not set `Processed`: there are no votes to turn into an OL ballot, and finalizing would lock the voter out with nothing counted. That row is pending-but-skipped until names are written again. A leftover `Processing` claim with an empty payload is restored to `Submitted` instead of wiped.

Accept-all creates a regular ballot at the Online location (computer code `OL`) as if a teller had typed from paper, then wipes the online payload (`ListPool`, `PoolLocked`, `BallotGuid`) and sets status `Processed`. After that, the voter cannot change the vote. Acceptance is not reversible: we do not keep a link from the online row to the regular ballot.

v3 required the window to be closed before processing. That was rejected here so tellers can accept current pending ballots up to the last moment without shutting voters out.

A second overlapping Accept-all for the same election on one host is refused (process-wide election-scoped lock, HTTP 409). That lock is only a fast same-host gate. Two app servers can share one database, so uniqueness is in the row status, not in process memory.

Accept-all is two passes:

1. Load the expected `Submitted` (and already-`Processing`) row ids, then persist `Processing` with `UPDATE … SET Status = Processing WHERE Status = Submitted`. Another server can see that claim. `Processing` is a real stored status (varchar(10); the word fits). Empty Submitted rows (no votes, no legacy `BallotGuid`) are left out of this claim set.
2. For each expected id, open a transaction and process **only if the row is still `Processing`** (`UPDATE … SET Status = Processed WHERE Status = Processing`). 0 rows means the other server already took it. Ballot create and payload wipe share that transaction; a rollback restores `Processing` so a later run can retry. If the taken row has no votes and no legacy `BallotGuid`, Status is restored to `Submitted` (not Draft, not Processed).

Submit updates with `WHERE Status = Submitted AND BallotGuid IS NULL`, and rejects when `BallotGuid` is set or status is `Processing`/`Processed`, so it cannot revive a claimed row or mint a second ballot from a legacy submitted row.

In v3, a second call that started while the first was still creating ballots produced duplicates. The in-process lock serializes Accept-all on one host; the two-pass DB claim is what makes duplicates impossible across instances and vs Submit.

Rows that already have a `BallotGuid` from the older submit-creates-ballot path are marked `Processed` and unlinked without creating a second ballot. The voter cannot resubmit those rows (`BallotGuid` is not nulled).

**Rejected alternative:** keep creating the regular ballot on voter submit, and treat Accept-all as only a status flip. That would not match “create a new regular ballot and wipe the online content,” and a concurrent accept that also created ballots is the failure mode from #188.

**Rejected alternative:** treat the in-process lock plus a Status re-read as enough to prevent duplicates. Two Azure instances can both read `Submitted` under READ COMMITTED, and a Submit that loaded `Submitted` can overwrite `Processed` if it UPDATEs by primary key only.

**Rejected alternative:** jump `Submitted` → `Processed` in one transaction with no stored interim. That CAS is atomic, but while one server is still creating ballots the row still looks `Submitted` to everyone else until commit. A persisted `Processing` claim is visible to the other server for the whole run.

**Rejected alternative:** require the online window to be closed before Accept-all (v3). Tellers need to accept what is in hand without closing voting.

**Rejected alternative:** Accept-all an empty Submitted overwrite as `Processed` with no OL ballot (the path `Votes: []` opened). That locks the voter out (`CannotChangeOnlineVote`) with nothing counted if a teller Accept-alls while names are cleared mid-edit.

**Rejected alternative:** demote empty Submitted to Draft so Accept-all ignores it. Draft vs Submitted is never-demote; an emptied resubmit stays Submitted and pending-but-skipped.

**Reason:** pending votes stay changeable until a teller accepts them; accepted votes become ordinary ballots with no remaining online payload.

### Automated coverage for submit → Accept-all → counts

**Status:** active  
**Evidence:** confirmed (issue #169 remaining; HTTP integration in `OnlineVotingBallotFlowTests`)

Issue #169 is the test script for this process, not a second product build. Coverage is the HTTP integration path (voter submit → teller Accept-all → monitor/summary counts, ListPool wipe, OL regular ballot, tally) plus the existing Accept-all service tests and monitor Vitest counts. That HTTP test uses the shared SQLite `CustomWebApplicationFactory`, which is relational, so it exercises the `ExecuteUpdate` claim that in-memory unit tests skip.

**Rejected alternative:** Playwright browser E2E. Frontend tests stay Vitest + jsdom; the monitor already locks counts-only (no named pending/accepted list) and a reload after Accept-all.

**Rejected alternative:** run these automated tests against Azure SQL. Backend.Tests stay on the local SQLite factory. Live cloud-agent app runs use local Docker SQL + migrations + SeedOnStartup, not production Azure SQL.

**Reason:** lock the already-shipped Accept-all contract (including anonymity: counts only) without a browser driver and without a production database.

## Pending vs accepted on the monitor (counts only)

**Status:** active  
**Evidence:** confirmed (issue #188 remaining slice; Glen, PR #296)

> Superseded 2026-09: a named pending/accepted row list (person + WhenStatus) was rejected — see below.

The monitor tells pending from accepted using `OnlineVotingInfo.Status` counts only:

- **Pending** = `Submitted` + `Processing` (same set Accept-all will take).
- **Submitted** = still changeable.
- **Processing** = claimed by Accept-all; submit is already blocked.
- **Accepted** = `Processed`.

No person name, email, phone, kiosk, voter id, row id, or WhenStatus is returned or rendered for these rows. Front Desk / people roll may still show who voted; that is a different surface and is not paired with the OL ballots Accept-all creates.

**Rejected alternative:** list person names (or hide names in the UI while the API still sends them). Pairing a named pending/accepted list with the regular OL ballots created by Accept-all identifies how that person voted. Worst when n=1.

**Rejected alternative:** suppress names only when the pending or accepted count is under 10. Watching names (or named rows) move during Accept-all still matches a voter to the new OL ballot.

**Rejected alternative:** keep anonymous per-row Status/WhenStatus lists. A timestamped or shrinking row list can be watched the same way during Accept-all.

**Rejected alternative:** treat the accepted side as a join to the regular `Ballot`. Acceptance is not reversible and must not reconnect the online row to the counted ballot.

**Reason:** tellers need pending vs accepted (and Submitted vs Processing) across multiple Accept-all runs while the window stays open, without a secret-ballot leak.

## Monitor: 5-minute close countdown

**Status:** active  
**Evidence:** confirmed (issue #184 remaining slice; v3 `Monitor.cshtml` / `closeOnline`)

The Monitor Progress online card shows whether the voting window is open or closed, a relative close line, and a `m:ss` clock in the last five minutes. Full tellers can **Schedule close in 5 minutes** (firm — `OnlineCloseIsEstimate = false`), **Close now** (one second ago, estimate unchanged), or **Open for 5 minutes** when already closed (estimate unchanged). Those buttons call the existing online-window API; they do not invent a second close path.

v3 used “Expected to close” when the close was an estimate and “Will close” when firm. The last-five-minute highlight is `minutes <= 5` (same as v3 `onlineSoon`).

**Rejected alternative:** put the 5-minute / close-now buttons only on the header Online Voting drawer. Rejected — v3 tellers used them on Monitor Progress; the header already has the date pickers.

**Rejected alternative:** implement named “active voters building a ballot” in the same slice. Rejected — Draft is restore-only and is not Accept-all pending; a named list next to pending/accepted OL counts would reopen the secret-ballot pairing #188 closed. Anonymous ballot-page sessions are the later #184 item (see below).

**Reason:** tellers need a visible, testable 5-minute close on the monitor without pairing voters to ballots.

## Monitor: connected online voters (sessions, not names)

**Status:** active  
**Evidence:** confirmed (issue #184 remaining slice; Draft autosave exists for restore only; v3 `AllVotersHub` docs in `docs/Hubs-in-v3.md` have no connection-count API and no named composing list)

v3 Monitor (this repo’s hub docs) pushed online window changes via FrontDeskHub. It did not document a named “who is building a ballot” list, and v3 `AllVotersHub` was a global notify group with no membership-count API.

v4 Draft is silent autosave only (not Accept-all pending). The monitor still does not list who is composing. “Building a ballot” is not shown as names.

The monitor shows **Connected online voters → Ballot-page sessions**: an anonymous count of AllVotersHub connections that called `JoinElection` for this election (the voter ballot page). One person with two tabs counts as two. The API and UI return that integer only — no person name, email, phone, kiosk, voter id, row id, or WhenStatus. `IOnlineVoterPresenceService` stores connection id → election GUID only.

The count is same-host in-memory. Two app servers do not share it. Auto-refresh (30s) is how tellers see a new number.

`VoterPersonalHub` cannot do this safely: its group is `Voter{voterId}` (identifying) and the voter JWT has no election claim.

**Rejected alternative:** a named list of voters on the ballot page. Pairing that list with pending/accepted OL / Accept-all identifies how that person voted.

**Rejected alternative:** put the site-wide `AllVoters` connection count on a per-election monitor. A voter on another election’s list would inflate this election. Misleading.

**Rejected alternative:** use Draft (or a composing heartbeat) as a named “building a ballot” list. Draft exists for reload restore; putting those names next to pending/accepted OL counts would reopen the secret-ballot pairing #188 closed. Not needed for “is anyone currently on this election’s ballot page?”

**Rejected alternative:** count unique voter ids (hashed) instead of sessions. Rejected for this slice — the product ask is sessions, and storing voter ids next to an election (even hashed) is extra identity surface for no teller gain.

**Reason:** tellers need a live anonymous signal that this election’s voting UI is in use, without a secret-ballot leak and without calling it “building a ballot.”

## Accept-all audit record

**Status:** active  
**Evidence:** confirmed (issue #188 remaining slice; `SecurityAuditLogs` replaced `Logs` / C_Log in `20260713054353_MergeLogsIntoSecurityAuditLogs`)

Each successful Accept-all persists one operational `SecurityAuditLog` row: who accepted (logged-in teller `UserId`, plus `DisplayName` when the account has one), when, and pending / accepted counts before and after that run. A teller may Accept-all more than once; each run is its own row. Failed runs and overlapping 409 refusals do not write a success audit.

Tellers see those rows on the monitor page (same place as Accept-all). They also appear on the existing Audit Logs page because that page reads `SecurityAuditLogs`.

`OnlineVotingInfo.HistoryStatus` is not this record. Accept-all still appends a per-voter `Processed|{timestamp}` there. That string has no teller identity and no before/after counts.

The generic `AuditMiddleware` POST path log is skipped for Accept-all so a second, count-less “success” row is not stored next to the real one.

The audit stores teller user id and optional display name only. It does not store voter email, phone, kiosk code, or voter id (CodeQL `cs/exposure-of-sensitive-information` on #294). Teller email is also left off the row.

**Rejected alternative:** treat `HistoryStatus` as the teller-run audit. It is per voter and cannot answer who accepted or the run counts.

**Rejected alternative:** add a new Accept-all audit table. `SecurityAuditLogs` with `OperationalActivity` is already the election activity log that replaced C_Log.

**Reason:** issue #188 asked for a durable who/when/counts record in the existing TallyJ log, visible to tellers, without a parallel system and without voter contact details.

## Teller resolution of free-text names

**Status:** active  
**Evidence:** confirmed (issue #256, v3 `BallotNormal.cshtml.js` `findWithRawVotePart`; Glen on issue #187)

In TallyJ, **random name** means a vote on an **online ballot** (selection process random / both) where the voter typed a name. It is not paper teller entry and not a general people-search feature. After Accept-all, those lines become regular votes; tellers resolve the typed name on that ballot — they do not get a new empty line. Import mismatches reuse the same `OnlineVoteRaw` payload; that is not a paper random-name flow.

- Persist free-text as v3-compatible `OnlineRawVote` JSON (`First` / `Last` / `OtherInfo`) on `Vote.OnlineVoteRaw`. Legacy plain strings are still parsed.
- Unresolved votes stay `VoteStatus.Raw`; the ballot is `BallotStatus.Raw` until every line has a person or a spoil reason.
- Matching a name **updates that vote** and keeps `OnlineVoteRaw` so the voter-entered text is never discarded (issue #187).
- **Find** copies first + last into search. Each extra click drops the last letter of both names (same as v3). The teller then picks a search hit for the current line and the next unresolved line is selected.
- The open-ballot list marks unresolved `OnlineVoteRaw` (`isUnresolvedRawVote`: truthy raw, no person, no spoil). That includes online typed names and CDN/import mismatches. v3 used a saturated peach (`#f1b787` / `rawDonefalse`) so unfinished lines jumped out next to matched green. v4 uses a warning-tinted row, left stripe, and **Find** — no text chip. A matched line keeps the original text above the person name. The **raw** line uses `--el-color-warning-dark`; the matched person name stays the normal vote-name color. Paper votes without `onlineVoteRaw` are not marked.
- Row fills mix the warning/success token onto `--el-bg-color` so they stay pale on white and dark on navy. Text on those rows is `--color-text-primary`. EP `*-light-9` stays mint/peach in dark while inherited text goes near-white (Glen UAT).

**Rejected alternative:** keep treating free-text as a display-only string and let tellers add new votes underneath. That dropped the v3 process, hid the original names (and treated `Raw` as spoiled in the UI), and could duplicate lines instead of resolving the submitted vote.

**Rejected alternative:** treat any vote without a person — including paper empty slots — as needing resolution. That would invent a paper random-name flow.

**Rejected alternative:** key the mark off `OnlineVoteRaw` alone. Matching keeps the original text, so resolved lines would stay highlighted.

**Rejected alternative:** keep the pale `warning-light-8` / `*-light-9` fills (with or without a **Needs matching** chip). The chip was redundant with Find + the warning row. Those EP light tints stay pale in dark theme, so `--el-text-color-*` (near-white) failed contrast.

**Reason:** tellers already know this workflow from v3; shortening is how they widen a misspelled search without retyping. They must also see which raw lines are still unmatched.

## Teller-created ballots stay off the Online location

**Status:** active  
**Evidence:** confirmed (issue #287; maintainer)

The reserved location is only for voter-initiated ballots (computer code `OL`). Identify it by `LocationTypeCode` / `LocationType.Online`, never by the display name. Names are user-facing and translated; the English word “Online” is not a stable key.

A teller starting a paper ballot must store it at the location currently selected in that browser. If that selection has type Online, starting is blocked.

Create used to ignore the requested location and take `Locations.FirstOrDefault` for the election, so new teller ballots could land on the reserved location.

Older submit code (and the locations form) could store a row *named* “Online” with a null type. `LocationTypeEnum` treats a missing code as Manual, so that row is a normal paper location as far as create-ballot is concerned.

**Rejected alternative:** allow tellers to create ballots at the Online-typed location (the original #287 wording). Mixing teller-entered paper ballots into the voter-submitted set hides which ballots came from voters.

**Rejected alternative:** treat a location named “Online” as reserved. The product is multilingual; English names cannot be used as identifiers.

**Reason:** the browser location is the teller's workstation context; the Online *type* is not a paper station.

The typed Online location is added when setup enables online voting, and removed if online voting is turned off and that location has no ballots (or computers). Voter submit still ensures the row exists if setup enabled voting but the location is missing.

**Rejected alternative:** create the location only on the first voter ballot. Tellers would not see it in the location list until a vote arrived, and disabling unused online voting would leave an empty reserved location behind.

## Reserved location display (Online and Imported)

**Status:** active  
**Evidence:** confirmed (issue #287; Glen, 4 Sep 2026; Imported parity, Glen, #303 UAT)

Online and Imported are both reserved `LocationType` rows. Identity is the type code, never the stored name. Display uses the current-language label (`locations.typeOnline` / `locations.typeImported` via `formatLocationLabel` / `LocationDisplayHelper`). The stored `Name` is a fallback for reports and logs, not what tellers edit.

On the Locations page each reserved row is a single i18n tag (no text+badge duplicate), contact shows `-`, and the row is highlighted by type. A paper location whose name happens to be “Online” or “Imported” is not that row.

Editing a reserved row may change sort order only. Name is read-only (the i18n label). Contact, latitude, and longitude are not offered. The API ignores those fields on a reserved-type update and refuses delete. Teller create never assigns Online or Imported; `OnlineLocationHelper` creates Online, and CDN ballot import creates Imported.

Teller-started paper ballots are blocked at both reserved locations (FE start checks + `BallotService.CreateBallotAsync`). Online remains voter-initiated (`OL`); Imported remains import-initiated (`IM`).

**Rejected alternative:** treat a location named “Online” / “Imported” as reserved, or POST the translated label as the stored name. Names are user-facing and translated; writing the current language back would change the stored fallback and still would not identify the row.

**Rejected alternative:** give Imported a weaker Locations UX than Online (editable name/contact while Online is locked). Both are system-managed stations; tellers should not dress them up as paper locations.

Ballot entry panels and the votes dialog load locations when the ballot’s `locationGuid` is not already in the location store, so `formatLocationLabel` can use type. If that fetch fails, the stored `locationName` remains the display fallback.

The ballots report projects location name + type with `AsNoTracking` instead of materializing the full ballot/location/vote/person graph, then formats reserved types the same way. That keeps the report label correct without tracking entities for a read-only export.

**Rejected alternative:** rely only on `ballot.locationName` in the UI, or keep `Include` graphs for the report. The stored name is the English (or setup-time) fallback and can disagree with the teller’s language; the Include path also tracked entities the report never updates.

**Reason:** tellers need to see which rows are system stations, in their language, without being able to rename them or start paper ballots there.

## Online and imported ballot codes

**Status:** active  
**Evidence:** confirmed (issue #256 follow-up; v3 used `OL` / `IM`)

Online ballots use reserved computer code `OL` and a per-location sequence (`OL1`, `OL2`, …). Imported ballots use `IM`. Tellers see **Online 3** / **Imported 3**, not `OL3` or `WW0`.

`WW` was a leftover stand-in for “online” and every new ballot was stored as number `0`, so they all displayed as `WW0`. Existing `WW` / `0` online ballots are renumbered when the election’s ballots are loaded or another online ballot is submitted.

**Rejected alternative:** treat `WW` as a normal workstation code and keep `WW03`. That collides with teller computers (`A`–`ZZ`) and hides that these ballots did not come from a paper station.

Tellers do not delete votes, delete the ballot, or drag-reorder names on online/imported ballots. Those lines arrived as a submitted set; the work is to match names, not edit the set.

A missing name or spoiled vote (U01 / U02) is applied **to the selected line**. **Set as spoiled vote or new name** stays hidden until the teller is finding a name for that line (Find / Change), then appears above **Start another ballot**. The drawer shows the voter-entered name and, for a new person, prefills first and last from that text. A small **Swap** on the person form (ballot drawer and People Management) reverses first and last when the split guessed the order wrong. Eligibility starts empty and must be chosen — a write-in is not assumed eligible. Search matches work the same way: they replace the selected vote instead of appending a new one.

**Rejected alternative:** keep the paper flow (always show the button and add a new line). That would grow a submitted set instead of resolving the line the voter already sent.

## Name selection process codes

**Status:** active  
**Evidence:** confirmed (voter ballot page, seed data, `OnlineElectionInfoDto`)

v4 stores `OnlineSelectionProcess` as `A` (list), `B` (random / free text), or `C` (both). v3 stored `L` / `R` / `B` for the same three modes.

**Rejected alternative:** keep v3's `L`/`R`/`B` letters. Existing v4 elections, seeders, and the voter UI already use `A`/`B`/`C`.

## Related

- Ballot validation
- Election state management
