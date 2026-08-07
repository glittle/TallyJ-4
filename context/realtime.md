# Realtime (SignalR)

## Hub-specific group name patterns

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); hub `GetGroupName` helpers; `SignalRNotificationService`  
**Revisit when:** a hub is added/removed, or group naming is unified deliberately

Do **not** assume a single `election-{guid}` convention for all realtime traffic. Each hub builds its own group name (via `GetGroupName` statics) and broadcasts go through matching methods in `SignalRNotificationService`.

| Pattern                                                     | Hub / use                                               |
| ----------------------------------------------------------- | ------------------------------------------------------- |
| `Main{electionGuid}` (+ `…Known` / `…Guest`)                | MainHub — election updates, status, closed              |
| `Analyze{electionGuid}`                                     | AnalyzeHub — tally progress/complete                    |
| `FrontDesk{electionGuid}`                                   | FrontDeskHub — people, ballots, online election, reload |
| `BallotImport{electionGuid}` / `PeopleImport{electionGuid}` | import hubs                                             |
| `Public`                                                    | PublicHub — guest-teller joinable elections list        |

Frontend: `frontend/src/services/signalrService.ts` (`connectTo*Hub`, `joinElection`, etc.) and store subscriptions.

**Reason:** different surfaces need different fan-out and sometimes different membership (known vs guest). One flat `election-{guid}` group would over-notify or under-notify and couple unrelated UI areas.

**Rejected alternative:** one shared election group for every event type. Rejected because Main, Front Desk, Analyze, and Public have different listeners and update cadences.

## No OnlineVotingHub (election-scoped online voting SignalR)

**Status:** active  
**Evidence:** confirmed  
**Source:** maintainer review of unused scaffold vs v3 `AllVotersHub` / `VoterPersonalHub`  
**Revisit when:** online voters need live list refresh or personal status push

There is no `/hubs/online-voting` hub and no `online-election-{guid}` group. Online voting remains HTTP (`/api/online-voting/*`). Operator-facing online window updates use FrontDeskHub (`updateOnlineElection`). Ballot submit confirmation is the HTTP response.

**Rejected alternative:** keep scaffolded `OnlineVotingHub` with `online-election-{electionGuid}` and client-callable `BallotSubmitted` (payload including `totalVotes`). Rejected — unused (no server `IHubContext` producers, no frontend client), mismatched v3 voter model (global all-voters + personal `Voter{id}`, not per-election ballot events), and would push vote totals rather than a thin “refetch” signal.

**Preferred shape if restored later:** authenticated voter channel; thin “changed” (or `electionGuid` only) event so clients re-call `GET availableElections` / vote status APIs — avoid disclosing election detail or tallies on the hub.

## No anonymous public results display

**Status:** active  
**Evidence:** confirmed  
**Source:** product decision (maintainer)  
**Revisit when:** a deliberate, authenticated presentation product requirement appears

Anonymous clients must not learn election results (or other election detail) just by knowing a GUID. `PublicHub` is only for the guest-teller join surface: the static `Public` group receives list open/close notifications used by teller join.

**Rejected alternative:** per-election `public-display-{guid}` groups + `GET /api/Public/{guid}/publicDisplay` + full-screen public results page. Removed — no business need for random users to view election data; authenticated results presentation (e.g. in-app results/presentation views) covers operator needs.

Election detail over HTTP follows the same rule: `GET /api/Elections/{guid}/status` (and other election-scoped routes) require `ElectionAccess` (full or guest teller joined to that election). Anonymous public endpoints stay limited to guest-join discovery (`/api/Public/elections`, hub `Public` group) and non-sensitive health/home.
