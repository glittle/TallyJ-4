# TallyJ SignalR Hubs Documentation

## Overview

TallyJ uses **SignalR 2.4.3** for real-time, bidirectional communication between server and clients. The system implements **10 SignalR hubs** organized by functional area, enabling live updates for election progress, voter status, ballot entry, and roll call displays.

---

## Architecture Pattern

### Dual-Class Hub Pattern

Each hub uses a **two-class pattern**:

1. **Wrapper Class** (e.g., `MainHub`): Business logic and server-to-client message sending
2. **Core Class** (e.g., `MainHubCore : Hub`): Empty class required by SignalR infrastructure

**Example**:
```csharp
// Wrapper class - contains business logic
public class MainHub
{
    private IHubContext _coreHub;

    private IHubContext CoreHub
    {
        get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<MainHubCore>()); }
    }

    public void Join(string connectionId)
    {
        var group = HubNameForCurrentElection + (UserSession.IsKnownTeller ? "Known" : "Guest");
        CoreHub.Groups.Add(connectionId, group);
    }

    public void StatusChanged(object infoForKnown, object infoForGuest)
    {
        CoreHub.Clients.Group(HubNameForCurrentElection + "Known").statusChanged(infoForKnown);
        CoreHub.Clients.Group(HubNameForCurrentElection + "Guest").statusChanged(infoForGuest);
    }
}

// Core class - empty, required by SignalR
public class MainHubCore : Hub
{
    // empty class needed for signalR use!!
    // referenced by helper and in JavaScript
}
```

### Connection Groups

Hubs use **SignalR connection groups** to target messages to specific clients:

| Group Pattern | Example | Purpose |
|---------------|---------|---------|
| `{HubName}{ElectionGuid}` | `Main550e8400-e29b-41d4-a716-446655440000` | All clients for an election |
| `{HubName}{ElectionGuid}Known` | `Main550e8400...Known` | Only authenticated admin tellers |
| `{HubName}{ElectionGuid}Guest` | `Main550e8400...Guest` | Only guest tellers (access code) |
| `{HubName}{VoterId}` | `Votervoter@example.com` | Specific voter by email/phone |
| `Public` | `Public` | All unauthenticated users on home page |

**Benefits**:
- Isolate elections from each other
- Different permissions for admins vs guests
- Per-voter notifications
- Broadcast to public users

---

## The 10 SignalR Hubs

### 1. MainHub
**Purpose**: General election status updates and teller coordination

**Location**: `CoreModels/Hubs/MainHub.cs`

**Connection Group**: `Main{ElectionGuid}Known` or `Main{ElectionGuid}Guest`

**Server → Client Methods**:
```javascript
// Client-side JavaScript listens for:
mainHub.client.statusChanged = function(info) {
    // Update election status, tally status, online voting status
    // info contains: StateName, Online, Passcode, ElectionGuid, Listed, etc.
};

mainHub.client.electionClosed = function() {
    // Election closed - guest tellers kicked out
    // Show message to reload or return home
};
```

**Server Methods**:
```csharp
public void Join(string connectionId)
// Join based on user type (Known/Guest teller)

public void JoinAll(string connectionId, string electionGuidList)
// Join multiple elections (for dashboard monitoring)

public void StatusChanged(object infoForKnown, object infoForGuest)
// Send different data to admins vs guests

public void StatusChangedForElection(Guid electionGuid, object infoForKnown, object infoForGuest)
// Update specific election (cross-election notification)

public void CloseOutGuestTellers()
// Kick out all guest tellers from current election

public void CloseOutGuestTellers(Guid electionGuid)
// Kick out guest tellers from specific election
```

**Use Cases**:
- Election status changes (NotStarted → NamesReady → Tallying → Finalized)
- Online voting opens/closes
- Teller access code changes
- Election listed/unlisted for public
- Guest teller lockout when election closed

---

### 2. FrontDeskHub
**Purpose**: Real-time voter registration and status updates at front desk

**Location**: `CoreModels/Hubs/FrontDeskHub.cs`

**Connection Group**: `FrontDesk{ElectionGuid}`

**Server → Client Methods**:
```javascript
frontDeskHub.client.updatePeople = function(message) {
    // Refresh voter list (another teller registered someone)
    // message contains: person details, voting method, location, etc.
};

frontDeskHub.client.reloadPage = function() {
    // Force page reload (major data change)
};

frontDeskHub.client.updateOnlineElection = function(message) {
    // Online voting status changed (opened/closed/countdown)
};
```

**Server Methods**:
```csharp
public void Join(string connectionId)
// Join front desk for current election

public void UpdatePeople(object message)
// Notify all front desk clients of voter updates

public void ReloadPage()
// Force all clients to reload (drastic updates)

public void UpdateOnlineElection(object message)
// Notify of online voting changes
```

