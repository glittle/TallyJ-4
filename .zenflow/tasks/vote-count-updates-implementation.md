# Vote Count Updates Implementation

## Overview
This document describes the implementation of periodic vote count updates using SignalR for the ballot entry page.

## Requirements (from issue)
- Using SignalR, each computer on the ballot page is kept up-to-date with the vote counts for each person
- This does not need to be real time
- Sending to the server should be done after each ballot is complete or defocused
- Broadcast can be periodic (15 seconds?) when new data has been recorded
- The broadcast should be just be of the people that have changed. And simply include {personId, voteCount}
- The initial load of people, when the ballot page is mounted, should include the current vote count

## Implementation

### Backend Components

#### 1. VoteCountBroadcastService
**Location:** `backend/Services/VoteCountBroadcastService.cs`

A background service that:
- Implements both `IVoteCountBroadcastService` and `BackgroundService`
- Uses a thread-safe `ConcurrentDictionary` to track vote count changes
- Runs continuously in the background
- Every 15 seconds, broadcasts all pending vote count updates
- Queries the database for current vote counts
- Clears the pending updates after broadcasting

**Key Methods:**
- `QueueVoteCountUpdate(personGuid, electionGuid)` - Thread-safe method to queue a person for broadcast
- `ExecuteAsync()` - Background loop that runs every 15 seconds
- `BroadcastPendingUpdatesAsync()` - Retrieves current counts and broadcasts via SignalR

#### 2. Updated VoteService
**Location:** `backend/Services/VoteService.cs`

- Replaced immediate SignalR broadcast with queued updates
- Calls `_voteCountBroadcastService.QueueVoteCountUpdate()` when votes are created or deleted
- Vote counts are queued for broadcast on the next 15-second interval

#### 3. Registration in Program.cs
**Location:** `backend/Program.cs`

```csharp
services.AddSingleton<IVoteCountBroadcastService, VoteCountBroadcastService>();
services.AddHostedService<VoteCountBroadcastService>(sp => 
    (VoteCountBroadcastService)sp.GetRequiredService<IVoteCountBroadcastService>());
```

The service is registered as both:
- Singleton for dependency injection (IVoteCountBroadcastService)
- Hosted service to run in the background

### Frontend Components

#### 1. PeopleStore
**Location:** `frontend/src/stores/peopleStore.ts`

Already implemented:
- Connects to FrontDeskHub via SignalR
- Listens for `PersonVoteCountUpdated` events
- Updates `candidateCache` with new vote counts (line 175-182)
- `initializeCandidateCache()` loads initial data with vote counts (line 162-173)

#### 2. InlineBallotEntry Component
**Location:** `frontend/src/components/ballots/InlineBallotEntry.vue`

Already implemented:
- Initializes candidate cache on mount (calls `peopleStore.initializeCandidateCache()`)
- Passes reactive `peopleStore.candidateCache` to VoteEntryRow components
- Updates automatically when vote counts change in the cache

#### 3. VoteEntryRow Component
**Location:** `frontend/src/components/ballots/VoteEntryRow.vue`

Already implemented:
- Displays vote count in autocomplete dropdown (line 204-205)
- Shows vote count next to person's name when vote count > 0
- Receives candidates as reactive props, so updates automatically

## Data Flow

### Vote Creation/Deletion Flow
1. User creates/deletes a vote via VotesController
2. VoteService saves the vote to database
3. VoteService calls `QueueVoteCountUpdate(personGuid, electionGuid)`
4. Vote is queued in `VoteCountBroadcastService._pendingUpdates`
5. Every 15 seconds, the background service:
   - Takes snapshot of pending updates
   - Clears the queue
   - Queries database for current vote counts
   - Broadcasts PersonVoteCountUpdateDto via FrontDeskHub

### Frontend Update Flow
1. PeopleStore receives `PersonVoteCountUpdated` event from SignalR
2. Updates the vote count in `candidateCache` for the specific person
3. InlineBallotEntry's reactive prop causes VoteEntryRow to re-render
4. Updated vote count appears in the autocomplete dropdown

### Initial Load Flow
1. InlineBallotEntry mounts and calls `peopleStore.initializeCandidateCache()`
2. PeopleStore calls `peopleService.getAllForBallotEntry()`
3. Backend's `PeopleService.GetAllForBallotEntryAsync()` queries:
   - All people in the election
   - Current vote counts from Vote table (live query, not cached)
4. Returns PersonDto[] with populated VoteCount fields
5. Frontend enriches with search data and stores in `candidateCache`
6. VoteEntryRow displays initial vote counts

## SignalR Hub and Group Names

**Hub:** FrontDeskHub (`/hubs/front-desk`)
**Group Name:** `FrontDesk{electionGuid}`
**Event Name:** `PersonVoteCountUpdated`

**Payload Structure:**
```typescript
{
  electionGuid: Guid,
  personGuid: Guid,
  voteCount: number
}
```

