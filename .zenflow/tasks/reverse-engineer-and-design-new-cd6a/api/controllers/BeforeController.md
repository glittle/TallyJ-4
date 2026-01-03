# BeforeController API Documentation

## Overview
**Purpose**: Front Desk voter registration and Roll Call public display functionality  
**Base Route**: `/Before`  
**Authorization**: `[AllowTellersInActiveElection]` (controller-level)  
**Authentication System**: Admin + Guest Teller Authentication

## Use Cases

This controller supports two critical real-time election workflows:

1. **Front Desk**: Tellers register voters as they arrive, marking them as present and their voting method (in-person, online, mail-in)
2. **Roll Call**: Public display (projector mode) showing all voters who have checked in or cast ballots

Both use **SignalR for real-time updates** - when one teller registers a voter, all other Front Desk screens and all Roll Call displays update instantly.

---

## Endpoints

### 1. GET `/Before/Index`
**Purpose**: Default route (not used)  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: `null`

**Business Logic**:
- No-op endpoint
- Returns null (likely redirects or 404)

**SignalR**: None

---

### 2. GET `/Before/FrontDesk`
**Purpose**: Display Front Desk voter registration page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View with `PeopleModel` data

**Business Logic**:
1. Check if current location is virtual
2. If virtual or null, switch to first physical location:
   - Call `LocationModel.GetLocations_Physical().First()`
   - Set `UserSession.CurrentLocationGuid` to physical location
3. Return view with `PeopleModel` containing all voters

**Why Physical Location Required**:
- Front Desk is for in-person registration
- Virtual locations are for online voting only
- Ensures teller is at an actual voting booth/hall

**View Data**:
- `PeopleModel`: Contains all voters in election with registration status

**SignalR**: None (page load only; client connects via `JoinFrontDeskHub`)

**Related UI Screenshot**: Front Desk screenshot shows voter list with checkboxes for voting methods

---

### 3. GET `/Before/RollCall`
**Purpose**: Display Roll Call public display page (projector mode)  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View with `RollCallModel` data

**Business Logic**:
- Return view with `RollCallModel`
- Model contains logic for displaying checked-in voters

**Use Case**:
- Typically displayed on projector for assembly to see who has voted
- Shows real-time updates as voters check in
- Used in Bahá'í elections for transparency

**View Data**:
- `RollCallModel`: Contains voters who have checked in or voted

**SignalR**: None (page load only; client connects via `JoinRollCallHub`)

**Related UI Screenshot**: Roll Call Display screenshot shows grid of voter names

---

### 4. POST `/Before/PeopleForFrontDesk`
**Purpose**: Get list of all voters with current registration status  
**HTTP Method**: POST (should be GET, but uses POST)  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON array of voter records

**Response** (JSON):
```json
[
  {
    "personGuid": "guid",
    "name": "string",
    "email": "string",
    "phone": "string",
    "canVote": "Yes|No",
    "votingMethod": "InPerson|Online|MailIn|null",
    "isInPerson": bool,
    "isOnline": bool,
    "isMailIn": bool,
    "registeredTime": "datetime",
    "locationName": "string",
    "flags": "string"
  }
]
```

**Business Logic**:
- Call `PeopleModel.FrontDeskPersonLines()`
- Returns all Person records for current election
- Includes computed fields for voting status
- Used for initial page load and manual refresh

**SignalR**: None (use FrontDeskHub for real-time updates)

**Performance**:
- Returns all voters (could be 50-500 records)
- Cached in PeopleModel
- SignalR sends deltas for real-time updates

---

### 5. POST `/Before/VotingMethod`
**Purpose**: Register voter's voting method (InPerson, Online, MailIn)  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON result with success/error

**Request Parameters**:
```csharp
{
  "id": int,              // Person.C_RowId
  "type": string,         // "InPerson", "Online", or "MailIn"
  "loc": int,             // LocationId (optional, default 0)
  "forceDeselect": bool   // If true, clears the voting method (optional, default false)
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "Person": {
    // Updated person record with new voting method
  }
}
```

**Business Logic**:
1. Call `PeopleModel.RegisterVotingMethod(id, type, forceDeselect, loc)`
2. Updates `Person` record:
   - Sets `VotingMethod` field
   - Records timestamp
   - Sets location if provided
3. Validates:
   - Person exists
   - Person is eligible to vote
   - Voting method is valid
4. **Broadcasts via SignalR** to all Front Desk and Roll Call clients

