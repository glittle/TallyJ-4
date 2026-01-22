# Technical Specification: TallyJ4 - Phase 5: Real-time Features (SignalR)

## 1. Overview

**Phase**: Phase 5 - Real-time Features (SignalR)  
**Objective**: Implement real-time bidirectional communication for live election updates, ballot entry synchronization, voter notifications, and progress updates during tally calculations.

**Complexity**: **Hard** - Complex real-time architecture with multiple hubs, connection management, authentication, group isolation, and frontend-backend integration.

---

## 2. Technical Context

### 2.1 Technology Stack

**Backend**:
- .NET 10.0 ASP.NET Core Web API
- Microsoft.AspNetCore.SignalR v1.2.0 (already installed)
- JWT Bearer authentication (existing)
- Entity Framework Core 10.0 (existing)

**Frontend**:
- Vue 3.5.22 (Composition API)
- TypeScript 5.9.3
- @microsoft/signalr v9.0.6 (already installed)
- Pinia 3.0.3 (state management)
- Existing stores: authStore, electionStore, peopleStore, ballotStore, resultStore

**Current State**:
- ✅ Phase 1: Database Foundation - Complete
- ✅ Phase 2: API Layer - Complete (8 controllers, 40+ endpoints)
- ✅ Phase 3: Advanced Features - Complete (tally algorithms)
- ✅ Phase 4: Frontend Application - Complete (Vue 3 SPA)
- 🚧 Phase 5: Real-time Features (SignalR) - **Current Phase**

### 2.2 Original System Analysis

From reverse engineering documentation (`.zenflow/tasks/reverse-engineer-and-design-new-cd6a/signalr/hubs-overview.md`), the original TallyJ system implemented **10 SignalR hubs**:

1. **MainHub** - General election status updates
2. **FrontDeskHub** - Real-time voter registration updates
3. **RollCallHub** - Roll call display synchronization
4. **PublicHub** - Public home page election list updates
5. **VoterPersonalHub** - Per-voter notifications
6. **AllVotersHub** - Broadcast to all voters
7. **VoterCodeHub** - Verification code status
8. **AnalyzeHub** - Tally progress updates
9. **BallotImportHub** - Ballot import progress
10. **ImportHub** - Voter import progress

**For Phase 5, we will implement a prioritized subset**:
- **Priority 1 (Must Have)**: MainHub, AnalyzeHub, BallotImportHub
- **Priority 2 (Should Have)**: FrontDeskHub, PublicHub
- **Priority 3 (Future)**: VoterPersonalHub, AllVotersHub, VoterCodeHub, RollCallHub, ImportHub

---

## 3. Architecture Design

### 3.1 SignalR Hub Pattern

**Modern ASP.NET Core SignalR Pattern** (single class):
```csharp
// Hub class inherits from Hub base class
public class MainHub : Hub
{
    private readonly IElectionService _electionService;
    private readonly ILogger<MainHub> _logger;

    public MainHub(IElectionService electionService, ILogger<MainHub> logger)
    {
        _electionService = electionService;
        _logger = logger;
    }

    // Client calls this to join election group
    public async Task JoinElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    // Server calls this to send updates to all clients in election
    public async Task SendElectionUpdate(Guid electionGuid, object update)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("ElectionUpdated", update);
    }

    private static string GetGroupName(Guid electionGuid) => $"Election_{electionGuid}";
}
```

**Note**: Original TallyJ used a dual-class wrapper pattern (wrapper + core) for SignalR 2.x. Modern ASP.NET Core SignalR uses a single Hub class with dependency injection.

### 3.2 Connection Groups

Hubs use **SignalR connection groups** to isolate messages per election:

| Group Pattern | Example | Purpose |
|---------------|---------|---------|
| `Election_{ElectionGuid}` | `Election_550e8400-e29b-41d4-a716-446655440000` | All clients viewing this election |
| `ElectionAdmin_{ElectionGuid}` | `ElectionAdmin_550e8400...` | Admin/teller users only |
| `Public` | `Public` | All unauthenticated users on home page |
| `TallyProgress_{ElectionGuid}` | `TallyProgress_550e8400...` | Clients watching tally calculation |