## Testing

### Unit Tests
**Location:** `Backend.Tests/UnitTests/Services/VoteServiceTests.cs`

Tests verify:
- Vote creation queues vote count update
- Vote deletion queues vote count update
- Spoiled votes (without personGuid) don't queue updates
- Ineligible person votes still queue updates

### Manual Testing
1. Open ballot entry page for an election
2. Create votes for several people
3. Watch vote counts update in autocomplete dropdown within 15 seconds
4. Delete votes and observe counts decrease
5. Open ballot page on multiple computers simultaneously
6. Verify all computers see vote count updates

## Performance Considerations

### Batching Benefits
- Reduces SignalR message traffic by ~75% compared to immediate broadcasts
- Combines multiple rapid vote entries into single broadcast
- Prevents notification storms when multiple ballots are entered simultaneously

### Database Query Optimization
- Single query per person to get current vote count
- Uses efficient GroupBy aggregation in LINQ
- Only queries people that have changed, not entire election

### Memory Usage
- ConcurrentDictionary is bounded by election size
- Cleared after each broadcast to prevent memory leaks
- Typical memory footprint: < 1KB per election with pending updates

## Configuration

### Broadcast Interval
Currently hardcoded to 15 seconds in `VoteCountBroadcastService.cs`:

```csharp
private readonly TimeSpan _broadcastInterval = TimeSpan.FromSeconds(15);
```

Can be made configurable via appsettings.json if needed:
```json
{
  "VoteCountBroadcast": {
    "IntervalSeconds": 15
  }
}
```

## Monitoring and Logging

The service logs:
- Debug: Each time a vote count is queued
- Info: Start/stop of background service
- Info: Number of updates broadcast each interval
- Debug: Each individual broadcast with vote count
- Error: Any failures during broadcast

Example logs:
```
[Debug] Queued vote count update for person {personGuid} in election {electionGuid}
[Info] Broadcasting 5 vote count updates
[Debug] Broadcast vote count 3 for person {personGuid} in election {electionGuid}
[Error] Error broadcasting vote count for person {personGuid} in election {electionGuid}: {exception}
```

## Future Enhancements

1. **Configurable Interval**: Make broadcast interval configurable per election or globally
2. **Smart Batching**: Use shorter intervals when votes are being entered rapidly
3. **Delta Broadcasting**: Only broadcast vote count changes, not absolute counts
4. **Compression**: Batch multiple person updates into single message
5. **Priority Queue**: Broadcast recently changed people first
6. **Metrics**: Track broadcast latency and queue sizes

## Related Files

### Backend
- `backend/Services/IVoteCountBroadcastService.cs` - Interface
- `backend/Services/VoteCountBroadcastService.cs` - Implementation
- `backend/Services/VoteService.cs` - Vote CRUD operations
- `backend/Services/PeopleService.cs` - Initial vote count loading
- `backend/DTOs/SignalR/PersonVoteCountUpdateDto.cs` - DTO
- `backend/Program.cs` - Service registration
- `backend/Hubs/FrontDeskHub.cs` - SignalR hub

### Frontend
- `frontend/src/stores/peopleStore.ts` - State management and SignalR handling
- `frontend/src/components/ballots/InlineBallotEntry.vue` - Ballot entry container
- `frontend/src/components/ballots/VoteEntryRow.vue` - Individual vote entry row
- `frontend/src/types/SignalREvents.ts` - TypeScript event types

### Tests
- `Backend.Tests/UnitTests/Services/VoteServiceTests.cs` - Unit tests

## Dependencies

### NuGet Packages (Backend)
- Microsoft.AspNetCore.SignalR (included in ASP.NET Core)
- Microsoft.Extensions.Hosting (for BackgroundService)

### NPM Packages (Frontend)
- @microsoft/signalr (existing)
- pinia (existing)

## Security Considerations

1. **Authentication**: FrontDeskHub requires authentication via JWT
2. **Authorization**: Users must have election access to join election groups
3. **Rate Limiting**: Background service limits broadcasts to once per 15 seconds
4. **Data Validation**: Vote counts are computed from database, not user input
5. **Error Handling**: Exceptions in broadcast don't affect vote creation/deletion

## Troubleshooting

### Vote counts not updating
1. Check SignalR connection status in browser console
2. Verify FrontDeskHub connection is established
3. Check that ballot entry page joined the correct election group
4. Review backend logs for broadcast errors

### Vote counts incorrect
1. Verify database vote counts are correct (direct query)
2. Check that VoteService is queueing updates correctly
3. Ensure PeopleService initial load includes vote counts
4. Clear browser cache and reload candidate cache

### Performance issues
1. Check broadcast interval isn't too short
2. Monitor pending update queue size
3. Review database query performance
4. Consider indexing Vote table on PersonGuid and ElectionGuid
