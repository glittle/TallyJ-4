# ElectionsController API Documentation

## Overview
**Purpose**: Election CRUD operations, tally status management, cache control, and SignalR hub connections  
**Base Route**: `/Elections`  
**Authorization**: Mixed (`[AllowTellersInActiveElection]` and `[ForAuthenticatedTeller]`)  
**Authentication System**: Admin + Guest Teller Authentication

## Use Cases

This controller provides core election lifecycle operations:

1. **Election Selection**: Join into an election (set as current election)
2. **Election Creation**: Create new elections
3. **Tally Status**: Update election status (NotStarted → InProgress → Finalized)
4. **Export/Import**: Export election data, delete elections
5. **SignalR Connections**: Join hubs for real-time updates (ImportHub, AnalyzeHub)
6. **Cache Management**: Clear cached data when needed

---

## Endpoints

### 1. GET `/Elections/Index`
**Purpose**: Default route (not used)  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: `null`

**Business Logic**:
- No-op endpoint
- Returns null (likely redirects or 404)

**SignalR**: None

---

### 2. POST `/Elections/SelectElection`
**Purpose**: Select an election as the current active election  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (admin only, NOT guest tellers)  
**Returns**: JSON with election data or error

**Request Parameters**:
```csharp
{
  "guid": "guid",              // Election.ElectionGuid
  "oldComputerGuid": "guid?"   // Previous Computer.ComputerGuid (optional)
}
```

**Success Response** (JSON):
```json
{
  "Selected": true,
  "ElectionName": "string",
  "ElectionGuid": "guid",
  "CompGuid": "guid",
  "Locations": [
    {
      "Name": "string",
      "C_RowId": int
    }
  ]
}
```

**Error Response** (JSON):
```json
{
  "Selected": false
}
```

**Business Logic**:
1. Call `ElectionHelper.JoinIntoElection(guid, oldComputerGuid)`
2. If successful:
   - Sets `UserSession.CurrentElectionGuid`
   - Sets `UserSession.CurrentElection`
   - Creates or updates `Computer` record
   - Returns election details and locations
3. If failed:
   - Returns `Selected: false`

**Side Effects**:
- Updates `UserSession.CurrentElectionGuid`
- Updates `UserSession.CurrentElectionName`
- Updates `Computer` record in database
- Sets `UserSession.CurrentComputer`

**Use Case**:
- Admin selects election from election list
- System remembers which election is active
- Subsequent actions apply to this election

**Computer Record**:
- Each admin session has a `Computer` record
- Tracks current election and location
- `oldComputerGuid` used when switching elections (moves computer from old to new election)

**SignalR**: None

**Related Endpoint**: `/Dashboard/ElectionList` shows available elections

---

### 3. POST `/Elections/UpdateElectionStatus`
**Purpose**: Update election's tally status  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "state": "string"  // TallyStatus enum value
}
```

**Valid States**:
- `"NotStarted"`: Initial state, no tallying yet
- `"InProgress"`: Tallying in progress
- `"Finalized"`: Tallying complete, results locked
- `"Archived"`: Election archived (read-only)

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "NewStatus": "string"
}
```

