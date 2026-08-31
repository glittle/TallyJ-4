# Test elections

## Duplicate copies settings, people, and locations — not ballots or runtime state

**Status:** active  
**Evidence:** confirmed (issue #193 first slice)  
**Source:** issue #193; v4 `CreateElectionAsync` / `JoinElectionUser` ownership; v3 `ElectionHelper.Copy` (commented; guest tellers denied; SQL `CloneElection`)  
**Revisit when:** test-only reset slice lands, or copy scope needs ballots

`POST /api/Elections/{guid}/duplicateElection` creates a new election from one the caller already owns. The copy gets a new `ElectionGuid`, `ShowAsTest = true`, stage `SettingUp`, and the same ownership row create uses (`JoinElectionUser` Role `Admin`). People and locations are copied with new GUIDs. Person phones go through `EnsureOnlineVotersForPhonesAsync` (same helper as person create/import). Ballots, results, computers, tellers, online votes (`OnlineVotingInfo`), SMS logs, and analysis rows are not copied. Check-in / envelope / teller-name / `HasOnlineBallot` fields on people are cleared. Teller access (`ListedForPublicAsOf`) starts closed. The online window starts closed: `OnlineWhenOpen` / `OnlineWhenClose` are cleared and `UseOnlineVoting` is false. `GetAvailableElectionsAsync` does not filter `ShowAsTest` and treats `UseOnlineVoting` plus a null window as open, so leaving those copied would list the test copy to the same phone/email/kiosk voters. A teller can turn online voting back on and set a window.

**Rejected alternative:** map the source into `CreateElectionDto` and call `CreateElectionAsync`. That DTO does not carry every persisted setting, so it would be a second, lossy create path. Duplicate assigns settings explicitly and only reuses the create ownership join.

**Rejected alternative:** `[Authorize]` only (same as create) or `ElectionAccess`. Create has no source election; duplicate does. `ElectionAccess` would let guest tellers copy. `FullTellerAccess` plus a service-side Owner/Admin join check matches “owner can duplicate their election” and v3’s guest-teller deny. SuperAdmin / global Admin is not given a bypass they do not already have on this route.

**Rejected alternative:** add a new `IsTest` column. `Election.ShowAsTest` already exists (dashboard TEST badge, create/update DTOs, teller banner, v3 `IsTest`). The later reset slice hangs on this flag.

**Rejected alternative:** copy `UseOnlineVoting` and only null the window dates. Availability treats a null window as already open when `UseOnlineVoting` is true, so the copy would still appear to voters.

“Reset only if Test” is a later slice of #193.

## Teller pages show a persistent Test Election banner

**Status:** active  
**Evidence:** confirmed (issue #193 second slice)  
**Source:** issue #193  
**Revisit when:** test-only reset lands, or voter-facing chrome is added

While a teller is on an election-scoped route (`/elections/:id/…`) whose `currentElection.showAsTest` is true, MainLayout shows a full-width “Test Election” strip between the fixed header and main content. It reads the existing `Election.ShowAsTest` / `showAsTest` field — no new column. Hidden when there is no current election, `showAsTest` is false or null, or the route has no election id (Dashboard, Profile, create). Leftover `currentElection` after leaving a test election does not keep the banner up. Voter pages use PublicLayout and do not get this chrome.

Colors are an explicit pair (white on `--color-error-700`), not inherited header text/background. The translucent public header and dark Front Desk toolbar already produced a dark-on-dark miss on the dashboard icon-only Copy control.

**Rejected alternative:** a chip inside `AppHeader` only. The header is a fixed 60px bar (48px on Front Desk). A strip there is clipped, and the bar is already crowded with status controls.

**Rejected alternative:** page-local banners on `ElectionDetailPage`. Tellers also work Front Desk, ballots, people, setup, and results; a detail-only mark is easy to leave.

**Rejected alternative:** show whenever `currentElection.showAsTest` is true, ignoring the route. `currentElection` stays set after returning to the dashboard, so the banner would appear on pages that are not “inside” that election.

## Default copy name

**Status:** active  
**Evidence:** inferred (v3 copy was “copy of …” in the issue hunches; no live v3 `CloneElection` script in the TallyJ-3.0 repo)  
**Verification:** uncorroborated against a runnable v3 clone

When the client omits a name, the service uses `Copy of {source name}` (trimmed user name otherwise; truncated to 150 characters).
