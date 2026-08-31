# Online Ballot Acceptance & Name Resolution

## Status: active

## Evidence: confirmed (issues #188 / #169 / #187 / #256; v3 BallotNormal Find flow)

Highest-risk functionality. Random name resolution and online acceptance introduce failure modes that did not exist (or were rare) in paper-only flows.

### Design posture

Treat online ballot paths with the same rigor as core analysis. Prefer explicit failure and recovery over silent best-effort behavior.

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