**Benefits**:
- Isolate elections from each other
- Different permissions for admins vs viewers
- Targeted notifications
- Efficient message routing

### 3.3 Hub-to-Hub Communication

For sending SignalR messages from API controllers or services:

```csharp
public class ElectionService : IElectionService
{
    private readonly IHubContext<MainHub> _hubContext;

    public ElectionService(IHubContext<MainHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task UpdateElectionAsync(Guid electionGuid, UpdateElectionDto dto)
    {
        // ... save to database ...

        // Broadcast update via SignalR
        var groupName = $"Election_{electionGuid}";
        await _hubContext.Clients.Group(groupName).SendAsync("ElectionUpdated", election);
    }
}
```

---

## 4. Implementation Scope

### 4.1 Priority 1: Core Real-time Features (Must Have)

#### Hub 1: MainHub - Election Status Updates
**Purpose**: Real-time election status changes (setup, voting, tallying, finalized)

**Backend**:
- Hub file: `backend/Hubs/MainHub.cs`
- Client methods: `JoinElection(electionGuid)`, `LeaveElection(electionGuid)`
- Server → Client events: `ElectionUpdated`, `ElectionStatusChanged`, `TallyStatusChanged`

**Frontend**:
- Service: `frontend/src/services/signalrService.ts` (SignalR connection manager)
- Composable: `frontend/src/composables/useSignalR.ts` (Vue composable for hooks)
- Integration: Update `electionStore.ts` to listen for `ElectionUpdated` events

**Use Cases**:
- Multiple tellers viewing same election see real-time status changes
- Election owner changes status from "Setup" → "Voting" → "Tallying" → "Finalized"
- All connected clients receive immediate update without refresh

#### Hub 2: AnalyzeHub - Tally Calculation Progress
**Purpose**: Live progress updates during long-running tally calculations

**Backend**:
- Hub file: `backend/Hubs/AnalyzeHub.cs`
- Client methods: `JoinTallySession(electionGuid)`
- Server → Client events: `TallyProgress`, `TallyComplete`, `TallyError`

**Frontend**:
- Integration: Update `resultStore.ts` to show progress bar
- Component: Add progress indicator to ResultsPage.vue

**Use Cases**:
- Teller clicks "Calculate Results" for election with 500 ballots
- Progress updates every 50 ballots: "Processing... 50/500", "Processing... 100/500", etc.
- Final message: "Tally complete! 500 ballots processed, 4500 votes counted."

#### Hub 3: BallotImportHub - Ballot Import Progress
**Purpose**: Progress updates during CSV ballot import

**Backend**:
- Hub file: `backend/Hubs/BallotImportHub.cs`
- Client methods: `JoinImportSession(electionGuid)`
- Server → Client events: `ImportProgress`, `ImportComplete`, `ImportError`

**Frontend**:
- Integration: Add import dialog with progress bar to BallotManagementPage.vue

**Use Cases**:
- Admin uploads CSV with 200 ballots
- Progress bar shows: "Importing... 50/200 ballots", "Validating votes..."
- Final summary: "Import complete! 200 ballots imported, 1800 votes added."

### 4.2 Priority 2: Enhanced Features (Should Have)

#### Hub 4: FrontDeskHub - Voter Management Sync
**Purpose**: Real-time sync when multiple tellers manage voters simultaneously

**Backend**:
- Hub file: `backend/Hubs/FrontDeskHub.cs`
- Server → Client events: `PersonAdded`, `PersonUpdated`, `PersonDeleted`

**Frontend**:
- Integration: Update `peopleStore.ts` to refresh list on events

**Use Cases**:
- Teller A adds new voter "John Smith"
- Teller B (on different computer) sees "John Smith" appear in list instantly
- No need to refresh page

#### Hub 5: PublicHub - Public Election List Updates
**Purpose**: Update election list on public home page when elections are listed/unlisted

**Backend**:
- Hub file: `backend/Hubs/PublicHub.cs`
- Server → Client events: `ElectionListUpdated`

**Frontend**:
- Create public dashboard view that listens for updates

