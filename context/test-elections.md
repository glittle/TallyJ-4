# Test elections

## Duplicate copies settings, people, and locations — not ballots or runtime state

**Status:** active  
**Evidence:** confirmed (issue #193 first slice)  
**Source:** issue #193; v4 `CreateElectionAsync` / `JoinElectionUser` ownership; v3 `ElectionHelper.Copy` (commented; guest tellers denied; SQL `CloneElection`)  
**Revisit when:** banner or test-only reset slices land, or copy scope needs ballots

`POST /api/Elections/{guid}/duplicateElection` creates a new election from one the caller already owns. The copy gets a new `ElectionGuid`, `ShowAsTest = true`, stage `SettingUp`, and the same ownership row create uses (`JoinElectionUser` Role `Admin`). People and locations are copied with new GUIDs. Person phones go through `EnsureOnlineVotersForPhonesAsync` (same helper as person create/import). Ballots, results, computers, tellers, online votes (`OnlineVotingInfo`), SMS logs, and analysis rows are not copied. Check-in / envelope / teller-name / `HasOnlineBallot` fields on people are cleared. Teller access (`ListedForPublicAsOf`) starts closed.

**Rejected alternative:** map the source into `CreateElectionDto` and call `CreateElectionAsync`. That DTO does not carry every persisted setting, so it would be a second, lossy create path. Duplicate assigns settings explicitly and only reuses the create ownership join.

**Rejected alternative:** `[Authorize]` only (same as create) or `ElectionAccess`. Create has no source election; duplicate does. `ElectionAccess` would let guest tellers copy. `FullTellerAccess` plus a service-side Owner/Admin join check matches “owner can duplicate their election” and v3’s guest-teller deny. SuperAdmin / global Admin is not given a bypass they do not already have on this route.

**Rejected alternative:** add a new `IsTest` column. `Election.ShowAsTest` already exists (dashboard TEST badge, create/update DTOs, v3 `IsTest`). The later banner/reset slices hang on this flag.

Banner UI and “reset only if Test” are later slices of #193.

## Default copy name

**Status:** active  
**Evidence:** inferred (v3 copy was “copy of …” in the issue hunches; no live v3 `CloneElection` script in the TallyJ-3.0 repo)  
**Verification:** uncorroborated against a runnable v3 clone

When the client omits a name, the service uses `Copy of {source name}` (trimmed user name otherwise; truncated to 150 characters).