**Side Effects**:
- Updates `Person` table in database
- Broadcasts via `FrontDeskHub.UpdatePerson()`
- Broadcasts via `RollCallHub.UpdateRollCall()`

**SignalR Updates**:
- All Front Desk screens: Updated person row
- All Roll Call displays: Updated voter list

**Use Cases**:
- Voter arrives at Front Desk → Teller clicks "In Person"
- Voter calls to say they'll vote online → Teller clicks "Online"
- Voter mails in ballot → Teller clicks "Mail In"
- Undo/deselect → Teller clicks again with forceDeselect=true

---

### 6. POST `/Before/SetFlag`
**Purpose**: Set custom flags on voters (e.g., "Excused", "Ineligible")  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON result with success/error

**Request Parameters**:
```csharp
{
  "id": int,              // Person.C_RowId
  "type": string,         // Flag type (e.g., "Excused", "Ineligible")
  "loc": int,             // LocationId (optional, default 0)
  "forceDeselect": bool   // If true, clears the flag (optional, default false)
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "Person": {
    // Updated person record with new flag
  }
}
```

**Business Logic**:
1. Call `PeopleModel.SetFlag(id, type, forceDeselect, loc)`
2. Updates `Person` record with flag
3. **Broadcasts via SignalR** to all Front Desk clients

**Side Effects**:
- Updates `Person` table in database
- Broadcasts via `FrontDeskHub.UpdatePerson()`

**Use Cases**:
- Mark voter as excused from voting
- Mark voter as ineligible
- Custom election-specific flags

---

### 7. POST `/Before/JoinFrontDeskHub`
**Purpose**: Add SignalR connection to FrontDeskHub group  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: void

**Request Parameters**:
```csharp
{
  "connId": "string"  // SignalR connection ID
}
```

**Business Logic**:
1. Call `FrontDeskHub.Join(connId)`
2. Adds connection to election-specific group
3. Connection will receive real-time updates for this election

**SignalR**:
- Connection is added to group: `"FrontDesk_{electionGuid}"`
- Will receive updates when voters are registered
- Will receive updates when flags are set

**Client Pattern**:
```javascript
// Client connects to SignalR
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/signalr/FrontDesk")
  .build();

await connection.start();

// Client calls this endpoint with connection ID
await axios.post('/Before/JoinFrontDeskHub', { 
  connId: connection.connectionId 
});

// Client listens for updates
connection.on("UpdatePerson", (person) => {
  // Update UI with changed person data
});
```

**Notes**:
- Must be called after SignalR connection is established
- Connection ID is provided by SignalR client library
- See `signalr/hubs-overview.md` for FrontDeskHub details

---

### 8. POST `/Before/JoinRollCallHub`
**Purpose**: Add SignalR connection to RollCallHub group  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: void

**Request Parameters**:
```csharp
{
  "connId": "string"  // SignalR connection ID
}
```

**Business Logic**:
1. Call `RollCallHub.Join(connId)`
2. Adds connection to election-specific group
3. Connection will receive real-time updates for roll call display

**SignalR**:
- Connection is added to group: `"RollCall_{electionGuid}"`
- Will receive updates when voters check in
- Will receive updates when voting methods change

**Client Pattern**:
```javascript
// Client connects to SignalR
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/signalr/RollCall")
  .build();

await connection.start();

// Client calls this endpoint with connection ID
await axios.post('/Before/JoinRollCallHub', { 
  connId: connection.connectionId 
});

// Client listens for updates
connection.on("UpdateRollCall", (voters) => {
  // Update projector display with checked-in voters
});
```

**Use Case**:
- Projector display showing who has voted
- Updates in real-time as voters check in
- Typically used on large screen visible to assembly

**Notes**:
- Multiple Roll Call displays can be active simultaneously
- Each display joins the hub with its own connection
- See `signalr/hubs-overview.md` for RollCallHub details

---

## Business Logic Classes

### PeopleModel
**Location**: `CoreModels/PeopleModel.cs`

**Key Methods**:
- `FrontDeskPersonLines()`: Returns all voters with registration status
- `RegisterVotingMethod(id, type, forceDeselect, loc)`: Registers voting method
- `SetFlag(id, type, forceDeselect, loc)`: Sets custom flags

**Responsibilities**:
- Voter data access and manipulation
- Validation of voting method changes
- Broadcasting SignalR updates

### LocationModel
**Location**: `CoreModels/LocationModel.cs`