**Use Cases**:
- Public viewer on home page sees list of available elections
- Admin unlists an election → election disappears from public list instantly

---

## 5. Source Code Structure

### 5.1 Backend Structure

**New Files**:
```
backend/
├── Hubs/
│   ├── MainHub.cs                  # Election status updates
│   ├── AnalyzeHub.cs               # Tally progress
│   ├── BallotImportHub.cs          # Import progress
│   ├── FrontDeskHub.cs             # Voter management sync (Priority 2)
│   └── PublicHub.cs                # Public list updates (Priority 2)
├── Services/
│   ├── ISignalRNotificationService.cs   # Interface for sending notifications
│   └── SignalRNotificationService.cs    # Implementation
└── DTOs/
    ├── ElectionUpdateDto.ts        # Election update payload
    ├── TallyProgressDto.ts         # Tally progress payload
    └── ImportProgressDto.ts        # Import progress payload
```

**Modified Files**:
```
backend/
├── Program.cs                      # Register SignalR services and hubs
├── Services/
│   ├── ElectionService.cs         # Add SignalR notifications
│   ├── BallotService.cs           # Add SignalR notifications
│   ├── PeopleService.cs           # Add SignalR notifications
│   └── TallyService.cs            # Add progress notifications
```

### 5.2 Frontend Structure

**New Files**:
```
frontend/src/
├── services/
│   └── signalrService.ts           # SignalR connection manager
├── composables/
│   └── useSignalR.ts               # Vue composable for SignalR hooks
└── types/
    ├── SignalREvents.ts            # TypeScript event definitions
    └── SignalRConnection.ts        # Connection state types
```

**Modified Files**:
```
frontend/src/
├── stores/
│   ├── electionStore.ts            # Listen for ElectionUpdated events
│   ├── ballotStore.ts              # Listen for BallotImportProgress events
│   ├── peopleStore.ts              # Listen for PersonAdded/Updated/Deleted events
│   └── resultStore.ts              # Listen for TallyProgress events
├── pages/
│   ├── elections/ElectionDetailPage.vue    # Show real-time status
│   ├── ballots/BallotManagementPage.vue    # Show import progress
│   ├── people/PeopleManagementPage.vue     # Real-time sync
│   └── results/ResultsPage.vue             # Show tally progress
└── components/
    └── common/ProgressNotification.vue     # Reusable progress component
```

---

## 6. Data Models & DTOs

### 6.1 Backend DTOs

**ElectionUpdateDto.cs**:
```csharp
public class ElectionUpdateDto
{
    public Guid ElectionGuid { get; set; }
    public string? Name { get; set; }
    public string? TallyStatus { get; set; }
    public string? ElectionStatus { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**TallyProgressDto.cs**:
```csharp
public class TallyProgressDto
{
    public Guid ElectionGuid { get; set; }
    public int TotalBallots { get; set; }
    public int ProcessedBallots { get; set; }
    public int TotalVotes { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public bool IsComplete { get; set; }
}
```

**ImportProgressDto.cs**:
```csharp
public class ImportProgressDto
{
    public Guid ElectionGuid { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public bool IsComplete { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### 6.2 Frontend Types

**SignalREvents.ts**:
```typescript
export interface ElectionUpdateEvent {
  electionGuid: string;
  name?: string;
  tallyStatus?: string;
  electionStatus?: string;
  updatedAt: string;
}

export interface TallyProgressEvent {
  electionGuid: string;
  totalBallots: number;
  processedBallots: number;
  totalVotes: number;
  message: string;
  percentComplete: number;
  isComplete: boolean;
}

export interface ImportProgressEvent {
  electionGuid: string;
  totalRows: number;
  processedRows: number;
  successCount: number;
  errorCount: number;
  currentStatus: string;
  percentComplete: number;
  isComplete: boolean;
  errors: string[];
}
```

**SignalRConnection.ts**:
```typescript
export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting'
}

export interface SignalRConfig {
  hubUrl: string;
  automaticReconnect?: boolean;
  accessTokenFactory?: () => string | null;
}
```

---

## 7. API Changes

### 7.1 SignalR Endpoints

SignalR hubs are automatically exposed at endpoints:

| Hub | Endpoint | Authentication |
|-----|----------|----------------|
| MainHub | `/hubs/main` | Required |
| AnalyzeHub | `/hubs/analyze` | Required |
| BallotImportHub | `/hubs/ballot-import` | Required |
| FrontDeskHub | `/hubs/front-desk` | Required |
| PublicHub | `/hubs/public` | Optional |

### 7.2 CORS Configuration

Update `Program.cs` CORS policy to allow SignalR:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required for SignalR
    });
});
```

### 7.3 Modified Service Methods

**ElectionService.UpdateElectionAsync**:
- After updating election in database, send `ElectionUpdated` event to all connected clients

**TallyService.CalculateTallyAsync**:
- Send `TallyProgress` events every N ballots (e.g., every 50 ballots)
- Send `TallyComplete` or `TallyError` event at end

**BallotService.ImportBallotsAsync**:
- Send `ImportProgress` events every N rows
- Send `ImportComplete` or `ImportError` event at end

---

## 8. Implementation Approach

### 8.1 Backend Implementation

**Step 1: Configure SignalR in Program.cs**
```csharp
// Add SignalR
builder.Services.AddSignalR();