**Business Logic**:
1. Call `ElectionHelper.SetTallyStatusJson(state)`
2. Validates state transition (e.g., can't go from Finalized back to NotStarted without admin override)
3. Updates `Election.TallyStatus`
4. Updates cache
5. **Broadcasts to SignalR**: All tellers see status change

**Side Effects**:
- Updates `Election` table
- Updates election cache
- Broadcasts via `MainHub.StatusChangedForElection()`
- May trigger UI changes (e.g., lock ballot entry when Finalized)

**Use Case**:
- After ballot entry complete: Set to "InProgress" to start tallying
- After tally complete: Set to "Finalized" to lock results
- Admin can reopen if needed

**SignalR**:
- `MainHub.StatusChangedForElection(electionGuid, info)`: Updates dashboard for all tellers

**Related Documentation**: `business-logic/tally-algorithms.md` - Tally process

---

### 4. POST `/Elections/CreateElection`
**Purpose**: Create new election  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: JSON with new election data or error

**Request**: No parameters (creates with defaults)

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "Election": {
    "ElectionGuid": "guid",
    "Name": "New Election",
    "ElectionType": "Normal",
    "NumberToElect": 9,
    "ElectionPasscode": "string"
  }
}
```

**Business Logic**:
1. Call `ElectionHelper.Create()`
2. Creates new `Election` record with defaults:
   - Name: "New Election" (user edits later)
   - Type: "Normal" (9-member LSA)
   - Status: "NotStarted"
   - Generates random access code for guest tellers
3. Creates default locations (Virtual, Physical Location 1)
4. Creates `JoinElectionUser` record (owner = current user)
5. **Automatically selects as current election**

**Default Election Settings**:
- **Name**: "New Election" (editable)
- **Type**: "Normal" (9-member election)
- **NumberToElect**: 9
- **Locations**: 2 (Virtual for online, Physical for in-person)
- **ElectionPasscode**: Random 6-character code
- **Owner**: Current logged-in user

**Side Effects**:
- Inserts `Election` record
- Inserts `Location` records (2 default)
- Inserts `JoinElectionUser` record (owner)
- Sets as `UserSession.CurrentElectionGuid`

**Use Case**:
- Admin clicks "Create New Election"
- System creates election with defaults
- Admin immediately taken to election setup wizard

**Next Steps After Creation**:
- User goes to Setup page (`/Setup/Index`)
- Completes 4-step setup wizard (documented in SetupController)

**SignalR**: None (new election, no other users yet)

---

### 5. POST `/Elections/JoinImportHub`
**Purpose**: Connect to ImportHub for voter import progress updates  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: JSON `true`

**Request Parameters**:
```csharp
{
  "connId": "string"  // SignalR connection ID
}
```

**Response** (JSON):
```json
true
```

**Business Logic**:
1. Call `ImportHub.Join(connId)`
2. Adds SignalR connection to election-specific import group
3. Connection will receive progress updates during voter CSV import

**SignalR**:
- Connection added to group: `"Import_{electionGuid}"`
- Receives progress updates every N records
- Receives completion/error messages

**Use Case**:
- User on Setup page about to import voters from CSV
- Client connects to SignalR first
- Client calls this endpoint to join hub
- User starts import
- Client receives progress: "100 of 500 voters imported..."

**Client Pattern**:
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/signalr/Import")
  .build();

await connection.start();
await axios.post('/Elections/JoinImportHub', { connId: connection.connectionId });

connection.on("ImportProgress", (progress) => {
  // Update progress bar: progress.current / progress.total
});

connection.on("ImportComplete", (result) => {
  // Show success message, refresh voter list
});
```

**Related Hub**: `signalr/hubs-overview.md` - ImportHub documentation

---

### 6. POST `/Elections/JoinAnalyzeHub`
**Purpose**: Connect to AnalyzeHub for tally progress updates  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: JSON `true`

**Request Parameters**:
```csharp
{
  "connId": "string"  // SignalR connection ID
}
```

**Response** (JSON):
```json
true
```

**Business Logic**:
1. Call `AnalyzeHub.Join(connId)`
2. Adds SignalR connection to election-specific analysis group
3. Connection will receive progress updates during tally/analysis

**SignalR**:
- Connection added to group: `"Analyze_{electionGuid}"`
- Receives progress updates every 10 ballots
- Receives completion message with results

**Use Case**:
- User on Monitor Progress page during tally
- Client connects to SignalR first
- Client calls this endpoint to join hub
- User starts tally
- Client receives progress: "50 of 200 ballots tallied..."
- Client receives results when complete

**Client Pattern**:
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/signalr/Analyze")
  .build();

