# Ballot Entry – Technical Specification

## Difficulty: Hard

Multiple interacting concerns: backend data-model changes, live SignalR broadcast, eligibility-based spoiled votes, in-browser fuzzy search re-sorting, and UX keyboard navigation across several frontend components.

---

## Technical Context

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10 ASP.NET Core, EF Core, SignalR, AutoMapper |
| Frontend | Vue 3 (Composition API), TypeScript, Pinia, Element Plus, `@microsoft/signalr` |
| Real-time | FrontDeskHub (group `FrontDesk{electionGuid}`) |
| Testing | xUnit (backend), Vitest (frontend) |

---

## Current State & Gaps

### 1. Candidate search list contains only eligible people
`peopleStore.initializeCandidateCache` calls `GET /api/people/{guid}/getCandidates` which filters `CanReceiveVotes == true`. The task requires **all** people in the election to be searchable.

### 2. Ineligible person → exception, not spoiled vote
`VoteService.CreateVoteAsync` throws `InvalidOperationException` when `CanReceiveVotes != true`. The task requires the vote to be created with `StatusCode` set to the person's `IneligibleReasonCode` (e.g. `X01`, `R02`, `V06`).

### 3. Vote count is tally-result, not live ballot count
`PersonDto.VoteCount` is populated from the `Result` table (tally output). The task requires a **live** count of how many current ballots contain that person — sourced directly from the `Vote` table.

### 4. No SignalR broadcast when a vote is added/deleted
After a vote is created or deleted, other ballot-entry clients must receive the updated live vote count for the affected person so their candidate lists sort correctly.

### 5. Search result ordering ignores vote count
`usePersonSearch` sorts by relevance weight then alphabetically. The task requires **primary sort by `voteCount` descending**, secondary by relevance weight.

### 6. VoteEntryRow dropdown items don't show vote count
The autocomplete items show only the person name; the live ballot count must be visible.

### 7. VoteEntryRow keyboard navigation
Arrow-key navigation in the dropdown and Enter-to-select are partly provided by `el-autocomplete` but need verification. The current implementation may have focus issues after selection.

---

## Implementation Approach

### Backend

#### A. New endpoint – all people for ballot entry
Add `GET /api/people/{electionGuid}/getAllForBallotEntry` which returns **all** people (no eligibility filter), with `VoteCount` computed live from the `Vote` table (not `Result` table).

The live count query:
```csharp
var liveVoteCounts = await _context.Votes
    .Where(v => v.PersonGuid != null && v.Ballot.Location.ElectionGuid == electionGuid)
    .GroupBy(v => v.PersonGuid!.Value)
    .Select(g => new { PersonGuid = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.PersonGuid, x => x.Count);
```

#### B. Spoiled votes for ineligible persons
Remove the `CanReceiveVotes` guard in `VoteService.CreateVoteAsync`. Instead:
- If person exists but `CanReceiveVotes != true`, look up their `IneligibleReasonCode` from `IneligibleReasonEnum` and override `StatusCode`.
- The backend is the authoritative source; the frontend also pre-fills `statusCode` for UX clarity.

#### C. Live vote count SignalR broadcast
Add `PersonVoteCountUpdateDto` and `SendPersonVoteCountUpdateAsync` to the notification service. After any vote is created or deleted, `VoteService` computes the new live vote count for the affected `personGuid` and broadcasts it via `FrontDeskHub` group.

`VoteService` needs `ISignalRNotificationService` injected.

---

### Frontend

#### D. peopleService – new method
Add `getAllForBallotEntry(electionGuid)` calling the new backend endpoint.

#### E. peopleStore – broader cache + vote count updates
- `initializeCandidateCache` calls `getAllForBallotEntry` instead of `getCandidates`.
- `initializeSignalR` listens for new `PersonVoteCountUpdated` hub event and updates the matching entry in `candidateCache.voteCount`.

#### F. usePersonSearch – sort by vote count first
Change the sort comparator: primary `b.person.voteCount - a.person.voteCount`, secondary existing weight comparison.

#### G. VoteEntryRow – UX improvements
- Autocomplete dropdown item template shows vote count as a small badge/pill on the right.
- Ineligible persons (canReceiveVotes != true) shown with a distinct style (e.g., colour or icon).
- When an ineligible person is selected, `statusCode` in the emitted vote is set to `person.ineligibleReasonCode`.
- Keyboard navigation: arrow keys move through dropdown; Enter selects. This is native to `el-autocomplete`; ensure `disabled` state is not applied while a candidate is being confirmed (current implementation disables input once person selected — correct, no change needed).
- After a name is selected and the vote emitted, `searchQuery` is kept as the person's name (readonly display) and focus advances to the next row via `InlineBallotEntry`.

#### H. InlineBallotEntry / BallotEntryPage – spoiled vote creation
- `InlineBallotEntry.handleVoteSelected` passes the `statusCode` from the emitted VoteDto up through `BallotEntryPage.handleVoteAdded → ballotStore.createVote`.
- `CreateVoteDto.statusCode` is already optional; just ensure the frontend correctly passes the value from the vote.
- Success message distinguishes spoiled vs. normal votes (translation key already exists; may need new keys).

---

## Source Code Changes

### Backend – New / Modified Files