**Use Cases**:
- Multiple tellers at different front desk stations
- Real-time voter check-in status
- Envelope number assignment
- Voting method changes (in-person, dropped off, mailed in, online)
- Online voting window updates

---

### 3. RollCallHub
**Purpose**: Real-time roll call display for projector/public viewing

**Location**: `CoreModels/Hubs/RollCallHub.cs`

**Connection Group**: `RollCall{ElectionGuid}`

**Server → Client Methods**:
```javascript
rollCallHub.client.updatePeople = function(message) {
    // Update displayed voter list as tellers advance through roll call
    // message contains: current voter, next voter, voting methods, etc.
};
```

**Server Methods**:
```csharp
public void Join(string connectionId)
// Join roll call display for current election

public void UpdateAllConnectedClients(object message)
// Update all roll call displays (projectors, monitors)
```

**Use Cases**:
- Public roll call display (projector shows voter names)
- Multiple displays in different rooms
- Synchronized navigation through voter list
- Real-time highlighting as tellers advance

---

### 4. PublicHub
**Purpose**: Updates for unauthenticated users on public home page

**Location**: `CoreModels/Hubs/PublicHub.cs`

**Connection Group**: `Public` (global, not per-election)

**Server → Client Methods**:
```javascript
publicHub.client.ElectionsListUpdated = function(html) {
    // Refresh list of available elections on home page
    // html contains: rendered HTML of election list
};
```

**Server Methods**:
```csharp
public void Join(string connectionId)
// Join public group (no authentication required)

public void TellPublicAboutVisibleElections()
// Broadcast updated election list to all public viewers
```

**Use Cases**:
- Home page shows elections open for tellers/voters
- Real-time updates when elections are listed/unlisted
- No authentication required
- Helps tellers/voters find their election

---

### 5. VoterPersonalHub
**Purpose**: Per-voter notifications (login events, registration changes)

**Location**: `CoreModels/Hubs/VoterPersonalHub.cs`

**Connection Group**: `Voter{VoterId}` (e.g., `Votervoter@example.com` or `Voter+15551234567`)

**Server → Client Methods**:
```javascript
voterPersonalHub.client.updateVoter = function(data) {
    // data.login = true: Another device logged in with same email/phone
    // data.updateRegistration = true: Teller changed registration (voting method, location)
    // data contains: VotingMethod, RegistrationTime, ElectionGuid
};
```

**Server Methods**:
```csharp
public void Join(string connectionId)
// Join voter's personal channel (by email/phone)

public void Update(Person person)
// Notify voter when registration changes (teller updated front desk)

public void Login(string voterId)
// Notify voter when they log in from another device/browser
```

**Use Cases**:
- Multi-device detection (logged in on phone and computer)
- Front desk updates (teller marks voter as "checked in")
- Security notification (someone else using your email/phone)

---

### 6. AllVotersHub
**Purpose**: Broadcast to all authenticated voters in an election

**Location**: `CoreModels/Hubs/AllVotersHub.cs`

**Connection Group**: `AllVoters{ElectionGuid}`

**Use Cases**:
- Announce online voting opening/closing
- System-wide voter messages
- Emergency notifications

---

### 7. VoterCodeHub
**Purpose**: Verification code status during voter authentication

**Location**: `CoreModels/Hubs/VoterCodeHub.cs`

**Connection Group**: Temporary hub key (session-based)

**Use Cases**:
- Real-time feedback during code entry
- "Code sent" confirmation
- "Code verified" or "Invalid code" feedback
- Multi-step authentication flow

---

### 8. AnalyzeHub
**Purpose**: Progress updates during ballot analysis/tallying

**Location**: `CoreModels/Hubs/AnalyzeHub.cs`

**Connection Group**: `Analyze{ElectionGuid}`

**Server → Client Methods**:
```javascript
analyzeHub.client.statusUpdate = function(message, showProgress) {
    // message: "Processing ballots", "Processed 150 ballots (1350 votes)"
    // showProgress: true/false to show/hide progress indicator
};
```

**Use Cases**:
- Long-running tally operations (thousands of ballots)
- Progress bar updates every 10 ballots
- "Analyzing...", "Saving...", "Complete" messages
- Keep users informed during 30-second to 5-minute operations

---

### 9. BallotImportHub
**Purpose**: Progress updates during ballot CSV import

**Location**: `CoreModels/Hubs/BallotImportHub.cs`

**Connection Group**: `BallotImport{ElectionGuid}`

**Use Cases**:
- Importing hundreds/thousands of ballots from external systems
- Row-by-row processing progress
- Validation errors and warnings
- Import summary statistics

---

### 10. ImportHub
**Purpose**: Progress updates during voter CSV import

