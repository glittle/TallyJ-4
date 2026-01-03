# DashboardController API Documentation

## Overview
**Purpose**: Election dashboard, election list management, teller management, and location selection  
**Base Route**: `/Dashboard`  
**Authorization**: Mixed (`[AllowTellersInActiveElection]` and `[ForAuthenticatedTeller]`)  
**Authentication System**: Admin + Guest Teller Authentication

## Use Cases

This controller provides the main dashboard and election management interface:

1. **Dashboard**: Main landing page after login showing current election status
2. **Election List**: View all elections user has access to
3. **Teller Management**: Add/remove tellers (both guest and full tellers)
4. **Location Management**: Choose which physical location/computer is being used
5. **Election Visibility**: Control whether election is listed publicly for guest tellers
6. **Election Import**: Import V2 elections (legacy feature)

---

## Endpoints

### 1. GET `/Dashboard/Index`
**Purpose**: Main dashboard landing page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View or Redirect

**Business Logic**:
1. Check if `UserSession.CurrentElectionGuid` is empty or `UserSession.CurrentElection` is null
2. If no current election:
   - If `IsKnownTeller`: Redirect to `/Dashboard/ElectionList`
   - If guest teller: Redirect to `/Account/LogOff`
3. If current election exists:
   - Return view with `ElectionsListViewModel`

**Response**:
- **No election**: HTTP 302 redirect
- **Has election**: View with election data

**View Data**:
- `ElectionsListViewModel`: Contains current election details, status, statistics

**Use Case**:
- First page after login
- Shows current election name, status, quick actions
- Links to Front Desk, Ballot Entry, Results, etc.

**SignalR**: None

**Related UI Screenshot**: Dashboard screenshot shows election name, status, and action buttons

---

### 2. GET `/Dashboard/ElectionList`
**Purpose**: Display list of all elections user can access  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (admins only, NOT guest tellers)  
**Returns**: View with `ElectionsListViewModel`

**Business Logic**:
1. If `CurrentElectionGuid` is empty:
   - Get or create temporary computer record:
     - `ComputerModel.GetTempComputerForMe()` OR
     - `ComputerCacher.UpdateComputer(currentComputer)`
2. Call `PublicHub.TellPublicAboutVisibleElections()` to broadcast current elections
3. Return view with `ElectionsListViewModel`

**View Data**:
- `ElectionsListViewModel`: List of all elections user owns or has access to
- Includes election status, passcode, online voting status, teller count

**Computer Record**:
- Each logged-in admin has a "Computer" record tracking their current election/location
- Used for multi-election support (admin can switch between elections)
- Temporary record created if none exists

**Use Case**:
- Admin views all their elections
- Can select an election to work on
- Can create new election
- Can see which elections are listed publicly

**SignalR**:
- Broadcasts to `PublicHub` with updated list of visible elections
- Public page shows elections available for guest tellers to join

**Related UI Screenshot**: Election List page (not in screenshots, but referenced in navigation)

---

### 3. POST `/Dashboard/MoreInfoStatic`
**Purpose**: Get static election information that doesn't change often  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with election configuration

**Response** (JSON):
```json
{
  "ElectionGuid": "guid",
  "ElectionName": "string",
  "ElectionType": "string",
  "NumberToElect": int,
  "OwnerName": "string",
  "Created": "datetime",
  "TotalVoters": int,
  "TotalLocations": int
}
```

**Business Logic**:
- Call `ElectionsListViewModel.MoreInfoStatic()`
- Returns election configuration that rarely changes
- Used for dashboard display

**Use Case**:
- Initial page load gets static data
- MoreInfoLive() endpoint gets dynamic data
- Reduces payload size for frequent updates

**SignalR**: None

---

### 4. POST `/Dashboard/MoreInfoLive`
**Purpose**: Get live election information that changes frequently  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with current election status