| File | Change |
|------|--------|
| `backend/DTOs/SignalR/PersonVoteCountUpdateDto.cs` | NEW – `{ ElectionGuid, PersonGuid, VoteCount }` |
| `backend/Services/ISignalRNotificationService.cs` | Add `SendPersonVoteCountUpdateAsync(PersonVoteCountUpdateDto)` |
| `backend/Services/SignalRNotificationService.cs` | Implement method; broadcasts `PersonVoteCountUpdated` to `FrontDesk{electionGuid}` group |
| `backend/Services/IVoteService.cs` | Add `GetLiveVoteCountAsync(Guid personGuid, Guid electionGuid): Task<int>` (optional helper) |
| `backend/Services/VoteService.cs` | Inject `ISignalRNotificationService`; change ineligible-person guard to set statusCode; broadcast live count after create/delete |
| `backend/Services/IPeopleService.cs` | Add `GetAllForBallotEntryAsync(Guid electionGuid)` |
| `backend/Services/PeopleService.cs` | Implement `GetAllForBallotEntryAsync` – no eligibility filter, live vote count from Vote table |
| `backend/Controllers/PeopleController.cs` | Add `GET {electionGuid}/getAllForBallotEntry` endpoint |

### Frontend – New / Modified Files

| File | Change |
|------|--------|
| `frontend/src/types/SignalREvents.ts` | Add `PersonVoteCountUpdateEvent { electionGuid, personGuid, voteCount }` |
| `frontend/src/services/peopleService.ts` | Add `getAllForBallotEntry(electionGuid)` method |
| `frontend/src/stores/peopleStore.ts` | Update `initializeCandidateCache` to call `getAllForBallotEntry`; add SignalR handler for `PersonVoteCountUpdated` |
| `frontend/src/composables/usePersonSearch.ts` | Change sort: primary `voteCount` desc, secondary weight desc |
| `frontend/src/components/ballots/VoteEntryRow.vue` | Show voteCount in dropdown; ineligible styling; set statusCode on select for ineligible |
| `frontend/src/components/ballots/InlineBallotEntry.vue` | Pass statusCode from VoteDto through to emit |
| `frontend/src/pages/ballots/BallotEntryPage.vue` | Pass statusCode when creating vote; show appropriate success/info message |
| `frontend/src/locales/en/ballots.json` | Add keys: `ballots.voteSpoiledSuccess`, `ballots.ineligible` |
| `frontend/src/locales/fr/ballots.json` | Mirror new keys in French |

---

## Data Model / API / Interface Changes

### New backend DTO
```csharp
// backend/DTOs/SignalR/PersonVoteCountUpdateDto.cs
public class PersonVoteCountUpdateDto
{
    public Guid ElectionGuid { get; set; }
    public Guid PersonGuid { get; set; }
    public int VoteCount { get; set; }
}
```

### New SignalR event (frontend)
```ts
// frontend/src/types/SignalREvents.ts
export interface PersonVoteCountUpdateEvent {
  electionGuid: string;
  personGuid: string;
  voteCount: number;
}
```

### New backend endpoint
```
GET /api/people/{electionGuid}/getAllForBallotEntry
→ ApiResponse<List<PersonDto>>
  All people in the election; VoteCount = live count from Vote table
```

### Vote creation – spoiled status
- Backend auto-sets `StatusCode = person.IneligibleReasonCode` if person can't receive votes.
- Frontend pre-fills same value in `CreateVoteDto.statusCode` for immediate UI feedback (no waiting for server).
- No new DTO fields required (existing `statusCode` on `CreateVoteDto` is sufficient).

---

## Verification Approach

### Backend
```bash
cd backend && dotnet build
cd .. && dotnet test
```
Key backend tests to add (in `TallyJ4.Tests`):
- `VoteService_CreateVote_IneligiblePerson_CreatesSpoiledVote` – verify statusCode is set to ineligibility code rather than throwing.
- `VoteService_CreateVote_BroadcastsVoteCountUpdate` – mock `ISignalRNotificationService`, verify `SendPersonVoteCountUpdateAsync` called after create.
- `VoteService_DeleteVote_BroadcastsVoteCountUpdate` – same for delete.
- `PeopleService_GetAllForBallotEntry_ReturnsAllPeople` – verify ineligible persons included.
- `PeopleService_GetAllForBallotEntry_VoteCount_IsLiveFromVoteTable` – verify count comes from Vote, not Result.

### Frontend
```bash
cd frontend && npx vue-tsc --noEmit
npm run test
```
Key frontend test additions:
- `usePersonSearch.spec.ts` – add test asserting results sorted by voteCount desc then weight.
- `VoteEntryRow` – verify ineligible person selection emits correct `statusCode`.
- `peopleStore` – verify `handlePersonVoteCountUpdated` updates `candidateCache`.

---

## Key Design Decisions

1. **Single new endpoint vs. modifying getCandidates** – New `getAllForBallotEntry` endpoint keeps `getCandidates` unchanged (still used by other parts of the app).

2. **Live vote count from Vote table vs. Result table** – Result table is only populated after running the tally process. Ballot entry needs real-time data from the Vote table.

3. **Backend auto-corrects statusCode for ineligible** – Even if frontend passes wrong status, backend enforces correctness. This makes frontend pre-fill an optimistic UX enhancement, not a security boundary.

4. **SignalR event name `PersonVoteCountUpdated`** – Sent on `FrontDeskHub` to `FrontDesk{electionGuid}` group, which is already joined by all ballot entry clients. No new hub is needed.

5. **Sort order** – `voteCount desc` first (as specified), then relevance weight (preserving existing fuzzy/phonetic logic). Within same voteCount and weight, alphabetical by lastName.
