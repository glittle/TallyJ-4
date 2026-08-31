# Test elections

## Duplicate copies settings, people, and locations — not ballots or runtime state

**Status:** active  
**Evidence:** confirmed (issue #193 first slice)  
**Source:** issue #193; v4 `CreateElectionAsync` / `JoinElectionUser` ownership; v3 `ElectionHelper.Copy` (commented; guest tellers denied; SQL `CloneElection`)  
**Revisit when:** copy scope needs ballots

`POST /api/Elections/{guid}/duplicateElection` creates a new election from one the caller already owns. The copy gets a new `ElectionGuid`, `ShowAsTest = true`, stage `SettingUp`, and the same ownership row create uses (`JoinElectionUser` Role `Admin`). People and locations are copied with new GUIDs. Person phones go through `EnsureOnlineVotersForPhonesAsync` (same helper as person create/import). Ballots, results, computers, tellers, online votes (`OnlineVotingInfo`), SMS logs, and analysis rows are not copied. Check-in / envelope / teller-name / `HasOnlineBallot` fields on people are cleared. Teller access (`ListedForPublicAsOf`) starts closed. The online window starts closed: `OnlineWhenOpen` / `OnlineWhenClose` are cleared and `UseOnlineVoting` is false. `GetAvailableElectionsAsync` does not filter `ShowAsTest` and treats `UseOnlineVoting` plus a null window as open, so leaving those copied would list the test copy to the same phone/email/kiosk voters. A teller can turn online voting back on and set a window.

**Rejected alternative:** map the source into `CreateElectionDto` and call `CreateElectionAsync`. That DTO does not carry every persisted setting, so it would be a second, lossy create path. Duplicate assigns settings explicitly and only reuses the create ownership join.

**Rejected alternative:** `[Authorize]` only (same as create) or `ElectionAccess`. Create has no source election; duplicate does. `ElectionAccess` would let guest tellers copy. `FullTellerAccess` plus a service-side Owner/Admin join check matches “owner can duplicate their election” and v3’s guest-teller deny. SuperAdmin / global Admin is not given a bypass they do not already have on this route.

**Rejected alternative:** add a new `IsTest` column. `Election.ShowAsTest` already exists (dashboard TEST badge, create/update DTOs, teller banner, test-only reset, v3 `IsTest`).

**Rejected alternative:** copy `UseOnlineVoting` and only null the window dates. Availability treats a null window as already open when `UseOnlineVoting` is true, so the copy would still appear to voters.

## Teller pages show a persistent Test Election banner

**Status:** active  
**Evidence:** confirmed (issue #193 second slice; UAT: gather orange, not error red)  
**Source:** issue #193; PR #284 UAT  
**Revisit when:** voter-facing chrome is added

While a teller is on an election-scoped route (`/elections/:id/…`) whose `currentElection.showAsTest` is true, MainLayout shows a full-width “Test Election” strip between the fixed header and main content. It reads the existing `Election.ShowAsTest` / `showAsTest` field — no new column. Hidden when there is no current election, `showAsTest` is false or null, or the route has no election id (Dashboard, Profile, create). Leftover `currentElection` after leaving a test election does not keep the banner up. Voter pages use PublicLayout and do not get this chrome.

Colors are an explicit pair (white on `--color-stage-gather`, the same burnt orange as the selected Gathering Ballots mode chip), not inherited header text/background. The strip always uses that token — it is a test-election marker, not a stage indicator. Error/danger red was too attention-getting; being in a test election is not a bad thing. The translucent public header and dark Front Desk toolbar already produced a dark-on-dark miss on the dashboard icon-only Copy control.

**Rejected alternative:** `--color-error-700` (white on dark red). UAT: too attention-getting; test is not an error.

**Rejected alternative:** follow `STAGE_META` for the current election stage. That would make the banner look like another stage chip and change color as the election advances.

**Rejected alternative:** a chip inside `AppHeader` only. The header is a fixed 60px bar (48px on Front Desk). A strip there is clipped, and the bar is already crowded with status controls.

**Rejected alternative:** page-local banners on `ElectionDetailPage`. Tellers also work Front Desk, ballots, people, setup, and results; a detail-only mark is easy to leave.

**Rejected alternative:** show whenever `currentElection.showAsTest` is true, ignoring the route. `currentElection` stays set after returning to the dashboard, so the banner would appear on pages that are not “inside” that election.

## Default copy name

**Status:** active  
**Evidence:** inferred (v3 copy was “copy of …” in the issue hunches; no live v3 `CloneElection` script in the TallyJ-3.0 repo)  
**Verification:** uncorroborated against a runnable v3 clone

When the client omits a name, the service uses `Copy of {source name}` (trimmed user name otherwise; truncated to 150 characters).

## Reset wipes runtime data only on ShowAsTest elections

**Status:** active  
**Evidence:** confirmed for the test-only gate (issue #193; `ShowAsTest` must be true). Inferred for wipe list, `SettingUp`, and window-close matching duplicate (no live v3 reset found).  
**Source:** issue #193; v4 `DuplicateElectionAsync` wipe/start list; `GetAvailableElectionsAsync` null-window rule  
**Verification:** uncorroborated against a runnable v3 reset (TallyJ-3.0 search did not find `ResetElection` / `CloneElection` / equivalent)  
**Revisit when:** reset scope needs import files or messages, or a v3 reset script is found

`POST /api/Elections/{guid}/resetElection` wipes runtime data on an owned `ShowAsTest` election so a teller can practice again without deleting the copy. `ShowAsTest` must be `true`. `false` and `null` refuse with a non-success result (HTTP 400, “Only test elections can be reset”). People, locations, election settings, `ShowAsTest`, and `JoinElectionUser` ownership stay. Person GUIDs and location GUIDs stay (this is the same election, not a new copy).

Deleted rows match what duplicate does not copy: votes, ballots, results, result summaries, result ties, computers, tellers, `OnlineVotingInfo`, SMS logs. Person check-in / envelope / teller-name / `HasOnlineBallot` / registration history are cleared. Location tally status and ballots-collected are cleared. `LastEnvNum` is cleared.

Stage returns to `SettingUp`. A newly duplicated test copy starts there, and a reset is meant to be a fresh practice run. Leaving Finalized or ProcessingBallots with no ballots would be a half-reset. This bypasses `ChangeElectionStageAsync` (no leave-Finalized confirmation) because reset is its own high-consequence operation with an explicit confirm in the UI.

The online window is closed the same way as a new test copy: `UseOnlineVoting = false` and `OnlineWhenOpen` / `OnlineWhenClose` cleared. `GetAvailableElectionsAsync` treats `UseOnlineVoting` plus a null window as open, so keeping the flag and only nulling dates would list the practice election to voters. Guest teller access (`ListedForPublicAsOf`) is closed; if it was open, the public list update and guest close-out run. FrontDesk clients get `reloadPage`.

Auth matches duplicate: `FullTellerAccess` plus a service-side Owner/Admin join check. Guest tellers cannot reset. SuperAdmin / global Admin is not given a bypass they do not already have on this route (a global Admin without an Owner/Admin join is 403).

The reset control is on `ElectionDetailPage` in the existing danger zone, visible only when `showAsTest` is true and the viewer is not a guest. Hidden (not disabled) on live elections. Confirmation is a destructive `ElMessageBox.confirm`. No new `IsTest` column.

**Rejected alternative:** allow reset on any owned election. Live elections must not be wipeable this way; the issue is test-only.

**Rejected alternative:** leave stage unchanged after reset. A practice election left in ProcessingBallots or Finalized with empty runtime data is not a fresh run, and duplicate copies already start at SettingUp.

**Rejected alternative:** keep `UseOnlineVoting` and only null the window dates. Availability treats that as open.

**Rejected alternative:** put the control on the dashboard list for every TEST row. Reset is destructive and belongs next to delete, inside the election, after the teller can see the Test banner.

**Rejected alternative:** `[Authorize]` only or `ElectionAccess`. Same reasons as duplicate: guest tellers must not reset; create-style auth has no source election.

Import files and election messages are kept. They were not on the duplicate omit list; whether a v3 reset removed them is unknown.