**Key Methods**:
- `GetLocations_Physical()`: Returns all physical (non-virtual) locations

**Responsibilities**:
- Location data access
- Filtering physical vs virtual locations

### RollCallModel
**Location**: `CoreModels/RollCallModel.cs`

**Key Methods**:
- Provides data for roll call display
- Filters voters who have checked in or voted

**Responsibilities**:
- Roll call display logic
- Voter filtering for public display

---

## SignalR Hub Details

### FrontDeskHub
**File**: `CoreModels/Hubs/FrontDeskHub.cs`  
**Full Documentation**: `signalr/hubs-overview.md`

**Server → Client Methods**:
- `UpdatePerson(person)`: Single person updated
- `RefreshPeople()`: Full refresh needed

**Use Cases**:
- Multi-teller coordination (all see same data)
- Real-time voter registration
- Immediate feedback when voter checks in

**Connection Groups**:
- Group per election: `"FrontDesk_{electionGuid}"`
- Only tellers in active election receive updates

### RollCallHub
**File**: `CoreModels/Hubs/RollCallHub.cs`  
**Full Documentation**: `signalr/hubs-overview.md`

**Server → Client Methods**:
- `UpdateRollCall(voters)`: Updated list of checked-in voters
- `RefreshRollCall()`: Full refresh needed

**Use Cases**:
- Public projector display
- Assembly transparency (see who has voted)
- Real-time updates as voters check in

**Connection Groups**:
- Group per election: `"RollCall_{electionGuid}"`
- Can have multiple displays (different rooms, backup projector)

---

## Authorization

**Controller-Level**: `[AllowTellersInActiveElection]`

**What This Means**:
- Admin tellers: Must be logged in (username/password)
- Guest tellers: Must have entered election access code
- Must have active election selected
- Prevents access before election setup or after election close

**Authorization Flow**:
1. Check if `UserSession.IsKnownTeller` (admin) OR `UserSession.IsGuestTeller`
2. Check if `UserSession.CurrentElectionGuid` is set and election is active
3. If either fails, redirect to login or election selection

**Related Documentation**: `security/authentication.md`

---

## Session State Dependencies

**Required Session Variables**:
- `UserSession.CurrentElectionGuid`: Active election
- `UserSession.CurrentLocationGuid`: Current location (physical for Front Desk)
- `UserSession.IsKnownTeller`: True if admin teller
- `UserSession.IsGuestTeller`: True if guest teller

**Location Logic**:
- Front Desk requires physical location
- If current location is virtual, automatically switches to first physical location
- Prevents registration at non-existent voting booths

---

## Migration Notes for .NET Core + Vue 3

### API Changes
**Current**: Mix of GET/POST returning Views or JSON  
**Target**: RESTful API with consistent JSON responses

**Recommended Endpoints**:
```
GET    /api/voters/front-desk        → List all voters with status
POST   /api/voters/{id}/voting-method → Register voting method
POST   /api/voters/{id}/flags         → Set flags
GET    /api/voters/roll-call          → Get checked-in voters
POST   /api/signalr/front-desk/join   → Join Front Desk hub
POST   /api/signalr/roll-call/join    → Join Roll Call hub
```

### SignalR Migration
- Use `@microsoft/signalr` client library
- Strongly-typed hubs in ASP.NET Core
- Consider consolidating FrontDeskHub + RollCallHub into single `VoterRegistrationHub`

### Vue 3 Components

**Front Desk Page**:
```vue
<template>
  <div class="front-desk">
    <h1>Front Desk - Voter Registration</h1>
    <voter-list 
      :voters="voters" 
      @update-voting-method="updateVotingMethod"
      @set-flag="setFlag" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useSignalR } from '@/composables/useSignalR';

const voters = ref<Voter[]>([]);
const { connection, joinGroup } = useSignalR('FrontDesk');

onMounted(async () => {
  voters.value = await fetchVoters();
  await joinGroup('FrontDesk');
  
  connection.on('UpdatePerson', (person: Voter) => {
    const index = voters.value.findIndex(v => v.id === person.id);
    if (index !== -1) voters.value[index] = person;
  });
});
</script>
```