await connection.start();
await axios.post('/Elections/JoinAnalyzeHub', { connId: connection.connectionId });

connection.on("AnalyzeProgress", (progress) => {
  // Update progress bar: progress.ballotsProcessed / progress.totalBallots
});

connection.on("AnalyzeComplete", (results) => {
  // Show results, navigate to results page
});
```

**Related Hub**: `signalr/hubs-overview.md` - AnalyzeHub documentation  
**Related Business Logic**: `business-logic/tally-algorithms.md` - Tally process

---

### 7. GET `/Elections/ExportElection`
**Purpose**: Export election data to file for backup or migration  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: File download (JSON or custom format)

**Request Parameters**:
```csharp
{
  "guid": "guid"  // Election.ElectionGuid (query string)
}
```

**Response**:
- Content-Type: application/json or application/octet-stream
- File download: `{ElectionName}_{date}.json`

**Business Logic**:
1. Call `ElectionExporter.Export(guid)`
2. Verifies user has access to election
3. Serializes all election data:
   - Election settings
   - Voters (Person records)
   - Locations
   - Tellers
   - Ballots
   - Votes
   - Results
   - Messages
4. Returns as downloadable file

**Export Format** (approximate):
```json
{
  "election": {
    "name": "string",
    "type": "string",
    "numberToElect": int,
    "created": "datetime"
  },
  "voters": [...],
  "ballots": [...],
  "votes": [...],
  "results": [...]
}
```

**Use Cases**:
- Backup election before major changes
- Archive election after completion
- Migrate election to another TallyJ instance
- Provide election data to auditors

**Security**:
- Only election owner or full tellers can export
- Export includes all sensitive data (voter emails, vote details)
- Should be stored securely

**Migration Note**: 
- Consider exporting to standard formats (CSV, Excel) for better interoperability
- May want separate endpoints for different export types (full data, results only, voter list)

---

### 8. GET `/Elections/ResetCache`
**Purpose**: Clear all cached data for current election  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: JSON confirmation

**Response** (JSON):
```json
"Cache cleared."
```

**Business Logic**:
1. Call `CacherHelper.DropAllCachesForThisElection()`
2. Clears all cached election data:
   - Election settings cache
   - Voter lists cache
   - Results cache
   - Location cache
   - Computer cache

**Side Effects**:
- **All tellers** see cache clear (cache is shared)
- Next data access will rebuild cache from database
- May cause brief performance impact as cache rebuilds

**Use Cases**:
- Troubleshooting: User sees stale data, admin clears cache
- After major data changes: Force all users to see fresh data
- Development/testing: Reset to known state

**When Cache is Normally Updated**:
- Automatically when data changes through normal endpoints
- Sometimes cache gets out of sync (rare edge cases)
- Manual reset ensures everyone sees correct data

**Performance Impact**:
- Cache rebuild happens on next data access
- Each teller rebuilds their own cache view
- Usually completes in < 1 second for typical elections

**SignalR**: None (but all users will fetch fresh data on next request)

---

### 9. GET `/Elections/DeleteElection`
**Purpose**: Delete election and all related data  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: Redirect or JSON confirmation

**Request Parameters**:
```csharp
{
  "guid": "guid"  // Election.ElectionGuid (query string)
}
```

**Response**:
- Redirect to `/Dashboard/ElectionList`
- Or JSON: `{ "Success": true }`

**Business Logic**:
1. Call `ElectionDeleter.Delete(guid)`
2. Verifies user is election OWNER (not just full teller)
3. Deletes all related data in order:
   - Votes
   - Ballots
   - Results, ResultSummaries, ResultTies
   - People (voters)
   - Locations
   - Tellers
   - Computers
   - JoinElectionUser records
   - Messages
   - Import files
   - Logs (C_Log, SmsLog)
   - OnlineVotingInfo records
   - Election record itself

**Cascade Delete Order**:
- Must delete child records first to avoid foreign key violations
- Votes → Ballots → Results → People → Election

**Side Effects**:
- **PERMANENT DELETION** - cannot be undone
- All guest tellers lose access immediately
- All full tellers lose access
- All cached data cleared

**Security**:
- Only election OWNER can delete (not full tellers)
- Verification happens in `ElectionDeleter` class
- Should prompt for confirmation in UI

**Use Cases**:
- Delete test elections
- Remove elections created by mistake
- Clean up after practice runs

**Warning**:
- No soft delete / archive option
- Consider adding confirmation: "Are you sure? Type election name to confirm"
- Consider adding "Archive" feature instead of delete

**SignalR**:
- Should disconnect all connected users from this election
- Should broadcast to MainHub that election no longer exists

---

## Business Logic Classes

### ElectionHelper
**Location**: `CoreModels/ElectionHelper.cs`

**Key Methods**:
- `JoinIntoElection(guid, oldComputerGuid)`: Select election as current
- `Create()`: Create new election with defaults
- `SetTallyStatusJson(state)`: Update tally status

**Responsibilities**:
- Election selection and switching
- Election creation with defaults
- Status management
- Computer record management

### ElectionExporter
**Location**: `CoreModels/ExportImport/ElectionExporter.cs`

**Key Methods**:
- `Export(guid)`: Serialize election to file

**Responsibilities**:
- Data serialization
- File generation
- Access control

### ElectionDeleter
**Location**: `CoreModels/ExportImport/ElectionDeleter.cs`

**Key Methods**:
- `Delete(guid)`: Delete election and all data

**Responsibilities**:
- Cascade deletion in correct order
- Owner verification
- Cache cleanup

### CacherHelper
**Location**: `EF/Partials/CacherHelper.cs`

**Key Methods**:
- `DropAllCachesForThisElection()`: Clear all cached data

**Responsibilities**:
- Cache management
- Cache invalidation
- Multi-teller cache coordination

---

## SignalR Hub Details

### ImportHub
**File**: `CoreModels/Hubs/ImportHub.cs`  
**Full Documentation**: `signalr/hubs-overview.md`

**Server → Client Methods**:
- `ImportProgress(current, total, message)`: Progress update during import
- `ImportComplete(result)`: Import finished successfully
- `ImportError(error)`: Import failed with error

**Use Cases**:
- CSV voter import progress
- Ballot import progress (if applicable)
- Long-running import operations

**Connection Groups**:
- Group per election: `"Import_{electionGuid}"`
- Only users importing data receive updates

### AnalyzeHub
**File**: `CoreModels/Hubs/AnalyzeHub.cs`  
**Full Documentation**: `signalr/hubs-overview.md`

**Server → Client Methods**:
- `AnalyzeProgress(ballotsProcessed, totalBallots, message)`: Tally progress
- `AnalyzeComplete(results)`: Tally finished
- `AnalyzeError(error)`: Tally failed

**Use Cases**:
- Tally progress updates (every 10 ballots)
- Result calculation progress
- Tie detection progress

**Connection Groups**:
- Group per election: `"Analyze_{electionGuid}"`
- All tellers monitoring progress receive updates

---

## Authorization

### Endpoint-Level Authorization

**`[AllowTellersInActiveElection]`**:
- Both admin and guest tellers allowed
- Must have active election selected
- Used for: Index (not used)

**`[ForAuthenticatedTeller]`**:
- ONLY admin tellers (NOT guest tellers)
- Must be logged in with username/password
- Used for: All actual endpoints (SelectElection, UpdateElectionStatus, CreateElection, JoinImportHub, JoinAnalyzeHub, ExportElection, ResetCache, DeleteElection)

**Why Admin Only?**:
- Guest tellers can't create, delete, or switch elections
- Guest tellers can't export data (security)
- Guest tellers can't clear cache (affects all users)
- Guest tellers work within one election only

---

## Session State Dependencies

**Required Session Variables**:
- `UserSession.CurrentElectionGuid`: Active election (set by SelectElection)
- `UserSession.CurrentElectionName`: Election name (set by SelectElection)
- `UserSession.CurrentElection`: Cached election object
- `UserSession.CurrentComputer`: Computer record
- `UserSession.UserGuid`: Admin user ID (for owner verification)
- `UserSession.IsKnownTeller`: Must be true (admin only)

---

## Migration Notes for .NET Core + Vue 3

### API Changes
**Current**: Mix of GET/POST, some return redirects, some JSON  
**Target**: RESTful API with consistent JSON responses

**Recommended Endpoints**:
```
GET    /api/elections                  → List elections
POST   /api/elections                  → Create election
GET    /api/elections/{id}             → Get election details
DELETE /api/elections/{id}             → Delete election
PATCH  /api/elections/{id}/status      → Update tally status
GET    /api/elections/{id}/export      → Export election
POST   /api/elections/{id}/select      → Select as current election
POST   /api/elections/{id}/cache/clear → Clear cache

