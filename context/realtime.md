# Realtime (SignalR)

## Hub-specific group name patterns

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); hub `GetGroupName` helpers; `SignalRNotificationService`  
**Revisit when:** a hub is added/removed, or group naming is unified deliberately

Do **not** assume a single `election-{guid}` convention for all realtime traffic. Each hub builds its own group name (via `GetGroupName` statics) and broadcasts go through matching methods in `SignalRNotificationService`.

| Pattern | Hub / use |
| --- | --- |
| `Main{electionGuid}` (+ `…Known` / `…Guest`) | MainHub — election updates, status, closed |
| `Analyze{electionGuid}` | AnalyzeHub — tally progress/complete |
| `FrontDesk{electionGuid}` | FrontDeskHub — people, ballots, online election, reload |
| `BallotImport{electionGuid}` / `PeopleImport{electionGuid}` | import hubs |
| `online-election-{electionGuid}` | OnlineVotingHub |
| `public-display-{electionGuid}` | PublicHub (per-election display) |
| `Public` | PublicHub global list/status |

Frontend: `frontend/src/services/signalrService.ts` (`connectTo*Hub`, `joinElection`, etc.) and store subscriptions.

**Reason:** different surfaces need different fan-out and sometimes different membership (known vs guest). One flat `election-{guid}` group would over-notify or under-notify and couple unrelated UI areas.

**Rejected alternative:** one shared election group for every event type. Rejected because Main, Front Desk, Analyze, Online, and Public have different listeners and update cadences.