**Location**: `CoreModels/Hubs/ImportHub.cs`

**Connection Group**: `Import{ElectionGuid}`

**Use Cases**:
- Importing voter lists (10,000+ records possible)
- Column mapping validation
- Duplicate detection
- Import summary (X added, Y updated, Z errors)

---

## Client-Side Connection Pattern

### JavaScript Connection Example
```javascript
// 1. Create proxy to hub
var mainHub = $.connection.mainHubCore;

// 2. Define client-side methods (server calls these)
mainHub.client.statusChanged = function(info) {
    console.log('Status changed:', info);
    // Update UI based on info
};

mainHub.client.electionClosed = function() {
    alert('Election closed. Please reload.');
    location.reload();
};

// 3. Start connection
$.connection.hub.start().done(function() {
    console.log('Connected to SignalR');
    var connectionId = $.connection.hub.id;
    
    // 4. Join hub group via server call
    site.callAjaxHandler('/Public/JoinMainHub', { 
        connId: connectionId,
        electionGuid: site.electionGuid
    });
});

// 5. Handle disconnection
$.connection.hub.disconnected(function() {
    console.log('Disconnected from SignalR');
    // Show reconnection banner (like screenshot)
    setTimeout(function() {
        $.connection.hub.start(); // Auto-reconnect
    }, 5000);
});
```

### Reconnection Handling
**From screenshot** (`ca8c5e4b-6a25-4741-8663-dc2ddcfe0d07.png`):

When SignalR connection is lost:
1. Client detects disconnection
2. Red banner appears: "We've been disconnected from the server for too long. Please reload/refresh this page to reconnect and continue."
3. User can dismiss or reload page
4. JavaScript may auto-reconnect after timeout

**Code Pattern** (typical):
```javascript
$.connection.hub.disconnected(function() {
    if (disconnectTimeExceeded) {
        showReconnectionBanner(); // Red banner from screenshot
    } else {
        setTimeout(function() {
            $.connection.hub.start(); // Auto-reconnect
        }, 5000);
    }
});
```

---

## Hub Authorization

### No Built-In Authorization Attributes
Unlike controllers, SignalR hubs in this system **do NOT use `[Authorize]` attributes**.

**Authorization Strategy**:
1. **Connection groups** isolate data
2. **Server-side checks** before joining groups
3. **Session-based** validation

**Example** (from `PublicController.cs:155-171`):
```csharp
public void JoinMainHub(string connId, string electionGuid)
{
    // Manual authorization check
    if (UserSession.CurrentElectionGuid == Guid.Empty)
    {
        return; // Silently reject
    }

    var guid = electionGuid.AsGuid();
    if (guid != UserSession.CurrentElectionGuid)
    {
        return; // Silently reject
    }

    // Only if authorized, join the group
    new MainHub().Join(connId);
}
```

**Security Model**:
- Unauthenticated users: Can only join `PublicHub`
- Voters: Can join `VoterPersonalHub`, `AllVotersHub`
- Tellers (guest): Can join `MainHub`, `FrontDeskHub`, `RollCallHub`, etc.
- Tellers (admin): Same as guest tellers + `AnalyzeHub`, `ImportHub`, etc.

---

## Message Patterns

### Server-Initiated Broadcasts
**Pattern**: Server code calls hub method to broadcast to all clients

```csharp
// Example: After saving election status change
var election = ...; // Updated election
var info = new
{
    ElectionGuid = election.ElectionGuid,
    StateName = election.TallyStatus,
    Online = election.OnlineCurrentlyOpen,
    Passcode = election.ElectionPasscode,
    Listed = election.ListedForPublicAsOf != null
};

new MainHub().StatusChanged(info, info);
// Broadcasts to both Known and Guest groups
```

### Client-Initiated Server Calls
**Pattern**: Client JavaScript calls server hub methods (not used much in TallyJ)

```javascript
// Example: Call server method from client (rare in TallyJ)
mainHub.server.someMethod(param1, param2).done(function(result) {
    console.log('Server returned:', result);
});
```

**Note**: TallyJ primarily uses **server → client** messages. Client-initiated calls go through **AJAX to controllers** instead of hub methods.

---

## Performance Considerations

### Connection Scaling
- Each browser tab = 1 connection
- Election with 50 concurrent tellers = ~50-100 connections (multiple tabs)
- Large election (1000+ online voters) = 1000+ connections

### Message Frequency
- **High frequency**: `AnalyzeHub` (every 10 ballots during 10,000-ballot tally)
- **Medium frequency**: `FrontDeskHub` (every voter check-in)
- **Low frequency**: `MainHub` (status changes), `PublicHub` (election list updates)

### Optimization Strategies
1. **Connection groups** reduce broadcast scope
2. **Selective updates** (Known vs Guest)
3. **Batch updates** (analyze every 10 ballots, not every 1)
4. **Cached data** (send HTML instead of raw data for `PublicHub`)