**Response** (JSON):
```json
{
  "TallyStatus": "string",
  "OnlineCurrentlyOpen": bool,
  "BallotsReceived": int,
  "VotesEntered": int,
  "VotersRegistered": int,
  "LastActivity": "datetime",
  "ActiveTellers": int
}
```

**Business Logic**:
- Call `ElectionsListViewModel.MoreInfoLive()`
- Returns frequently-changing election statistics
- Can be polled every few seconds for live updates

**Use Case**:
- Dashboard shows real-time ballot count
- Dashboard shows how many tellers are active
- Dashboard shows online voting status

**SignalR**: 
- This data is also broadcast via `MainHub.StatusChangedForElection()`
- Endpoint exists for initial load or fallback

---

### 5. POST `/Dashboard/ReloadElections`
**Purpose**: Refresh election list with latest data  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with updated election list

**Response** (JSON):
```json
{
  "Success": true,
  "elections": [
    {
      "ElectionGuid": "guid",
      "ElectionName": "string",
      "TallyStatus": "string",
      "OnlineCurrentlyOpen": bool,
      "ElectionPasscode": "string",
      "ListedForPublicAsOf": "datetime?",
      "TellerCount": int
    }
  ]
}
```

**Business Logic**:
1. Call `ElectionsListViewModel.GetMyElectionsInfo(true)` with force refresh
2. Returns fresh data from database (bypasses cache)

**Use Case**:
- User clicks "Refresh" button
- Verify changes made in another browser/device
- After adding/removing tellers

**SignalR**: None (user-initiated refresh)

---

### 6. POST `/Dashboard/UpdateListingForElection`
**Purpose**: Toggle whether election is publicly listed for guest tellers to join  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "listOnPage": bool,        // true = list publicly, false = hide
  "electionGuid": "guid"     // Election to update
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "IsOpen": bool,
  "Message": "string"  // only if error
}
```

**Business Logic**:
1. Verify user has access to election (must be in user's election list)
2. If not found: Return error "Unknown election"
3. If user is not known teller: Return error "Success: false"
4. Update election:
   - Set `Election.ListForPublic = listOnPage`
   - Set `Election.ListedForPublicAsOf = DateTime.UtcNow` (if listing) or null (if hiding)
5. Update cache: `ElectionCacher.UpdateItemAndSaveCache(election)`
6. Broadcast to public homepage: `PublicHub.TellPublicAboutVisibleElections()`
7. If hiding election: Close out all guest tellers: `MainHub.CloseOutGuestTellers(electionGuid)`
8. Broadcast status change: `MainHub.StatusChangedForElection(electionGuid, info, info)`

**Side Effects**:
- Updates `Election` table
- Updates election cache
- Broadcasts to `PublicHub` (updates public home page)
- Broadcasts to `MainHub` (updates dashboard for all tellers)
- If hiding: Logs out all guest tellers from this election

**Use Case**:
- Admin wants to open election for guest tellers to join (before meeting starts)
- Admin wants to hide election (after meeting ends or if too many tellers joined)

**Security**:
- Only election owner can list/unlist
- Guest tellers lose access immediately when election is unlisted

**SignalR**:
- `PublicHub.TellPublicAboutVisibleElections()`: Updates public page
- `MainHub.CloseOutGuestTellers(electionGuid)`: Kicks guest tellers
- `MainHub.StatusChangedForElection(electionGuid, info)`: Updates dashboard

---

### 7. POST `/Dashboard/LoadV2Election`
**Purpose**: Import election from TallyJ V2 (legacy feature)  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with success/error

**Request**:
- Content-Type: multipart/form-data
- File upload: V2 election export file

**Request Parameters**:
```csharp
{
  "loadFile": HttpPostedFileBase  // V2 election file
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "ElectionGuid": "guid"  // only if success
}
```

**Business Logic**:
1. Call `ElectionLoader.Import(loadFile)`
2. Parse V2 election file format
3. Create new V3 election with all data
4. Import voters, ballots, votes, results

**Use Case**:
- Migrate old elections from TallyJ V2 to V3
- Import historical data
- Legacy feature (may not be needed in V4)

**Error Handling**:
- Invalid file format
- Corrupted data
- Database errors

**SignalR**: None

**Migration Note**: 
- This feature may not be needed in .NET Core version
- V2 elections are years old now
- Consider removing or replacing with direct database migration

---

### 8. POST `/Dashboard/ChooseLocation`
**Purpose**: Set which physical location this computer is at  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with success status

**Request Parameters**:
```csharp
{
  "id": int  // Location.C_RowId
}
```

**Response** (JSON):
```json
{
  "Selected": bool
}
```

**Business Logic**:
1. Call `ComputerModel.MoveCurrentComputerIntoLocation(id)`
2. Updates `Computer` record with new location
3. Sets `UserSession.CurrentLocationGuid`

**Use Case**:
- Teller moves from one voting booth to another
- Multi-location elections (e.g., multiple rooms)
- Front Desk requires physical location

**Location Concept**:
- **Physical locations**: Voting booths, registration desks
- **Virtual location**: For online voting (no physical presence)
- Each computer can only be at one location at a time

**SignalR**: None

---

### 9. POST `/Dashboard/ChooseTeller`
**Purpose**: Assign guest teller (temporary teller for this election only)  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "num": int,           // Teller number (1-10)
  "teller": int,        // 0 for new teller, or existing Teller.C_RowId
  "newName": "string"   // Name for new teller (if teller == 0)
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "Teller": {
    "C_RowId": int,
    "Name": "string",
    "TellerNum": int
  }
}
```