// Register notification service
builder.Services.AddSingleton<ISignalRNotificationService, SignalRNotificationService>();

// Map SignalR hubs
app.MapHub<MainHub>("/hubs/main");
app.MapHub<AnalyzeHub>("/hubs/analyze");
app.MapHub<BallotImportHub>("/hubs/ballot-import");
```

**Step 2: Create Hubs**
- Implement each hub class inheriting from `Hub`
- Add `JoinElection` / `LeaveElection` methods for group management
- Add methods for sending updates to clients

**Step 3: Create SignalRNotificationService**
- Singleton service injected into domain services
- Provides helper methods for sending notifications
- Encapsulates hub context management

**Step 4: Integrate with Domain Services**
- Inject `ISignalRNotificationService` into ElectionService, TallyService, etc.
- Send notifications after database operations
- Add progress notifications in long-running operations

### 8.2 Frontend Implementation

**Step 1: Create SignalR Service**
```typescript
// signalrService.ts
import * as signalR from '@microsoft/signalr';

class SignalRService {
  private connections: Map<string, signalR.HubConnection> = new Map();

  async connect(hubUrl: string, accessToken?: string): Promise<signalR.HubConnection> {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => accessToken ?? '' })
      .withAutomaticReconnect()
      .build();

    await connection.start();
    this.connections.set(hubUrl, connection);
    return connection;
  }

  async disconnect(hubUrl: string): Promise<void> {
    const connection = this.connections.get(hubUrl);
    if (connection) {
      await connection.stop();
      this.connections.delete(hubUrl);
    }
  }

  getConnection(hubUrl: string): signalR.HubConnection | undefined {
    return this.connections.get(hubUrl);
  }
}

export const signalrService = new SignalRService();
```

**Step 2: Create Vue Composable**
```typescript
// useSignalR.ts
import { onUnmounted } from 'vue';
import { signalrService } from '@/services/signalrService';