POST   /api/signalr/import/join        → Join ImportHub
POST   /api/signalr/analyze/join       → Join AnalyzeHub
```

### Vue 3 Components

**Election Selector**:
```vue
<template>
  <div class="election-selector">
    <h2>Select Election</h2>
    <election-list-item 
      v-for="election in elections" 
      :key="election.guid"
      :election="election"
      @select="selectElection"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useElectionStore } from '@/stores/election';

const router = useRouter();
const electionStore = useElectionStore();
const elections = ref<Election[]>([]);

const selectElection = async (guid: string) => {
  const result = await electionStore.selectElection(guid);
  if (result.success) {
    router.push('/dashboard');
  }
};
</script>
```

**Tally Progress Monitor**:
```vue
<template>
  <div class="tally-progress">
    <h2>Tallying Ballots</h2>
    <el-progress :percentage="percentage" />
    <p>{{ progressMessage }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useSignalR } from '@/composables/useSignalR';

const ballotsProcessed = ref(0);
const totalBallots = ref(0);
const progressMessage = ref('');
const { connection, joinGroup } = useSignalR('Analyze');

const percentage = computed(() => 
  totalBallots.value > 0 
    ? Math.round((ballotsProcessed.value / totalBallots.value) * 100)
    : 0
);

onMounted(async () => {
  await joinGroup('Analyze');
  
  connection.on('AnalyzeProgress', (current: number, total: number, msg: string) => {
    ballotsProcessed.value = current;
    totalBallots.value = total;
    progressMessage.value = msg;
  });
  
  connection.on('AnalyzeComplete', (results: any) => {
    progressMessage.value = 'Tally complete!';
    // Navigate to results page
  });
});
</script>
```

### State Management (Pinia)
```typescript
// stores/election.ts
import { defineStore } from 'pinia';