**Business Logic**:
1. Call `TellerModel.ChooseTeller(num, teller, newName)`
2. If `teller == 0`: Create new guest teller with `newName`
3. If `teller > 0`: Assign existing teller to this number
4. Guest tellers are election-specific (not global admin accounts)

**Guest Teller Concept**:
- **Admin tellers**: Have accounts, can access multiple elections
- **Guest tellers**: Temporary helpers, no account needed
- **Authentication**: Election access code only (no password)
- **Permissions**: Limited to current election only

**Use Case**:
- Election organizer creates teller slots: "Teller 1", "Teller 2", etc.
- Guest arrives, enters access code, selects "Teller 1" from list
- Guest can now enter ballots, register voters, etc.
- After election, guest teller record can be deleted

**SignalR**: 
- May broadcast to `MainHub` to update teller list

**Related Documentation**: `security/authentication.md` - Guest Teller Authentication

---

### 10. POST `/Dashboard/DeleteTeller`
**Purpose**: Remove guest teller from election  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "id": int  // Teller.C_RowId
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string"
}
```

**Business Logic**:
1. Call `TellerModel.DeleteTeller(id)`
2. Deletes `Teller` record from database
3. Does NOT delete admin accounts (only guest tellers)

**Use Case**:
- Remove guest teller after election
- Clean up teller list
- Remove teller who left early

**Security**:
- Cannot delete admin tellers (only guest tellers)
- Guest teller loses access immediately

**SignalR**: 
- May need to disconnect guest teller if currently logged in

---

### 11. POST `/Dashboard/RemoveFullTeller`
**Purpose**: Remove full teller (admin) from election access  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (owner only)  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "email": "string",  // Teller's email
  "joinId": int       // JoinElectionUser.C_RowId
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string"
}
```

**Business Logic**:
1. Find `JoinElectionUser` record by `joinId`
2. Verify current user is election OWNER:
   - Find user's join record for this election
   - Check that `Role` is null (null = owner, "Full" = invited teller)
   - If not owner: Return error "Removal not allowed"
3. Delete `JoinElectionUser` record
4. Log event: "Removed full teller - {email}"
5. TODO: Sign out this user if currently logged in

