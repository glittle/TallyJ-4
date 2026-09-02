# Online Ballot Acceptance & Name Resolution

## Status: active

## Evidence: confirmed (issues #188 / #169 / #187 / #256; v3 BallotNormal Find flow)

Highest-risk functionality. Random name resolution and online acceptance introduce failure modes that did not exist (or were rare) in paper-only flows.

### Design posture

Treat online ballot paths with the same rigor as core analysis. Prefer explicit failure and recovery over silent best-effort behavior.

## Accept-all of pending online ballots

**Status:** active  
**Evidence:** confirmed (issue #188, Glen; v3 `ElectionHelper.ProcessOnlineBallots`)

A voter submit stores a pending payload on `OnlineVotingInfo` (status `Submitted`). It does not create a regular `Ballot`. A logged-in teller may Accept-all current pending online ballots while the online voting window is still open, and may do so more than once. Each run only accepts rows that are `Submitted` or already `Processing` at that moment.

Accept-all creates a regular ballot at the Online location (computer code `OL`) as if a teller had typed from paper, then wipes the online payload (`ListPool`, `PoolLocked`, `BallotGuid`) and sets status `Processed`. After that, the voter cannot change the vote. Acceptance is not reversible: we do not keep a link from the online row to the regular ballot.

v3 required the window to be closed before processing. That was rejected here so tellers can accept current pending ballots up to the last moment without shutting voters out.

A second overlapping Accept-all for the same election on one host is refused (process-wide election-scoped lock, HTTP 409). That lock is only a fast same-host gate. Two app servers can share one database, so uniqueness is in the row status, not in process memory.

Accept-all is two passes:

1. Load the expected `Submitted` (and already-`Processing`) row ids, then persist `Processing` with `UPDATE … SET Status = Processing WHERE Status = Submitted`. Another server can see that claim. `Processing` is a real stored status (varchar(10); the word fits).
2. For each expected id, open a transaction and process **only if the row is still `Processing`** (`UPDATE … SET Status = Processed WHERE Status = Processing`). 0 rows means the other server already took it. Ballot create and payload wipe share that transaction; a rollback restores `Processing` so a later run can retry.

Submit updates with `WHERE Status = Submitted AND BallotGuid IS NULL`, and rejects when `BallotGuid` is set or status is `Processing`/`Processed`, so it cannot revive a claimed row or mint a second ballot from a legacy submitted row.

In v3, a second call that started while the first was still creating ballots produced duplicates. The in-process lock serializes Accept-all on one host; the two-pass DB claim is what makes duplicates impossible across instances and vs Submit.

Rows that already have a `BallotGuid` from the older submit-creates-ballot path are marked `Processed` and unlinked without creating a second ballot. The voter cannot resubmit those rows (`BallotGuid` is not nulled).

**Rejected alternative:** keep creating the regular ballot on voter submit, and treat Accept-all as only a status flip. That would not match “create a new regular ballot and wipe the online content,” and a concurrent accept that also created ballots is the failure mode from #188.

**Rejected alternative:** treat the in-process lock plus a Status re-read as enough to prevent duplicates. Two Azure instances can both read `Submitted` under READ COMMITTED, and a Submit that loaded `Submitted` can overwrite `Processed` if it UPDATEs by primary key only.

**Rejected alternative:** jump `Submitted` → `Processed` in one transaction with no stored interim. That CAS is atomic, but while one server is still creating ballots the row still looks `Submitted` to everyone else until commit. A persisted `Processing` claim is visible to the other server for the whole run.

**Rejected alternative:** require the online window to be closed before Accept-all (v3). Tellers need to accept what is in hand without closing voting.

**Reason:** pending votes stay changeable until a teller accepts them; accepted votes become ordinary ballots with no remaining online payload.

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
**Evidence:** confirmed (issue #256, v3 `BallotNormal.cshtml.js` `findWithRawVotePart`)

When voters type names (online random/both modes) or an import cannot match a name, the original text is stored on the vote and tellers resolve it on the ballot — they do not get a new empty line.

- Persist free-text as v3-compatible `OnlineRawVote` JSON (`First` / `Last` / `OtherInfo`) on `Vote.OnlineVoteRaw`. Legacy plain strings are still parsed.
- Unresolved votes stay `VoteStatus.Raw`; the ballot is `BallotStatus.Raw` until every line has a person or a spoil reason.
- Matching a name **updates that vote** and keeps `OnlineVoteRaw` so the voter-entered text is never discarded (issue #187).
- **Find** copies first + last into search. Each extra click drops the last letter of both names (same as v3). The teller then picks a search hit for the current line and the next unresolved line is selected.

**Rejected alternative:** keep treating free-text as a display-only string and let tellers add new votes underneath. That dropped the v3 process, hid the original names (and treated `Raw` as spoiled in the UI), and could duplicate lines instead of resolving the submitted vote.

**Reason:** tellers already know this workflow from v3; shortening is how they widen a misspelled search without retyping.

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