export function useSignalR(hubUrl: string) {
  const connection = signalrService.getConnection(hubUrl);

  function on(eventName: string, callback: (...args: any[]) => void) {
    connection?.on(eventName, callback);
  }

  function off(eventName: string, callback: (...args: any[]) => void) {
    connection?.off(eventName, callback);
  }

  async function invoke(methodName: string, ...args: any[]) {
    return connection?.invoke(methodName, ...args);
  }

  onUnmounted(() => {
    // Cleanup listeners if needed
  });

  return { on, off, invoke };
}
```

**Step 3: Integrate with Stores**
- Connect to SignalR hubs in store initialization
- Listen for events and update store state
- Handle reconnection and error states

**Step 4: Update UI Components**
- Add progress indicators for tally and import operations
- Display real-time status updates
- Show reconnection banner if disconnected

---

## 9. Verification Approach

### 9.1 Backend Testing

**Unit Tests**:
```
TallyJ4.Tests/UnitTests/Hubs/
├── MainHubTests.cs                 # Test hub methods
├── AnalyzeHubTests.cs
└── SignalRNotificationServiceTests.cs
```

**Integration Tests**:
```
TallyJ4.Tests/IntegrationTests/
├── SignalRConnectionTests.cs       # Test client connections
└── SignalRNotificationTests.cs     # Test end-to-end notifications
```

**Test Commands**:
```bash
cd TallyJ4.Tests
dotnet test --filter "Category=SignalR"
```

### 9.2 Frontend Testing

**Manual Tests**:
1. Open two browser windows side-by-side
2. Login as admin in both
3. Navigate to same election
4. Update election in window 1 → verify window 2 updates instantly
5. Start tally calculation → verify progress bar updates in real-time
6. Import ballots via CSV → verify progress updates

**Connection Tests**:
1. Start application with SignalR connected
2. Stop backend server
3. Verify "Disconnected" banner appears
4. Restart backend server
5. Verify automatic reconnection

### 9.3 Performance Testing

**Load Test Scenarios**:
- 10 concurrent clients connected to same election
- 100 ballot import with progress updates
- 1000 ballot tally calculation with progress updates

**Metrics**:
- Connection establishment time < 500ms
- Event propagation latency < 100ms
- Memory usage per connection < 5MB
- Automatic reconnection within 10 seconds

---

## 10. Security Considerations

### 10.1 Authentication

**Hub Authentication**:
```csharp
[Authorize]  // Require JWT bearer token
public class MainHub : Hub
{
    // Only authenticated users can connect
}
```

**Public Hub** (optional authentication):
```csharp
public class PublicHub : Hub
{
    // No [Authorize] attribute - allows anonymous connections
}
```

### 10.2 Authorization

**Group Isolation**:
- Clients can only join groups for elections they have access to
- Validate election permissions in `JoinElection` method

```csharp
public async Task JoinElection(Guid electionGuid)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Verify user has access to this election
    var hasAccess = await _electionService.UserHasAccessAsync(electionGuid, userId);
    if (!hasAccess)
    {
        throw new HubException("Access denied to this election");
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(electionGuid));
}
```

### 10.3 Input Validation

**Client Input**:
- Validate all parameters in hub methods
- Sanitize string inputs
- Reject malformed GUIDs

**Rate Limiting**:
- Consider rate limiting on hub methods to prevent abuse
- Limit connection attempts per IP address

---

## 11. Error Handling & Resilience

### 11.1 Backend Error Handling

**Hub Exception Handling**:
```csharp
public async Task JoinElection(Guid electionGuid)
{
    try
    {
        // ... join logic ...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error joining election {ElectionGuid}", electionGuid);
        throw new HubException("Failed to join election");
    }
}
```

**Service Error Handling**:
- Wrap SignalR calls in try-catch
- Log errors but don't fail the main operation if notification fails
- Continue processing even if SignalR notification fails

### 11.2 Frontend Error Handling

**Connection Errors**:
```typescript
connection.onclose((error) => {
  console.error('SignalR connection closed:', error);
  showReconnectionBanner();
});

connection.onreconnecting((error) => {
  console.warn('SignalR reconnecting:', error);
  showReconnectingIndicator();
});

connection.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId);
  hideReconnectionBanner();
  // Rejoin groups
  rejoinElectionGroups();
});
```

**Event Handler Errors**:
```typescript
connection.on('ElectionUpdated', (data) => {
  try {
    electionStore.handleElectionUpdate(data);
  } catch (error) {
    console.error('Error handling ElectionUpdated event:', error);
    ElMessage.error('Failed to process election update');
  }
});
```

### 11.3 Automatic Reconnection

**Backend**:
- SignalR automatically handles reconnection attempts
- Configure reconnection policy in `Program.cs`

**Frontend**:
```typescript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/main')
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])  // Retry delays
  .build();