**Full Teller Concept**:
- **Owner**: Created the election, has full control
- **Full Teller**: Admin account invited by owner, can access this election
- **Invitation**: Owner invites by email, teller accepts on login

**Use Case**:
- Owner removes co-organizer who no longer needs access
- Revoke access for teller who left organization
- Clean up after election

**Security**:
- Only owner can remove full tellers
- Removed teller immediately loses access to this election
- Removed teller still has their admin account (can access other elections)

**SignalR**: 
- TODO: Should disconnect removed teller if currently active

**Logging**:
- Writes to `C_Log` table with teller email
- Audit trail of access changes

---

### 12. POST `/Dashboard/AddFullTeller`
**Purpose**: Invite full teller (admin) to access election  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (owner only)  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "email": "string",   // Teller's email to invite
  "election": "guid"   // Election to grant access to
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string",
  "user": {
    "Role": "Full",
    "InviteWhen": null,
    "InviteEmail": "string",
    "C_RowId": int,
    "Email": null,
    "UserName": "PENDING",
    "LastActivityDate": null,
    "isCurrentUser": false
  }
}
```

**Business Logic**:
1. Verify current user is election OWNER:
   - Find user's join record for this election
   - Check that `Role` is null (null = owner)
   - If not owner: Return error "Adding not allowed"
2. Create `JoinElectionUser` record:
   - `ElectionGuid`: Election being shared
   - `UserId`: Guid.Empty (will be filled when invited user logs in)
   - `Role`: "Full"
   - `InviteEmail`: Email provided
3. Save to database
4. Log event: "Registered full teller - {email}"
5. Return pending user object

**Invitation Flow**:
1. Owner enters email address to invite
2. System creates PENDING invitation record
3. Owner sends invitation email (separate endpoint: `SendInvitation`)
4. Invited user receives email with link
5. Invited user logs in (or registers)
6. System matches email to invitation, fills in `UserId`
7. Invited user now has access to election

**Use Case**:
- Owner wants co-organizer to help manage election
- Grant access to another admin for backup
- Collaborative election management

**Security**:
- Only owner can add full tellers
- Invitation email must match actual user email on login
- Full tellers have almost same permissions as owner (except can't add/remove tellers)

**Logging**:
- Writes to `C_Log` table with email
- Audit trail of access grants

**Related Endpoint**: `SendInvitation` sends email to invited user

---

### 13. POST `/Dashboard/SendInvitation`
**Purpose**: Send invitation email to full teller  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (owner only)  
**Returns**: JSON with success/error

**Request Parameters**:
```csharp
{
  "joinId": int  // JoinElectionUser.C_RowId
}
```

**Response** (JSON):
```json
{
  "Success": bool,
  "Message": "string"
}
```

**Business Logic**:
1. Find `JoinElectionUser` record by `joinId`
2. Verify current user is election OWNER
3. Find election details
4. Call `EmailHelper.SendFullTellerInvitation(election, inviteEmail)`
5. Email contains:
   - Election name
   - Owner's name
   - Link to TallyJ
   - Instructions to log in with invited email

**Email Template** (approximate):
```
Subject: You've been invited to help with election: [Election Name]

[Owner Name] has invited you to be a full teller for the election "[Election Name]".

To accept this invitation:
1. Go to https://tallyj.com
2. Log in with this email address: [InviteEmail]
3. You will see "[Election Name]" in your election list

If you don't have an account, you can register using this email address.