---

## Migration to .NET Core + Vue 3

### SignalR Core Changes

**Technology Migration**:
- SignalR 2.4.3 (.NET Framework) → SignalR Core (ASP.NET Core)
- Global.asax `RouteTable.Routes.MapHubs()` → `app.MapHub<>()` in Startup
- `GlobalHost.ConnectionManager.GetHubContext<>()` → Dependency injection

**Strongly-Typed Hubs** (Recommended):
```csharp
// Define client interface
public interface IMainClient
{
    Task StatusChanged(ElectionStatusDto info);
    Task ElectionClosed();
}

// Inherit Hub<IMainClient>
public class MainHub : Hub<IMainClient>
{
    public async Task JoinElection(Guid electionGuid)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Main{electionGuid}");
    }

    public async Task BroadcastStatusChange(Guid electionGuid, ElectionStatusDto info)
    {
        await Clients.Group($"Main{electionGuid}").StatusChanged(info);
    }
}
```

**Benefits**:
- Compile-time safety
- IntelliSense support
- Refactoring support
- TypeScript generation

### Vue 3 Integration

**Install SignalR Client**:
```bash
npm install @microsoft/signalr
```

**Service Pattern** (Vue 3 + TypeScript):
```typescript
// services/signalRService.ts
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { useElectionStore } from '@/stores/election';

export class MainHubService {
    private connection: HubConnection | null = null;

    async connect(electionGuid: string) {
        this.connection = new HubConnectionBuilder()
            .withUrl(`${import.meta.env.VITE_API_URL}/hubs/main`)
            .withAutomaticReconnect()
            .build();

        // Register client methods
        this.connection.on('StatusChanged', (info: ElectionStatusDto) => {
            const store = useElectionStore();
            store.updateElectionStatus(info);
        });

        this.connection.on('ElectionClosed', () => {
            alert('Election closed');
            window.location.href = '/';
        });

        await this.connection.start();
        await this.connection.invoke('JoinElection', electionGuid);
    }

    async disconnect() {
        await this.connection?.stop();
    }
}
```

**Composable Pattern** (Vue 3 Composition API):
```typescript
// composables/useMainHub.ts
import { ref, onMounted, onUnmounted } from 'vue';
import { MainHubService } from '@/services/signalRService';

export function useMainHub(electionGuid: string) {
    const isConnected = ref(false);
    const hubService = new MainHubService();

    onMounted(async () => {
        await hubService.connect(electionGuid);
        isConnected.value = true;
    });

    onUnmounted(async () => {
        await hubService.disconnect();
        isConnected.value = false;
    });

    return {
        isConnected
    };
}
```

**Component Usage**:
```vue
<script setup lang="ts">
import { useMainHub } from '@/composables/useMainHub';
import { useElectionStore } from '@/stores/election';

const electionStore = useElectionStore();
const { isConnected } = useMainHub(electionStore.currentElectionGuid);
</script>

<template>
    <div>
        <div v-if="!isConnected" class="connection-warning">
            Connecting to real-time updates...
        </div>
        <!-- Component content -->
    </div>
</template>
```

---

## Hub Consolidation Strategy

**Current**: 10 separate hubs
**Recommended**: Consolidate to **5 hubs**

| Old Hubs | New Hub | Rationale |
|----------|---------|-----------|
| MainHub | ElectionHub | General election updates |
| FrontDeskHub, RollCallHub | FrontDeskHub | Both for voter registration phase |
| AllVotersHub, VoterPersonalHub, VoterCodeHub | VoterHub | All voter-related |
| AnalyzeHub, BallotImportHub, ImportHub | ProgressHub | All progress updates |
| PublicHub | PublicHub | Keep separate (unauthenticated) |

**Benefits**:
- Fewer connections to manage
- Simpler client code
- Easier to maintain
- Still isolated by connection groups

---

## Testing Strategy

### Unit Testing Hubs
```csharp
[Fact]
public async Task MainHub_StatusChanged_BroadcastsToKnownGroup()
{
    // Arrange
    var mockClients = new Mock<IHubClients<IMainClient>>();
    var mockGroup = new Mock<IMainClient>();
    mockClients.Setup(c => c.Group("Main{guid}Known")).Returns(mockGroup.Object);

    var hub = new MainHub { Clients = mockClients.Object };

    // Act
    await hub.BroadcastStatusChange(guid, statusDto);

    // Assert
    mockGroup.Verify(m => m.StatusChanged(statusDto), Times.Once);
}
```

### Integration Testing
- Use SignalR test client
- Verify connection groups
- Test reconnection logic
- Load test with many concurrent connections

---

**End of SignalR Hubs Documentation**
