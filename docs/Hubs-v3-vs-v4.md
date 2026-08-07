# SignalR hubs: v3 vs v4

Companion to [Hubs-in-v3.md](./Hubs-in-v3.md). Summarizes how TallyJ 4’s realtime model differs from v3, what is already covered, and what still needs work.

**Sources:** v3 hub reference, `backend/Hubs/*`, `SignalRNotificationService`, frontend `signalrService` + stores, `context/realtime.md`.

**Tracking issue:** [glittle/TallyJ-4#234](https://github.com/glittle/TallyJ-4/issues/234)

**Explicitly deferred:** RollCallHub (not used in v4 yet).

---

## Hub inventory

| v3 hub | v4 | Notes |
|--------|----|--------|
| MainHub | `MainHub` (`/hubs/main`) | Present; status event wiring gap |
| PublicHub | `PublicHub` (`/hubs/public`) | Present; guest-teller join list only |
| FrontDeskHub | `FrontDeskHub` (`/hubs/front-desk`) | Present; richer events; some producers/listeners missing |
| AnalyzeHub | `AnalyzeHub` (`/hubs/analyze`) | Present; structured tally progress |
| ImportHub (people CSV) | `PeopleImportHub` (`/hubs/people-import`) | Renamed; election-scoped groups |
| ImportHub (election package load) | — | Not ported |
| BallotImportHub | `BallotImportHub` (`/hubs/ballot-import`) | Present; election-scoped groups |
| RollCallHub | — | Deferred |
| AllVotersHub | — | Not implemented (online voting HTTP-only for now) |
| VoterPersonalHub | — | Not implemented |
| VoterCodeHub | — | Not implemented |
| *(scaffold)* OnlineVotingHub | — | Removed on purpose; see `context/realtime.md` |

Mapped in `backend/Program.cs` as `/hubs/main`, `/analyze`, `/ballot-import`, `/people-import`, `/front-desk`, `/public`.

---

## Architectural differences

### Stack and connection model

| | v3 | v4 |
|--|----|----|
| Library | ASP.NET SignalR 2 (`/signalr`) | ASP.NET Core SignalR (`/hubs/*`) |
| Client connections | One shared hub connection | Separate connection per hub path |
| Join | AJAX MVC actions with `connId` | Client invokes hub methods (`JoinElection`, etc.) |
| Hub shape | `XxxHub` helper + empty `XxxHubCore` | Single hub class per endpoint |
| Server push | Domain code → hub helpers | Mostly `ISignalRNotificationService` (+ direct `IHubContext` in import services) |

### Security

| | v3 | v4 |
|--|----|----|
| Hub `[Authorize]` | None on `*HubCore` | `MainHub` is `[Authorize]`; Public stays anonymous |
| Isolation | Join-endpoint auth + session + groups | JWT/auth + groups; guest join gated when no main teller |
| Group names | Server-derived from session | Server-derived; client passes election GUID on join |

### Group naming

| Pattern | v3 | v4 |
|---------|----|----|
| Main (base) | — (Known/Guest only) | `Main{electionGuid}` **and** Known/Guest suffixes |
| Main Known/Guest | `Main{guid}Known` / `…Guest` | Same |
| Front desk | `FrontDesk{electionGuid}` | Same |
| Analyze | `Analyze{electionGuid}` | Same |
| Public | `Public` | Same |
| People import | `Import{loginId}` (shared name pattern with ballot import, different hub type) | `PeopleImport{electionGuid}` |
| Ballot import | `Import{loginId}` | `BallotImport{electionGuid}` |
| Roll call | `RollCall{electionGuid}` | — |
| All voters | `AllVoters` (global) | — |
| Voter personal | `Voter{voterId}` | — |
| Voter code | Client opaque key | — |

**Import scope change:** v3 scoped progress to the **login**; v4 scopes people/ballot import to the **election**. Concurrent tellers on the same election share the stream.

### MainHub extras in v4

- Computer code assignment is part of `JoinElection` (`IComputerAssignmentService`).
- Guest cannot join unless a main teller is connected (`CanGuestJoin`).
- Clients join both base `Main{guid}` and role suffix group.

### Front desk event model

- **v3:** Primary stream was `updatePeople`, plus `reloadPage` and `updateOnlineElection`.
- **v4:** Finer-grained events (`PersonCheckedIn`, `PersonFlagsUpdated`, `PersonVoteCountUpdated`, `PersonAdded`/`PersonUpdated`/`PersonDeleted`, `updateBallots`, …). Some v3 method names remain on the hub but are not fully wired.

### Online voting and public results

Documented in `context/realtime.md`:

- No election-scoped OnlineVotingHub; online voting is HTTP (`/api/online-voting/*`).
- PublicHub is **only** for guest-teller joinable elections — not anonymous results display.
- Preferred restore shape for voter realtime: thin “refetch” signals, not tallies or rich private data on the wire.

---

## What already works (v3 parity-ish)

| Capability | How in v4 |
|------------|-----------|
| Guest force-out when election closed/unlisted | `CloseOutGuestTellers` → `electionClosed` on Guest group; FE logs guest out |
| Public joinable election list live | PublicHub + `ElectionListUpdated` on teller-join page |
| Front desk check-in / flags / vote counts | FrontDesk producers + Front Desk page / vote-count listeners |
| People CSV import progress | PeopleImportHub + service `IHubContext` |
| Ballot import progress | BallotImportHub + `ImportService` (camelCase events) |
| Analysis / tally progress | AnalyzeHub: `tallyProgress` / `tallyComplete` / `statusUpdate` |

---

## Gaps and bugs

### A. Wiring mismatches (hubs exist; events don’t line up)

These look “implemented” until exercised.

#### 1. MainHub election status

| Layer | Actual |
|-------|--------|
| Notification service | Sends **`ElectionUpdated`** to base `Main{guid}` |
| Hub method `StatusChanged` | Would send **`statusChanged`** to Known/Guest; **unused** |
| Frontend `electionStore` | Listens for **`statusChanged` only** |

Guest close-out works; routine status/stage updates likely do not reach the SPA.

→ Issue [#227](https://github.com/glittle/TallyJ-4/issues/227)

#### 2. Person list updates

| Layer | Actual |
|-------|--------|
| Notification service | `PersonAdded` / `PersonUpdated` / `PersonDeleted` |
| `peopleStore` | Listens for **`updatePeople`** |
| Front desk page | `PersonCheckedIn`, `PersonFlagsUpdated` (good) |
| `VoterCountUpdated` | Server sends; front desk UI does **not** subscribe |

→ Issue [#232](https://github.com/glittle/TallyJ-4/issues/232)

#### 3. `updateOnlineElection` and post-import `reloadPage`

| Piece | Status |
|-------|--------|
| Hub methods | Exist on FrontDeskHub |
| Server producers | **None** for online window or post-import reload |
| FE `reloadPage` | Handled in some stores; never triggered after import |
| FE `updateOnlineElection` | No listener |

→ Issue [#228](https://github.com/glittle/TallyJ-4/issues/228)

#### 4. Ballot import progress dual path

| Path | Event names |
|------|-------------|
| `ImportService` → hub | camelCase `importProgress` / `importComplete` (matches FE) |
| `SendImportProgressAsync` | PascalCase `ImportProgress` / `ImportComplete` (would miss FE) |

→ Issue [#226](https://github.com/glittle/TallyJ-4/issues/226)

### B. Operator feature gaps

| Gap | v3 | v4 | Issue |
|-----|----|----|-------|
| Multi-election dashboard listen | MainHub `JoinAll` (known tellers) | Only current election | [#230](https://github.com/glittle/TallyJ-4/issues/230) |
| Election package load progress | ImportHub `loaderStatus` | No SignalR on load path | [#231](https://github.com/glittle/TallyJ-4/issues/231) |

### C. Online voter realtime (product-gated)

| Gap | v3 | v4 | Issue |
|-----|----|----|-------|
| All voters online window / process | AllVotersHub | HTTP only | [#233](https://github.com/glittle/TallyJ-4/issues/233) |
| Personal registration / multi-login | VoterPersonalHub | — | [#233](https://github.com/glittle/TallyJ-4/issues/233) |
| Code delivery live status | VoterCodeHub | `requestCode` / `verifyCode` only | [#229](https://github.com/glittle/TallyJ-4/issues/229) |

If restored: prefer server-derived groups, high-entropy channel tokens for code status (not short client keys), and thin refetch signals — see `context/realtime.md`.

### D. Deferred

| Gap | Notes |
|-----|--------|
| RollCallHub | Live roll-call display as registrations change. Not filed; add when product needs it. |

---

## v3 “must not lose” matrix (status)

| Capability | Hub(s) in v3 | v4 status |
|------------|--------------|-----------|
| Teller UI live election status | MainHub | **Partial** — guest close OK; status event mismatch |
| Guest kick on close/unlist | MainHub | **Yes** |
| Public landing joinable list | PublicHub | **Yes** |
| Front desk registration live | FrontDeskHub | **Mostly** — check-in/flags/counts; person CRUD event mismatch |
| Roll call live | RollCallHub | **Deferred** |
| Monitor online window | FrontDeskHub | **No** (no producer/listener) |
| Voters online open/close | AllVotersHub | **No** |
| Voter personal / multi-login | VoterPersonalHub | **No** |
| Voter code delivery status | VoterCodeHub | **No** |
| CSV import progress | ImportHub | **Yes** (PeopleImportHub) |
| Election-load progress | ImportHub | **No** |
| Ballot import progress + FD reload | BallotImportHub + FrontDesk | Progress **yes**; FD reload **no** |
| Results analysis progress | AnalyzeHub | **Yes** |

---

## Suggested fix order

1. [#227](https://github.com/glittle/TallyJ-4/issues/227) Main status wiring  
2. [#232](https://github.com/glittle/TallyJ-4/issues/232) Person event alignment  
3. [#228](https://github.com/glittle/TallyJ-4/issues/228) Online window + import reload  
4. [#226](https://github.com/glittle/TallyJ-4/issues/226) Import event name cleanup  
5. [#230](https://github.com/glittle/TallyJ-4/issues/230) Multi-election join  
6. [#231](https://github.com/glittle/TallyJ-4/issues/231) Election load progress  
7. [#233](https://github.com/glittle/TallyJ-4/issues/233) / [#229](https://github.com/glittle/TallyJ-4/issues/229) when online voter UX needs live push  

---

## Related files (v4)

| Area | Paths |
|------|--------|
| Hubs | `backend/Hubs/*.cs` |
| Map | `backend/Program.cs` (`MapHub`) |
| Notifications | `backend/Services/SignalRNotificationService.cs`, `ISignalRNotificationService.cs` |
| Frontend service | `frontend/src/services/signalrService.ts` |
| Stores | `electionStore`, `peopleStore`, `importStore`, `ballotStore`, `resultStore` |
| Why / decisions | `context/realtime.md` |
| v3 reference | `docs/Hubs-in-v3.md` |

---

*Written from a v3↔v4 hub comparison for rewrite tracking. Update this file when gaps are closed or decisions change.*