Questions? Contact [Owner Name] at [Owner Email]
```

**Use Case**:
- After adding full teller, owner sends invitation email
- Invited user receives clear instructions
- Reduces support burden (user knows what to do)

**Side Effects**:
- Sends email via SMTP
- May update `JoinElectionUser.InviteWhen` timestamp

**Error Handling**:
- Invalid joinId
- User not owner
- Email service failure
- Invalid email address

---

## Business Logic Classes

### ElectionsListViewModel
**Location**: `CoreModels/ElectionsListViewModel.cs`

**Key Methods**:
- `MyElections()`: Returns all elections user owns or has access to
- `GetMyElectionsInfo(forceRefresh)`: Returns election list with statistics
- `MoreInfoStatic()`: Returns static election configuration
- `MoreInfoLive()`: Returns live election statistics

**Responsibilities**:
- Election list data access
- Election statistics calculation
- Permission checking (owner vs full teller)

### ComputerModel
**Location**: `CoreModels/ComputerModel.cs`

**Key Methods**:
- `GetTempComputerForMe()`: Creates temporary computer record
- `MoveCurrentComputerIntoLocation(locationId)`: Updates computer location

**Computer Record Purpose**:
- Tracks which election user is currently working on
- Tracks which location user is at
- Supports multi-election workflow (switch between elections)

### TellerModel
**Location**: `CoreModels/TellerModel.cs`

**Key Methods**:
- `ChooseTeller(num, tellerId, newName)`: Creates or assigns guest teller
- `DeleteTeller(id)`: Removes guest teller

**Responsibilities**:
- Guest teller management
- Teller number assignment
- Validation and error handling

### ElectionCacher
**Location**: `EF/Partials/ElectionCacher.cs`

**Key Methods**:
- `UpdateItemAndSaveCache(election)`: Updates cached election data

**Purpose**:
- Election data is heavily cached for performance
- Cache must be invalidated when election changes
- Used by all tellers to avoid database round trips

### EmailHelper
**Location**: `CoreModels/Helper/EmailHelper.cs`

**Key Methods**:
- `SendFullTellerInvitation(election, email)`: Sends invitation email

**Responsibilities**:
- Email formatting
- SMTP sending
- Error handling

---

## SignalR Hub Details

### PublicHub
**File**: `CoreModels/Hubs/PublicHub.cs`  
**Full Documentation**: `signalr/hubs-overview.md`

**Server → Client Methods**:
- `TellPublicAboutVisibleElections()`: Broadcasts list of elections visible to public

**Use Cases**:
- Public homepage shows available elections
- Guest tellers see elections they can join
- Updates when elections are listed/unlisted

### MainHub
**File**: `CoreModels/Hubs/MainHub.cs`  
**Full Documentation**: `signalr/hubs-overview.md`

**Server → Client Methods**:
- `StatusChangedForElection(electionGuid, info)`: Election status changed
- `CloseOutGuestTellers(electionGuid)`: Disconnect guest tellers

**Use Cases**:
- Dashboard shows real-time election status
- Guest tellers are logged out when election is unlisted
- All tellers see ballot counts update

---

## Authorization

### Controller-Level Authorization
**None** - Each endpoint has its own authorization

### Endpoint-Level Authorization

**`[AllowTellersInActiveElection]`**:
- Both admin and guest tellers allowed
- Must have active election selected
- Used for: Dashboard Index, LoadV2Election, ChooseLocation, ChooseTeller, DeleteTeller

**`[ForAuthenticatedTeller]`**:
- ONLY admin tellers (NOT guest tellers)
- Must be logged in with username/password
- Used for: ElectionList, MoreInfoStatic, MoreInfoLive, ReloadElections, UpdateListingForElection, RemoveFullTeller, AddFullTeller, SendInvitation

**Why Two Authorization Levels?**:
- Guest tellers can help with current election (enter ballots, register voters)
- Guest tellers CANNOT manage elections, invite others, or switch elections
- Owner-only actions (add/remove full tellers) verified in endpoint logic

---

## Session State Dependencies

**Required Session Variables**:
- `UserSession.CurrentElectionGuid`: Active election
- `UserSession.CurrentElection`: Cached election object
- `UserSession.IsKnownTeller`: True if admin
- `UserSession.IsGuestTeller`: True if guest
- `UserSession.CurrentComputer`: Computer record for this session
- `UserSession.UserGuid`: Admin user ID (for owner checks)

---

## Migration Notes for .NET Core + Vue 3

### API Changes
**Current**: Mix of GET/POST returning Views or JSON  
**Target**: RESTful API with consistent JSON responses

**Recommended Endpoints**:
```
GET    /api/dashboard                      → Dashboard data
GET    /api/elections                      → Election list
POST   /api/elections                      → Create election
GET    /api/elections/{id}                 → Election details
PATCH  /api/elections/{id}/visibility      → Update listing status
POST   /api/elections/{id}/tellers         → Add guest teller
DELETE /api/elections/{id}/tellers/{num}   → Remove guest teller
POST   /api/elections/{id}/full-tellers    → Invite full teller
DELETE /api/elections/{id}/full-tellers/{joinId} → Remove full teller
POST   /api/elections/{id}/full-tellers/{joinId}/invite → Send invitation email
PATCH  /api/session/location               → Change location
```

### Vue 3 Components

**Dashboard**:
```vue
<template>
  <div class="dashboard">
    <election-header :election="currentElection" />
    <election-stats :stats="liveStats" />
    <quick-actions :election="currentElection" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useSignalR } from '@/composables/useSignalR';