export const useElectionStore = defineStore('election', {
  state: () => ({
    currentElection: null as Election | null,
    elections: [] as Election[]
  }),
  
  actions: {
    async selectElection(guid: string) {
      const response = await api.post(`/elections/${guid}/select`);
      if (response.data.Selected) {
        this.currentElection = {
          guid: response.data.ElectionGuid,
          name: response.data.ElectionName,
          // ...
        };
        return { success: true };
      }
      return { success: false };
    },
    
    async createElection() {
      const response = await api.post('/elections');
      if (response.data.Success) {
        this.elections.push(response.data.Election);
        await this.selectElection(response.data.Election.ElectionGuid);
        return response.data.Election;
      }
      throw new Error(response.data.Message);
    },
    
    async updateTallyStatus(status: string) {
      await api.patch(`/elections/${this.currentElection.guid}/status`, { status });
      if (this.currentElection) {
        this.currentElection.tallyStatus = status;
      }
    },
    
    async deleteElection(guid: string) {
      await api.delete(`/elections/${guid}`);
      this.elections = this.elections.filter(e => e.guid !== guid);
      if (this.currentElection?.guid === guid) {
        this.currentElection = null;
      }
    }
  }
});
```

### Authorization
```csharp
// .NET Core controller with policy-based authorization
[Authorize(Policy = "AuthenticatedTeller")]
[ApiController]
[Route("api/elections")]
public class ElectionsController : ControllerBase
{
  [HttpPost("{id}/select")]
  public async Task<IActionResult> SelectElection(Guid id, [FromBody] SelectElectionRequest request)
  {
    // Implementation
  }
  
