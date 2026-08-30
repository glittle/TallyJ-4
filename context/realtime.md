# Realtime (SignalR)

## Hub-specific group name patterns

**Status:** active  
**Evidence:** confirmed  
**Source:** project agent notes (`AGENTS.md`); hub `GetGroupName` helpers; `SignalRNotificationService`  
**Revisit when:** a hub is added/removed, or group naming is unified deliberately

Do **not** assume a single `election-{guid}` convention for all realtime traffic. Each hub builds its own group name (via `GetGroupName` statics) and broadcasts go through matching methods in `SignalRNotificationService`.

| Pattern                                                     | Hub / use                                               |
| ----------------------------------------------------------- | ------------------------------------------------------- |
| `Main{electionGuid}` (+ `…Known` / `…Guest`)                | MainHub — shared status on **base**; role events on suffix |
| `Analyze{electionGuid}`                                     | AnalyzeHub — tally progress/complete                    |
| `FrontDesk{electionGuid}`                                   | FrontDeskHub — people, ballots, online election, reload |
| `BallotImport{electionGuid}` / `PeopleImport{electionGuid}` | import hubs (election-scoped)                           |
| `ElectionPackageImport{userId}`                             | ElectionPackageImportHub — dashboard package load log   |
| `Public`                                                    | PublicHub — guest-teller joinable elections list        |
| `AllVoters` (global)                                        | AllVotersHub — online voter list/window refresh         |
| `Voter{voterId}`                                            | VoterPersonalHub — registration + multi-device login     |

Frontend: `frontend/src/services/signalrService.ts` (`connectTo*Hub`, `joinElection`, `joinDashboardElections`, `connectVoterHubs`, etc.) and store subscriptions.

## MainHub status vs role groups

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #227 fix; `SignalRNotificationService.SendElectionUpdateAsync`; `MainHub.JoinElection`  
**Revisit when:** Known vs Guest need different status payloads (v3 `infoForKnown` / `infoForGuest`)

- Every client that joins an election is added to **base** `Main{electionGuid}` **and** either `…Known` or `…Guest`.
- Shared election status (name, stage) is broadcast as **`statusChanged`** to the **base** group only — one payload, no double delivery.
- Role-specific events use suffix groups (guest **`electionClosed`** → `Main{guid}Guest` only).
- Server producers use `IHubContext<MainHub>` via `SignalRNotificationService` (and computer-assignment close-out), not client-callable hub methods.

**Rejected alternative:** leave server event as `ElectionUpdated` while FE listens for `statusChanged` (broken live updates). **Rejected alternative:** keep unused hub methods `StatusChanged` / `ElectionClosed` / `CloseOutGuestTellers` as the documented push path — they were never called by services and were client-invokable.

## MainHub multi-election dashboard listen (JoinElections)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #230; v3 `JoinAll` / `Public/JoinMainHubAll`; `MainHub.JoinElections` / `LeaveElections`  
**Revisit when:** dashboard needs different events per role, or multi-join is needed outside the elections list