const currentElection = ref<Election | null>(null);
const liveStats = ref<ElectionStats | null>(null);
const { connection } = useSignalR('Main');

onMounted(async () => {
  currentElection.value = await fetchCurrentElection();
  liveStats.value = await fetchLiveStats();
  
  connection.on('StatusChangedForElection', (electionGuid: string, info: any) => {
    if (electionGuid === currentElection.value?.guid) {
      liveStats.value = { ...liveStats.value, ...info };
    }
  });
});
</script>
```

**Election List**:
```vue
<template>
  <div class="election-list">
    <h1>My Elections</h1>
    <button @click="createElection">Create New Election</button>
    
    <election-card 
      v-for="election in elections" 
      :key="election.guid"
      :election="election"
      @select="selectElection"
      @toggle-visibility="toggleVisibility"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useElectionStore } from '@/stores/election';

const electionStore = useElectionStore();
const elections = ref<Election[]>([]);

onMounted(async () => {
  elections.value = await electionStore.fetchElections();
});

const toggleVisibility = async (electionGuid: string, visible: boolean) => {
  await electionStore.updateVisibility(electionGuid, visible);
  // SignalR will broadcast update to public page
};
</script>
```

**Teller Management**:
```vue
<template>
  <div class="teller-management">
    <h2>Guest Tellers</h2>
    <guest-teller-slot 
      v-for="num in 10" 
      :key="num"
      :num="num"
      :teller="getTeller(num)"
      @assign="assignTeller"
      @remove="removeTeller"
    />
    
    <h2>Full Tellers</h2>
    <full-teller-list 
      :tellers="fullTellers"
      @add="addFullTeller"
      @remove="removeFullTeller"
      @send-invite="sendInvite"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

const guestTellers = ref<Teller[]>([]);
const fullTellers = ref<FullTeller[]>([]);

const assignTeller = async (num: number, name: string) => {
  await api.post(`/elections/${electionId}/tellers`, { num, name });
  await loadTellers();
};

const addFullTeller = async (email: string) => {
  await api.post(`/elections/${electionId}/full-tellers`, { email });
  await loadFullTellers();
};
</script>
```

### State Management (Pinia)
```typescript
// stores/election.ts
import { defineStore } from 'pinia';