  [HttpPost]
  public async Task<IActionResult> CreateElection()
  {
    // Implementation
  }
  
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteElection(Guid id)
  {
    // Verify ownership before deletion
    var isOwner = await _electionService.IsOwner(id, User.GetUserId());
    if (!isOwner)
      return Forbid();
    
    // Implementation
  }
}
```

---

## Testing Recommendations

### Unit Tests
```csharp
[Test]
public void CreateElection_SetsDefaults()
{
  // Arrange
  var helper = new ElectionHelper();
  
  // Act
  var result = helper.Create();
  
  // Assert
  Assert.That(result.Election.Name, Is.EqualTo("New Election"));
  Assert.That(result.Election.NumberToElect, Is.EqualTo(9));
  Assert.That(result.Election.ElectionPasscode, Has.Length.EqualTo(6));
}

[Test]
public void DeleteElection_NonOwner_Fails()
{
  // Arrange
  var deleter = new ElectionDeleter(electionGuid);
  UserSession.UserGuid = nonOwnerGuid;
  
  // Act & Assert
  Assert.Throws<UnauthorizedAccessException>(() => deleter.Delete());
}
```

### Integration Tests
```csharp
[Test]
public async Task SelectElection_SetsSessionAndComputer()
{
  // Arrange & Act
  var result = await PostAsync("/Elections/SelectElection", new { guid = electionGuid });
  
  // Assert
  Assert.That(result.Selected, Is.True);
  Assert.That(UserSession.CurrentElectionGuid, Is.EqualTo(electionGuid));
  Assert.That(UserSession.CurrentComputer, Is.Not.Null);
}

[Test]
public async Task JoinAnalyzeHub_ReceivesProgress()
{
  // Arrange
  var connection = ConnectToAnalyzeHub();
  var progressReceived = false;
  connection.On<int, int, string>("AnalyzeProgress", 
    (current, total, msg) => progressReceived = true);
  
  await PostAsync("/Elections/JoinAnalyzeHub", new { connId = connection.ConnectionId });
  
  // Act - Start tally in another thread
  await StartTally();
  await Task.Delay(500);
  
  // Assert
  Assert.That(progressReceived, Is.True);
}
```

---

## Related Documentation

- **Authentication**: `security/authentication.md` - Admin authentication
- **SignalR Hubs**: `signalr/hubs-overview.md` - ImportHub, AnalyzeHub details
- **Database**: `database/entities.md` - Election entity
- **Tally Logic**: `business-logic/tally-algorithms.md` - Tally process
- **Setup Wizard**: SetupController.md (to be documented) - Election setup after creation

---

## Summary

ElectionsController manages the election lifecycle:
- **Creation**: Create new elections with sensible defaults
- **Selection**: Choose which election to work on
- **Status Management**: Update tally status as election progresses
- **Export/Delete**: Backup and cleanup
- **Cache Control**: Troubleshooting and performance
- **SignalR Connections**: Join hubs for real-time updates

Key architectural features:
- **Admin-only access**: No guest teller access to these operations
- **Computer tracking**: Each session tracked for multi-election support
- **Real-time progress**: SignalR hubs for imports and tally
- **Owner verification**: Sensitive operations (delete) verify ownership