- **Who:** known (full) tellers only — guests must not multi-join (v3 parity).
- **When:** elections dashboard load joins all managed election GUIDs; unmount and logout leave the multi-join set.
- **Groups:** each allowed GUID → base `Main{guid}` + `Main{guid}Known` (status is on **base** after #227). No computer-code assignment.
- **Auth:** per-GUID membership via `JoinElectionUsers`; unauthorized GUIDs are skipped (not a hard fail of the whole batch).
- **FE:** `signalrService.joinDashboardElections` / `leaveDashboardElections`; reconnect re-invokes `JoinElections` for the tracked set. Leaving multi-join **excludes** the active workstation election (`mainElectionGuid`) so SignalR group membership is not dropped for the session open election (groups are not refcounted).
- **UI effect:** existing `statusChanged` → `electionStore.handleElectionUpdate` already patches list cards; multi-join only expands who receives the event.

**Rejected alternative:** loop `JoinElection` for every dashboard card. Rejected — that assigns computer codes and runs guest-join gates unsuitable for listen-only multi-election status.

**Rejected alternative:** leave multi-join membership for the whole SPA session without leave-on-navigate. Rejected — would keep clients in many groups after leaving the list; v3 scoped multi-listen to the elections list.

### Who owns Main vs FrontDesk membership

**Status:** active  
**Evidence:** confirmed (issue #242 — guest stage redirects stopped after leaving ballots)

- **Main hub** (`joinElection` / `leaveElection`): owned by `electionStore` / `MainLayout` for the active election session (`statusChanged`, computer code, guest close-out).
- **Main hub multi-listen** (`joinDashboardElections` / `leaveDashboardElections`): owned by `DashboardPage` for known tellers only (issue #230).
- **FrontDesk hub** (`joinFrontDeskElection` / `leaveFrontDeskElection`): owned by page stores that listen for FD events (`ballotStore`, `peopleStore`, Front Desk page).
- **Do not** call full `leaveElection` from ballots/people unmount — that drops Main group membership and stops stage updates for the rest of the session.

**Rejected alternative:** page-level stores share `joinElection`/`leaveElection` for convenience. Rejected — unmount of one page tears down session-level Main membership.

**Reason:** different surfaces need different fan-out and sometimes different membership (known vs guest). One flat `election-{guid}` group would over-notify or under-notify and couple unrelated UI areas.

**Rejected alternative:** one shared election group for every event type. Rejected because Main, Front Desk, Analyze, and Public have different listeners and update cadences.

## No election-scoped OnlineVotingHub (ballot totals)

**Status:** active  
**Evidence:** confirmed  
**Source:** maintainer review of unused scaffold vs v3 `AllVotersHub` / `VoterPersonalHub`; issue #233  
**Revisit when:** product wants per-election voter groups or ballot-submit live counts

There is still no `/hubs/online-voting` hub and no `online-election-{guid}` group that pushes vote totals. Ballot submit confirmation remains the HTTP response.

**Rejected alternative:** scaffolded `OnlineVotingHub` with `online-election-{electionGuid}` and client-callable `BallotSubmitted` (payload including `totalVotes`). Rejected — mismatched v3 voter model, would push vote totals rather than thin “refetch” signals.

## Online voter hubs (AllVoters + VoterPersonal) — issue #233

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #233; v3 `AllVotersHub` / `VoterPersonalHub`; preferred thin-signal shape above  
**Revisit when:** global `AllVoters` becomes too chatty, or kiosk multi-login needs different UX

Online voters use two authenticated hubs (policy `OnlineVoter` — JWT claims `voterType=online` + `voterId`). FE connects with `withCredentials` only; the httpOnly `voter_token` cookie is accepted on `/hubs/all-voters` and `/hubs/voter-personal` by the same JWT `OnMessageReceived` path as HTTP. There is no client `accessTokenFactory` for voters.

| Hub | Path | Group | Join | Events |
| --- | ---- | ----- | ---- | ------ |
| AllVotersHub | `/hubs/all-voters` | `AllVoters` (global) | `Join` / `Leave` | `updateVoters` — thin `OnlineElectionUpdateDto` (same fields as FrontDesk `updateOnlineElection`) |
| VoterPersonalHub | `/hubs/voter-personal` | `Voter{voterId}` | `Join` / `Leave` (group from JWT only) | `updateVoter` — thin `VoterPersonalUpdateDto` (`updateRegistration` / `login`) |

**Producers** (via `ISignalRNotificationService` only — hubs are join/leave):

- Online window/process change (`ElectionService` → `SendOnlineElectionUpdateAsync`) → FrontDesk **and** AllVoters.
- Front desk check-in / unregister / envelope (`FrontDeskService`) → personal `updateVoter` with `updateRegistration` to email, phone, and kiosk groups when present.
- Successful online voter auth (`OnlineVotingService`) → personal `updateVoter` with `login: true` (other browsers for that id).

**Client behavior:** thin signal → re-call `GET availableElections` / vote status. Do not treat hub payloads as authoritative election detail or tallies. Login-elsewhere shows a dismissible notice.

**Why global AllVoters (not per-election):** one join after auth covers list refresh for any election whose online window changes; eligibility filtering stays on `GET availableElections`. Per-election groups would miss elections the voter has not joined yet.

**Rejected alternative:** per-election voter groups only. Rejected for MVP — discovery of newly opened elections for an already-connected voter would require a second discovery channel.

**Security:** personal join never accepts a client-supplied voter id (server uses JWT `voterId`). Personal updates target only groups for that person's contact identifiers; voter A does not receive voter B events.

## No anonymous public results display

**Status:** active  
**Evidence:** confirmed  
**Source:** product decision (maintainer)  
**Revisit when:** a deliberate, authenticated presentation product requirement appears

Anonymous clients must not learn election results (or other election detail) just by knowing a GUID. `PublicHub` is only for the guest-teller join surface: the static `Public` group receives list open/close notifications used by teller join.

**Rejected alternative:** per-election `public-display-{guid}` groups + `GET /api/Public/{guid}/publicDisplay` + full-screen public results page. Removed — no business need for random users to view election data; authenticated results presentation (e.g. in-app results/presentation views) covers operator needs.

Election detail over HTTP follows the same rule: `GET /api/Elections/{guid}/status` (and other election-scoped routes) require `ElectionAccess` (full or guest teller joined to that election). Anonymous public endpoints stay limited to guest-join discovery (`/api/Public/elections`, hub `Public` group) and non-sensitive health/home.

## FrontDeskHub event catalog

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #232 fix; `SignalRNotificationService` FrontDesk methods; `peopleStore` / `FrontDeskPage` listeners  
**Revisit when:** changing person payload shape, or adding more FrontDesk producers

Server pushes only via `IHubContext<FrontDeskHub>` in `SignalRNotificationService` (and future service producers). The hub exposes **join/leave only** — no client-callable broadcast methods (same pattern as MainHub after #227).

Group: `FrontDesk{electionGuid}`.

| Event | Payload | Producer | Primary listeners |
| ----- | ------- | -------- | ----------------- |
| `PersonAdded` | `PersonUpdateDto` (`action: "added"`) | `SendPersonUpdateAsync` ← PeopleService create | `peopleStore` (list + ballot cache); Front Desk refreshes eligible list |
| `PersonUpdated` | `PersonUpdateDto` (`action: "updated"`) | `SendPersonUpdateAsync` ← PeopleService update | same |
| `PersonDeleted` | `PersonUpdateDto` (`action: "deleted"`) | `SendPersonUpdateAsync` ← PeopleService delete | same |
| `PersonCheckedIn` | `FrontDeskVoterDto` | `NotifyPersonCheckedInAsync` ← check-in / unregister / envelope | Front Desk page (row patch) |
| `PersonFlagsUpdated` | `FrontDeskVoterDto` | `SendPersonFlagsUpdatedAsync` | Front Desk page |
| `VoterCountUpdated` | `FrontDeskStatsDto` | `NotifyVoterCountUpdatedAsync` ← after check-in / unregister | Front Desk page (`frontDeskStats`) |
| `PersonVoteCountUpdated` | `PersonVoteCountUpdateDto` | `SendPersonVoteCountUpdateAsync` | `peopleStore` ballot cache |
| `updateBallots` | `BallotUpdateDto` | `SendBallotUpdateAsync` | `ballotStore` |
| `reloadPage` | (none) | `RequestFrontDeskReloadAsync` ← CSV / CDN ballot import success; people import success; delete-all-people | Front Desk page, `peopleStore`, `ballotStore` (soft re-fetch, not `location.reload`) |
| `updateOnlineElection` | `OnlineElectionUpdateDto` | `SendOnlineElectionUpdateAsync` ← `ElectionService.UpdateElectionAsync` when online fields change | `electionStore` (patch online fields); Monitoring dashboard re-fetches monitor info |

**Person list contract:** v4 uses fine-grained `PersonAdded` / `PersonUpdated` / `PersonDeleted`, not v3’s single `updatePeople` stream. Handlers refetch the affected person (or drop the row on delete) rather than applying a partial patch from the thin DTO.

**Rejected alternative:** re-emit v3 `updatePeople` from the notification service to match an old `peopleStore` listener. Rejected — other Front Desk events already use PascalCase names; handlers for add/update/delete already existed; dual event names would keep the contract ambiguous.

**Rejected alternative:** leave unused hub instance methods (`UpdatePeople`, `ReloadPage`, …) as the “documented” push path. Rejected — they were client-invokable, never called by services, and misled readers (same lesson as MainHub #227).

## FrontDesk `reloadPage` vs full browser reload (#228)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #228; v3 used `location.reload()` after ballot import; v4 prefers soft re-fetch  
**Revisit when:** a bulk op leaves client state that soft re-fetch cannot repair

- **Chosen:** server still emits the v3 event name `reloadPage` (no payload). Clients re-fetch people lists, ballots, and front-desk eligible voters/stats instead of forcing `window.location.reload()`.
- **Why:** full reload is disruptive on multi-station front desk and ballot entry; re-fetch keeps SignalR connections and dialog state.
- **Rejected alternative:** keep `location.reload()` for exact v3 parity. Rejected as unnecessary disruption when list/stats APIs already return authoritative post-import state.
- **Also wired:** `updateOnlineElection` with open/close/estimate/selection process when election online settings change (not on every unrelated election field update).
- **People import:** bulk people CSV/XLSX import does **not** emit per-row `PersonAdded` (would flood the hub). After successful import (or delete-all-people), the server fires the same `reloadPage` signal so open front desks re-fetch eligible voters — same end result as single-person CRUD, without N SignalR events.

## Import hub event catalog (#226)

**Status:** active  
**Evidence:** confirmed  
**Source:** issue #226; `SignalRNotificationService` import methods; `importStore` / `PeopleImportPage` listeners  
**Revisit when:** changing progress payload shape, or package-load group key model

Server pushes only via `IHubContext` in `SignalRNotificationService`. Import hubs expose **join/leave only** — no client-callable broadcast methods (same pattern as MainHub / FrontDeskHub after #227 / #232).

ASP.NET Core SignalR **event names are case-sensitive**. SPA listeners use **camelCase** only.

### BallotImportHub — group `BallotImport{electionGuid}`

| Event | Payload | Producer | Primary listeners |
| ----- | ------- | -------- | ----------------- |
| `importProgress` | `ImportProgressDto` (object: processedRows, totalRows, successCount, errorCount, currentStatus, percentComplete, …) | `SendImportProgressAsync` ← `ImportService` | `importStore` → `ImportProgressDialog` |
| `importError` | `(errorMessage, rowNumber)` two args | `SendImportErrorAsync` ← `ImportService` | `importStore` |
| `importComplete` | summary object (ballotsCreated, votesCreated, …) | `SendImportCompleteAsync` ← `ImportService` | `importStore` |

### PeopleImportHub — group `PeopleImport{electionGuid}`

| Event | Payload | Producer | Primary listeners |
| ----- | ------- | -------- | ----------------- |
| `importProgress` | `{ processed, total, status }` | `SendPeopleImportProgressAsync` ← `PeopleImportService` | `PeopleImportPage` |
| `importError` | `(errorMessage, rowNumber)` | `SendPeopleImportErrorAsync` ← `PeopleImportService` | `PeopleImportPage` |
| `importComplete` | `ImportPeopleResult` (success, peopleAdded, …) | `SendPeopleImportCompleteAsync` ← `PeopleImportService` | `PeopleImportPage` |

### ElectionPackageImportHub — group `ElectionPackageImport{userId}` (#231)

| Event | Payload | Producer | Primary listeners |
| ----- | ------- | -------- | ----------------- |
| `loaderStatus` | `(message, isTemporary)` two args | `SendElectionPackageLoaderStatusAsync` ← JSON / v3 package loaders | `DashboardPage` → `ElectionPackageLoadDialog` |

- **Who:** known tellers only (`JoinSession` rejects guests). Group is **user-scoped** (not election-scoped).
- **Why user-scoped:** package load creates a new election — no `electionGuid` yet; concurrent known tellers must not steal each other’s stream (v3 `Import{loginId}` parity).
- **Temp lines:** `isTemporary: true` may replace the last temporary log line on the client (v3 scrolling log behavior).
- **Hub path:** `/hubs/election-package-import`. FE: `signalrService.connectToElectionPackageImportHub` / `joinElectionPackageImportSession`.

**Rejected alternative:** reuse PeopleImportHub / BallotImportHub with election groups. Rejected — no election exists at start of package load; would require inventing a session GUID or broadcasting to the wrong audience.

**Rejected alternative:** dual path — loaders calling `IHubContext` directly while people/ballot import use the notification service. Rejected — same case-sensitive / producer-drift lesson as #226.

**Rejected alternative:** dual path — `ImportService` / `PeopleImportService` calling `IHubContext` directly while `SendImportProgressAsync` used different PascalCase names (`ImportProgress` / `ImportComplete`). Rejected — case-sensitive mismatch would miss SPA listeners; two producers drift. **Chosen:** one producer (`ISignalRNotificationService`) with camelCase event names matching the SPA.