```

---

## 12. Performance Optimization

### 12.1 Connection Pooling

- SignalR manages connection pooling automatically
- Configure max concurrent connections if needed

### 12.2 Message Batching

For high-frequency updates (e.g., tally progress):
```csharp
// Batch progress updates - send every 50 ballots instead of every ballot
if (processedCount % 50 == 0)
{
    await SendProgressUpdate(processedCount, totalCount);
}
```

### 12.3 Group Management

**Best Practices**:
- Remove clients from groups when they navigate away
- Clean up stale groups periodically
- Use connection lifecycle events

```typescript
onBeforeUnmount(() => {
  // Leave election group when component unmounts
  connection?.invoke('LeaveElection', electionGuid);
});
```

---

## 13. Deployment Considerations

### 13.1 Azure SignalR Service (Optional)

For production scale-out, consider Azure SignalR Service:
- Handles 1000+ concurrent connections
- Automatic scaling
- Geographic distribution

**Configuration**:
```csharp
builder.Services.AddSignalR().AddAzureSignalR(options =>
{
    options.ConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
});
```

### 13.2 Sticky Sessions

If deploying to multiple servers without Azure SignalR:
- Enable sticky sessions (session affinity) on load balancer
- Ensure clients always connect to same server

### 13.3 WebSocket Configuration

**IIS Configuration**:
- Enable WebSocket Protocol feature
- Configure `web.config` for WebSocket support

**Firewall**:
- Ensure WebSocket ports are open (default: 80/443)
- Allow upgrade from HTTP to WebSocket protocol

---

## 14. Complexity Assessment

**Difficulty**: **Hard**

**Reasoning**:
1. **Multiple Integration Points**: 5 hubs × (backend + frontend) = 10 integration points
2. **State Synchronization**: Complex multi-client state management
3. **Error Scenarios**: Connection failures, reconnection, race conditions
4. **Testing Complexity**: Requires multi-client testing scenarios
5. **Authentication**: JWT token propagation to WebSocket connections
6. **Group Management**: Election isolation, permission-based groups
7. **Performance**: Real-time updates under load, message batching

**Risk Areas**:
- WebSocket connection failures in restrictive networks
- Race conditions with rapid updates
- Memory leaks if connections not cleaned up properly
- Authentication token expiration during long sessions

**Mitigation**:
- Comprehensive error handling and logging
- Automatic reconnection with exponential backoff
- Thorough manual testing with multiple clients
- Connection lifecycle management
- Token refresh mechanism integration

---

## 15. Success Criteria

**Functional Requirements**:
- ✅ Multiple users viewing same election see real-time status updates
- ✅ Tally calculation shows live progress bar
- ✅ Ballot import shows live progress updates
- ✅ Automatic reconnection after connection loss
- ✅ Proper authentication and authorization
- ✅ Election isolation (clients only see their elections)

**Non-Functional Requirements**:
- ✅ Event propagation latency < 500ms
- ✅ Reconnection within 30 seconds after disconnect
- ✅ Support 50+ concurrent connections per election
- ✅ No memory leaks after prolonged usage
- ✅ Graceful degradation if SignalR unavailable

**Testing Requirements**:
- ✅ Unit tests for all hubs (90% coverage)
- ✅ Integration tests for SignalR notifications
- ✅ Manual multi-client testing scenarios pass
- ✅ Load testing with 50+ concurrent clients
- ✅ Reconnection scenarios tested

---

## 16. Timeline Estimate

**Total Estimated Time**: 24-32 hours

**Breakdown**:
- Phase 5.1: Backend SignalR Setup (4 hours)
- Phase 5.2: MainHub Implementation (4 hours)
- Phase 5.3: AnalyzeHub + BallotImportHub (6 hours)
- Phase 5.4: Frontend SignalR Service (4 hours)
- Phase 5.5: Store Integration (4 hours)
- Phase 5.6: UI Components (4 hours)
- Phase 5.7: Testing & Bug Fixes (4-8 hours)
- Phase 5.8: FrontDeskHub + PublicHub (Optional, +6 hours)

**Dependencies**:
- Phase 4 must be complete (frontend application)
- Phase 2 API layer must be stable

**Blockers**:
- None identified (all dependencies already installed)

---

## 17. Future Enhancements (Post-Phase 5)

**Priority 3 Hubs**:
- **VoterPersonalHub**: Per-voter notifications for online voting
- **AllVotersHub**: Broadcast to all voters in election
- **VoterCodeHub**: Verification code status for 2FA
- **RollCallHub**: Roll call display synchronization
- **ImportHub**: Voter CSV import progress

**Advanced Features**:
- Presence detection (online/offline status)
- Typing indicators for collaborative editing
- Desktop notifications (browser notifications API)
- Mobile push notifications
- Audio/video streaming for announcements

**Monitoring & Analytics**:
- SignalR connection metrics dashboard
- Real-time connection count monitoring
- Event latency tracking
- Error rate monitoring
- User activity heat maps

---

## 18. References

**Documentation**:
- Original TallyJ SignalR: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/signalr/hubs-overview.md`
- Phase 4 Spec: `.zenflow/tasks/jan-20-6eb4/phase4-spec.md`
- ASP.NET Core SignalR Docs: https://learn.microsoft.com/en-us/aspnet/core/signalr/
- @microsoft/signalr Client: https://www.npmjs.com/package/@microsoft/signalr

