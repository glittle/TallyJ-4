# Technical Specification: Real-time Features (SignalR) Integration

## Technical Context
- **Frontend**: Vue 3 + TypeScript + Pinia stores
- **Backend**: .NET 9.0 ASP.NET Core with SignalR hubs
- **Real-time Protocol**: SignalR with automatic reconnection
- **Authentication**: JWT tokens for hub connections

## Implementation Approach

### Current State Analysis
- SignalR infrastructure is fully implemented in backend (5 hubs: Main, Analyze, BallotImport, FrontDesk, Public)
- Frontend has SignalR service layer and composables
- Stores have SignalR initialization methods but are not called in pages
- Real-time events are defined but not integrated into UI components

### Integration Strategy
1. **Page-level SignalR Initialization**: Initialize SignalR connections when entering election-specific pages
2. **Group Management**: Join/leave election groups for targeted broadcasts
3. **Real-time UI Updates**: Display progress bars, live data updates, and notifications
4. **Connection Lifecycle**: Handle connection states and cleanup on page navigation

## Source Code Structure Changes

### Modified Files
- `frontend/src/pages/elections/ElectionDetailPage.vue` - Initialize election SignalR
- `frontend/src/pages/results/TallyCalculationPage.vue` - Add real-time progress display
- `frontend/src/pages/people/PeopleManagementPage.vue` - Real-time people updates
- `frontend/src/pages/ballots/BallotEntryPage.vue` - Live ballot status updates
- `frontend/src/stores/electionStore.ts` - Join election groups
- `frontend/src/stores/resultStore.ts` - Tally progress integration
- `frontend/src/stores/peopleStore.ts` - People updates integration

### New Components (if needed)
- `ProgressDisplay.vue` - Reusable progress bar component
- `RealtimeIndicator.vue` - Connection status indicator

## Data Model / API / Interface Changes
- No changes required - SignalR events and DTOs already defined
- Existing interfaces: `TallyProgressEvent`, `ElectionUpdateEvent`, `PersonUpdateEvent`

## Verification Approach

### Testing Strategy
1. **Unit Tests**: SignalR service connection/disconnection logic
2. **Integration Tests**: Hub method invocations and event handling
3. **Manual Testing**:
   - Open multiple browser tabs
   - Start tally calculation in one tab
   - Verify progress updates in real-time across tabs
   - Test people updates, election status changes
   - Test connection recovery after network interruption

### Test Commands
```bash
# Backend tests
cd backend
dotnet test

# Frontend tests  
cd frontend
npm run test:unit

# E2E tests (if implemented)
npm run test:e2e
```

### Success Criteria
- Real-time tally progress display during calculation
- Live updates when people are added/modified/deleted
- Election status changes reflected immediately across clients
- Automatic reconnection after network issues
- No performance degradation with SignalR enabled