export const useElectionStore = defineStore('election', {
  state: () => ({
    currentElection: null as Election | null,
    elections: [] as Election[],
    liveStats: null as ElectionStats | null
  }),
  
  actions: {
    async selectElection(guid: string) {
      const response = await api.post(`/elections/${guid}/select`);
      this.currentElection = response.data.election;
      router.push('/dashboard');
    },
    
    async updateVisibility(guid: string, visible: boolean) {
      await api.patch(`/elections/${guid}/visibility`, { visible });
      const election = this.elections.find(e => e.guid === guid);
      if (election) election.listForPublic = visible;
    },
    
    updateLiveStats(stats: Partial<ElectionStats>) {
      this.liveStats = { ...this.liveStats, ...stats };
    }
  }
});
```

### Authorization
```csharp
// Policy-based authorization for .NET Core
services.AddAuthorization(options =>
{
  options.AddPolicy("TellerInActiveElection", policy =>
    policy.RequireAssertion(context =>
    {
      var isKnownTeller = context.User.HasClaim("IsKnownTeller", "true");
      var isGuestTeller = context.User.HasClaim("IsGuestTeller", "true");
      var hasActiveElection = context.User.HasClaim(c => c.Type == "CurrentElectionGuid");
      return (isKnownTeller || isGuestTeller) && hasActiveElection;
    })
  );
  
  options.AddPolicy("AuthenticatedTeller", policy =>
    policy.RequireClaim("IsKnownTeller", "true")
  );
  
  options.AddPolicy("ElectionOwner", policy =>
    policy.RequireAssertion(context =>
    {
      // Must verify in endpoint logic based on database records
      return context.User.HasClaim("IsKnownTeller", "true");
    })
  );
});

// Usage in controllers
[Authorize(Policy = "TellerInActiveElection")]
public class DashboardController : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Index()
  {
    // Implementation
  }
  
  [Authorize(Policy = "AuthenticatedTeller")]
  [HttpGet("elections")]
  public async Task<IActionResult> GetElections()
  {
    // Implementation
  }
}
```

---

## Testing Recommendations

### Unit Tests
```csharp
[Test]
public void Index_NoCurrentElection_RedirectsToElectionList()
{
  // Arrange
  UserSession.CurrentElectionGuid = Guid.Empty;
  UserSession.IsKnownTeller = true;
  var controller = new DashboardController();
  
  // Act
  var result = controller.Index() as RedirectToRouteResult;
  
  // Assert
  Assert.That(result.RouteValues["action"], Is.EqualTo("ElectionList"));
}

[Test]
public void AddFullTeller_NotOwner_ReturnsError()
{
  // Arrange
  var controller = new DashboardController();
  // Setup user as non-owner
  
  // Act
  var result = controller.AddFullTeller("test@example.com", electionGuid);
  
  // Assert
  Assert.That(result.Value.Success, Is.False);
  Assert.That(result.Value.Message, Contains.Substring("not allowed"));
}
```

### Integration Tests
```csharp
[Test]
public async Task UpdateVisibility_BroadcastsToSignalR()
{
  // Arrange
  var hubConnection = ConnectToPublicHub();
  var electionsUpdated = false;
  hubConnection.On("TellPublicAboutVisibleElections", () => electionsUpdated = true);
  
  // Act
  await PostAsync("/Dashboard/UpdateListingForElection", 
    new { listOnPage = true, electionGuid });
  await Task.Delay(100);
  
  // Assert
  Assert.That(electionsUpdated, Is.True);
}
```

---

## Related Documentation

- **Authentication**: `security/authentication.md` - Admin + Guest Teller auth
- **SignalR Hubs**: `signalr/hubs-overview.md` - MainHub, PublicHub details
- **Database**: `database/entities.md` - Election, Computer, Teller, JoinElectionUser entities
- **UI Screenshots**: `ui-screenshots-analysis.md` - Dashboard screenshot

---

## Summary

DashboardController is the central hub for election management:
- **Dashboard**: Main landing page with election status
- **Election List**: View and manage all elections
- **Teller Management**: Invite/remove both guest and full tellers
- **Visibility Control**: Show/hide elections from public
- **Location Selection**: Track which voting booth teller is at

Key architectural features:
- **Role-based access**: Owner vs Full Teller vs Guest Teller
- **Real-time updates**: SignalR for election status changes
- **Multi-election support**: Admins can manage multiple elections
- **Computer tracking**: Each session has computer record for location/election context