**Roll Call Display**:
```vue
<template>
  <div class="roll-call-display">
    <h1>Roll Call - Who Has Voted</h1>
    <voter-grid :voters="checkedInVoters" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useSignalR } from '@/composables/useSignalR';

const checkedInVoters = ref<Voter[]>([]);
const { connection, joinGroup } = useSignalR('RollCall');

onMounted(async () => {
  checkedInVoters.value = await fetchRollCall();
  await joinGroup('RollCall');
  
  connection.on('UpdateRollCall', (voters: Voter[]) => {
    checkedInVoters.value = voters;
  });
});
</script>
```

### State Management (Pinia)
```typescript
// stores/voterRegistration.ts
import { defineStore } from 'pinia';

export const useVoterRegistrationStore = defineStore('voterRegistration', {
  state: () => ({
    voters: [] as Voter[],
    currentLocation: null as Location | null
  }),
  
  actions: {
    async registerVotingMethod(voterId: string, method: VotingMethod) {
      const response = await api.post(`/voters/${voterId}/voting-method`, { method });
      // Update local state (SignalR will also broadcast)
      this.updateVoter(response.data.person);
    },
    
    updateVoter(voter: Voter) {
      const index = this.voters.findIndex(v => v.id === voter.id);
      if (index !== -1) this.voters[index] = voter;
    }
  }
});
```

### Authorization Middleware
```csharp
// ASP.NET Core equivalent of [AllowTellersInActiveElection]
public class TellerInActiveElectionRequirement : IAuthorizationRequirement { }

public class TellerInActiveElectionHandler 
  : AuthorizationHandler<TellerInActiveElectionRequirement>
{
  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    TellerInActiveElectionRequirement requirement)
  {
    var user = context.User;
    var isKnownTeller = user.HasClaim("IsKnownTeller", "true");
    var isGuestTeller = user.HasClaim("IsGuestTeller", "true");
    var hasActiveElection = user.HasClaim(c => c.Type == "CurrentElectionGuid");
    
    if ((isKnownTeller || isGuestTeller) && hasActiveElection)
    {
      context.Succeed(requirement);
    }
    
    return Task.CompletedTask;
  }
}
```

---

## Testing Recommendations

### Unit Tests
```csharp
[Test]
public void FrontDesk_EnsuresPhysicalLocation()
{
  // Arrange
  var controller = new BeforeController();
  UserSession.CurrentLocationGuid = virtualLocationGuid;
  
  // Act
  controller.FrontDesk();
  
  // Assert
  Assert.That(UserSession.CurrentLocation.IsVirtual, Is.False);
}
```

### Integration Tests
```csharp
[Test]
public async Task RegisterVotingMethod_BroadcastsToSignalR()
{
  // Arrange
  var hubConnection = ConnectToFrontDeskHub();
  var personUpdated = false;
  hubConnection.On<Person>("UpdatePerson", p => personUpdated = true);
  
  // Act
  await PostAsync("/Before/VotingMethod", new { id = 1, type = "InPerson" });
  await Task.Delay(100); // Wait for SignalR broadcast
  
  // Assert
  Assert.That(personUpdated, Is.True);
}
```

### E2E Tests (Playwright + Vue 3)
```typescript
test('Front Desk real-time updates', async ({ page, browser }) => {
  // Open two Front Desk windows (simulating two tellers)
  const page1 = await browser.newPage();
  const page2 = await browser.newPage();
  
  await page1.goto('/front-desk');
  await page2.goto('/front-desk');
  
  // Teller 1 registers voter
  await page1.click('[data-voter-id="123"] .btn-in-person');
  
  // Verify Teller 2 sees update immediately
  await expect(page2.locator('[data-voter-id="123"]')).toHaveClass(/registered/);
});
```

---

## Related Documentation

- **Authentication**: `security/authentication.md` - Admin + Guest Teller auth
- **SignalR Hubs**: `signalr/hubs-overview.md` - FrontDeskHub, RollCallHub details
- **Database**: `database/entities.md` - Person, Location entities
- **UI Screenshots**: `ui-screenshots-analysis.md` - Front Desk and Roll Call screenshots

---

## Summary

BeforeController provides the critical pre-voting workflows:
- **Front Desk**: Real-time voter registration with multi-teller coordination
- **Roll Call**: Public display for transparency in assembly elections

Key architectural features:
- **Real-time updates** via SignalR (FrontDeskHub, RollCallHub)
- **Multi-teller support** (all tellers see same data instantly)
- **Location awareness** (physical locations for in-person voting)
- **Simple HTTP + SignalR** pattern for real-time collaboration

This pattern is used throughout TallyJ wherever multiple users need to collaborate on live election data.