**Codebase**:
- Backend: `backend/Program.cs`, `backend/TallyJ4.csproj`
- Frontend: `frontend/package.json`, `frontend/src/stores/`
- Tests: `TallyJ4.Tests/`

---

## 19. Questions & Clarifications

**Open Questions**:
1. Should we implement all 5 hubs in Phase 5, or prioritize MainHub, AnalyzeHub, BallotImportHub first?
   - **Recommendation**: Start with Priority 1 (3 hubs), add Priority 2 if time permits

2. Should SignalR notifications be best-effort (log errors) or critical (fail operation)?
   - **Recommendation**: Best-effort for most operations, critical only for tally completion

3. Should we use Azure SignalR Service or self-hosted SignalR?
   - **Recommendation**: Start with self-hosted, add Azure SignalR config for production scaling

4. Should we implement desktop/mobile push notifications in Phase 5?
   - **Recommendation**: No, defer to future enhancement phase

**Assumptions**:
- Frontend Phase 4 is complete and stable
- Backend API endpoints are complete
- JWT authentication is working correctly
- CORS is properly configured

---

## 20. Appendix: Example Code Snippets

### Backend: MainHub.cs
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

[Authorize]
public class MainHub : Hub
{
    private readonly ILogger<MainHub> _logger;

    public MainHub(ILogger<MainHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public async Task LeaveElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    private static string GetGroupName(Guid electionGuid) => $"Election_{electionGuid}";
}
```

### Frontend: signalrService.ts
```typescript
import * as signalR from '@microsoft/signalr';

class SignalRService {
  private mainConnection: signalR.HubConnection | null = null;

  async connectToMainHub(accessToken: string): Promise<void> {
    this.mainConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/main', {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    this.mainConnection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
    });

    this.mainConnection.onreconnected(() => {
      console.log('SignalR reconnected');
    });

    await this.mainConnection.start();
    console.log('Connected to MainHub');
  }

  async joinElection(electionGuid: string): Promise<void> {
    await this.mainConnection?.invoke('JoinElection', electionGuid);
  }

  onElectionUpdated(callback: (data: any) => void): void {
    this.mainConnection?.on('ElectionUpdated', callback);
  }

  async disconnect(): Promise<void> {
    await this.mainConnection?.stop();
    this.mainConnection = null;
  }
}

export const signalrService = new SignalRService();
```

### Frontend: electionStore.ts Integration
```typescript
import { defineStore } from 'pinia';
import { signalrService } from '@/services/signalrService';

export const useElectionStore = defineStore('election', () => {
  const elections = ref<Election[]>([]);

  async function initializeSignalR() {
    const authStore = useAuthStore();
    await signalrService.connectToMainHub(authStore.accessToken);

    signalrService.onElectionUpdated((data) => {
      handleElectionUpdate(data);
    });
  }

  function handleElectionUpdate(data: any) {
    const index = elections.value.findIndex(e => e.electionGuid === data.electionGuid);
    if (index !== -1) {
      elections.value[index] = { ...elections.value[index], ...data };
      ElMessage.success('Election updated in real-time');
    }
  }

  return { elections, initializeSignalR };
});
```
