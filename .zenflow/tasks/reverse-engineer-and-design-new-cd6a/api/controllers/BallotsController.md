# BallotsController API Documentation

## Overview
**Purpose**: Ballot entry, vote recording, ballot management, and location tracking  
**Base Route**: `/Ballots`  
**Authorization**: `[AllowTellersInActiveElection]` at controller level  
**Authentication**: Requires authenticated teller (admin or guest) with active election session

## Context

This controller handles the core ballot entry workflow:
- Displaying ballot entry interface (normal and single-name elections)
- Recording individual votes on ballots
- Managing ballot status (Ok, NeedsReview, Spoiled)
- Switching between ballots
- Tracking votes per location
- Reconciling ballot counts
- Sorting ballots for review

**Election Types Supported**:
1. **Normal Election**: 9-member ballot (e.g., LSA elections)
2. **Single-Name Election**: 1-position ballot (e.g., chairperson)

---

## Endpoints

### 1. GET `/Ballots/Index`
**Purpose**: Display ballot entry page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View ("BallotNormal" or "BallotSingle" based on election type)

**Query Parameters**:
- `l` (int, optional): Location ID to switch to
- `b` (int, optional): Ballot ID to load

**Business Logic**:
1. Check if `l` (locationId) provided:
   - If provided and different from current → Attempt to switch location
   - Call `ComputerModel().MoveCurrentComputerIntoLocation(locationId)`
   - If switch fails (location locked/unavailable) → Redirect to Dashboard
2. Determine if single-name election: `UserSession.CurrentElection.IsSingleNameElection`
3. Get/create current ballot:
   - If `b` (ballotId) provided → Load specific ballot via `SetAsCurrentBallot(ballotId)`
   - If ballotId = 0 and single-name → Auto-get current ballot
   - Update ballot filter if ballot's ComputerCode differs from session filter
4. Return appropriate view:
   - **Normal**: "BallotNormal" view with ballot model
   - **Single**: "BallotSingle" view with ballot model

**Response Model** (passed to view):
```csharp
IBallotModel {
  CurrentBallot: Ballot,
  Votes: List<Vote>,
  People: List<Person>,  // Eligible candidates
  LocationInfo: { ... }
}
```

**Session State**:
- `UserSession.CurrentLocation` - Active location/polling place
- `UserSession.CurrentBallotFilter` - Computer code filter for ballot list

**Notes**:
- Entry point for ballot entry workflow
- Handles location switching and ballot loading
- Single-name elections auto-load ballot on page load

---

### 2. GET `/Ballots/Reconcile`
**Purpose**: Display ballot reconciliation page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View with `PeopleModel`

**Business Logic**:
- Instantiate `PeopleModel`
- Pass model to view

**Notes**:
- Used to compare physical ballot count vs. entered ballot count
- Helps identify missing or duplicate ballots

---

### 3. GET `/Ballots/SortBallots`
**Purpose**: Display ballot sorting/filtering page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View with `PeopleModel`

**Business Logic**:
- Instantiate `PeopleModel`
- Pass model to view

**Notes**:
- Used to review and sort ballots by status, location, or computer code

---

### 4. POST `/Ballots/BallotsForLocation`
**Purpose**: Get list of ballots and voters for a specific location  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with ballots and deselected voters

**Request**:
```
POST /Ballots/BallotsForLocation?id=5
```

**Response**:
```json
{
  "Ballots": [
    {
      "BallotId": 101,
      "LocationName": "Main Hall",
      "StatusCode": "Ok",
      "ComputerCode": "A",
      "Votes": []
    }
  ],
  "Deselected": [
    {
      "PersonId": 25,
      "FullName": "John Smith",
      "IneligibleReasonGuid": "...",
      "Reason": "Ineligible"
    }
  ]
}
```

**Business Logic**:
- Call `PeopleModel().BallotSources(locationId)`
- Call `PeopleModel().Deselected()` to get ineligible voters

**Notes**:
- Used by Reconcile page
- Shows which ballots were entered at which location
- Shows voters marked as ineligible

---

### 5. POST `/Ballots/SaveVote`
**Purpose**: Record a vote for a candidate on current ballot  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status and updated vote

**Request Parameters**:
- `pid` (int): Person ID (candidate being voted for)
- `vid` (int): Vote ID (0 for new vote, existing ID for update)
- `invalid` (string, optional): Invalid reason GUID if vote is spoiled
- `count` (int, optional): Vote count (used in tie-breaks or single-name)
- `lastVid` (int, optional): Last vote ID (for optimistic concurrency)
- `verifying` (bool, optional): Whether this is verification pass

**Response**:
```json
{
  "Success": true,
  "Vote": {
    "VoteId": 1234,
    "PersonId": 25,
    "FullName": "Alice Johnson",
    "Position": 1,
    "StatusCode": "Ok",
    "InvalidReasonGuid": null
  },
  "BallotStatus": "Ok",
  "Message": ""
}
```

**Business Logic**:
1. Call `CurrentBallotModel.SaveVote(pid, vid, invalidGuid, lastVid, count, verifying)`
2. If vid = 0 → Create new Vote record
3. If vid > 0 → Update existing Vote record
4. Validate:
   - Person is eligible candidate
   - Ballot is not finalized
   - Position is valid (1-9 for normal, 1 for single-name)
   - No duplicate votes for same person on ballot
5. Set Vote.StatusCode:
   - `Ok` if valid vote
   - `Unreadable` if invalid GUID provided
   - `Changed` if candidate name changed during entry
6. Update ballot status if all votes valid
7. Return updated vote

**Side Effects**:
- Inserts/updates `Vote` table
- May update `Ballot.StatusCode`
- Triggers SignalR update to other tellers

**SignalR Hub Triggered**:
- **MainHub**: `voteAdded` or `voteUpdated` - Notify other tellers
- **FrontDeskHub**: May update voter status

**Notes**:
- Called on every keystroke/candidate selection in ballot entry UI
- Position is auto-calculated (next available position on ballot)
- Invalid votes still recorded with InvalidReasonGuid

---

### 6. POST `/Ballots/DeleteVote`
**Purpose**: Remove a vote from current ballot  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Request**:
```
POST /Ballots/DeleteVote?vid=1234
```

**Response**:
```json
{
  "Success": true,
  "Message": "Vote deleted"
}
```

**Business Logic**:
1. Call `CurrentBallotModel.DeleteVote(vid)`
2. Validate:
   - Vote belongs to current ballot
   - Election not finalized
3. Delete Vote record
4. Re-number remaining votes (adjust positions)

**Side Effects**:
- Deletes from `Vote` table
- Updates positions on remaining votes
- May update `Ballot.StatusCode`

**SignalR Hub Triggered**:
- **MainHub**: `voteDeleted` - Notify other tellers

**Notes**:
- Used when teller makes data entry error
- Positions automatically re-numbered (e.g., delete vote 2 → vote 3 becomes vote 2)

---

### 7. POST `/Ballots/NeedsReview`
**Purpose**: Mark current ballot as needing review  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Request**:
```
POST /Ballots/NeedsReview?needs=true
```

**Response**:
```json
{
  "Success": true,
  "NeedsReview": true
}
```

**Business Logic**:
- Call `CurrentBallotModel.SetNeedsReview(needs)`
- Update `Ballot.StatusCode` to "Review" or back to "Ok"

**Side Effects**:
- Updates `Ballot.StatusCode`

**Notes**:
- Used when teller is unsure about ballot (illegible writing, unclear intent)
- Flagged ballots reviewed by head teller later

---

### 8. POST `/Ballots/SwitchToBallot`
**Purpose**: Change current ballot to a different ballot  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with ballot info

**Request**:
```
POST /Ballots/SwitchToBallot?ballotId=105&refresh=false
```

**Response**:
```json
{
  "BallotInfo": {
    "BallotId": 105,
    "LocationName": "Main Hall",
    "StatusCode": "Ok",
    "ComputerCode": "A"
  },
  "Votes": [
    {
      "VoteId": 501,
      "FullName": "Alice Johnson",
      "Position": 1
    }
  ],
  "Success": true
}
```

**Business Logic**:
- Call `CurrentBallotModel.SwitchToBallotAndGetInfo(ballotId, refresh)`
- Load ballot and votes
- Return ballot info and votes

**Session State**:
- Updates session's current ballot

**Notes**:
- Used when reviewing or editing previously entered ballots
- If refresh=true, reload ballot from database (discard local changes)

---

### 9. POST `/Ballots/UpdateLocationStatus`
**Purpose**: Change location status (Open, Closed, etc.)  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Request**:
```
POST /Ballots/UpdateLocationStatus?id=5&status=Closed
```

**Status Values**:
- `Open` - Location accepting ballots
- `Closed` - Voting closed, no more ballots
- `Receiving` - Ballots being collected but not entered yet

**Response**:
```json
{
  "Success": true,
  "Status": "Closed"
}
```

**Business Logic**:
- Call `LocationModel.UpdateStatus(id, status)`
- Update `Location.TallyStatus` field

**Side Effects**:
- Updates `Location` table
- May trigger SignalR update to monitoring dashboard

**SignalR Hub Triggered**:
- **MainHub**: `locationStatusChanged`

**Notes**:
- Used by tellers at each polling location
- Helps head teller track which locations finished voting

---

### 10. POST `/Ballots/UpdateLocationInfo`
**Purpose**: Update location notes/comments  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Request**:
```
POST /Ballots/UpdateLocationInfo?info=50 ballots collected
```

**Response**:
```json
{
  "Success": true,
  "Info": "50 ballots collected"
}
```

**Business Logic**:
- Call `LocationModel.UpdateLocationInfo(info)`
- Update `Location.ContactInfo` or similar field

**Side Effects**:
- Updates `Location` table

**Notes**:
- Free-text field for teller notes
- Visible to head teller on monitor page

---

### 11. POST `/Ballots/GetLocationInfo`
**Purpose**: Retrieve current location and ballot information  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with location and ballot data

**Response**:
```json
{
  "Location": {
    "LocationId": 5,
    "LocationName": "Main Hall",
    "Status": "Open",
    "ContactInfo": "50 ballots collected",
    "NumCollected": 50,
    "NumEntered": 45
  },
  "BallotInfo": {
    "BallotId": 105,
    "StatusCode": "Ok",
    "ComputerCode": "A"
  },
  "Ballots": [
    {
      "BallotId": 101,
      "StatusCode": "Ok",
      "ComputerCode": "A"
    }
    // ...more ballots
  ]
}
```

**Business Logic**:
1. Validate current location is set
2. Call `LocationModel.CurrentBallotLocationInfo()`
3. Call `CurrentBallotModel.CurrentBallotInfo()`
4. Call `CurrentBallotModel.CurrentBallotsInfoList()`
5. Return combined data

**Notes**:
- Called by ballot entry page to initialize UI
- Returns all ballots at current location
- Used for ballot navigation sidebar

---

### 12. POST `/Ballots/UpdateLocationCollected`
**Purpose**: Update count of physical ballots collected at location  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Request**:
```
POST /Ballots/UpdateLocationCollected?numCollected=50
```

**Response**:
```json
{
  "Success": true,
  "NumCollected": 50,
  "NumEntered": 45,
  "Remaining": 5
}
```

**Business Logic**:
- Call `LocationModel.UpdateNumCollected(numCollected)`
- Update `Location.NumCollected` field
- Calculate remaining: NumCollected - NumEntered

**Side Effects**:
- Updates `Location` table

**Notes**:
- Used for reconciliation (ensure all physical ballots entered)
- NumEntered auto-calculated from ballot count

---

### 13. POST `/Ballots/RefreshBallotsList`
**Purpose**: Reload ballot list for current location  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with ballot list

**Response**:
```json
[
  {
    "BallotId": 101,
    "StatusCode": "Ok",
    "ComputerCode": "A",
    "Votes": []
  }
  // ...more ballots
]
```

**Business Logic**:
- Call `CurrentBallotModel.CurrentBallotsInfoList(forceRefresh: true)`
- Query database for updated ballot list

**Notes**:
- Used to refresh UI after ballot entry
- Force refresh bypasses cache

---

### 14. POST `/Ballots/ChangeBallotFilter`
**Purpose**: Filter ballot list by computer code  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with filtered ballot list

**Request**:
```
POST /Ballots/ChangeBallotFilter?code=A
```

**Response**:
```json
[
  {
    "BallotId": 101,
    "StatusCode": "Ok",
    "ComputerCode": "A"
  }
  // ...only ballots with ComputerCode = "A"
]
```

**Business Logic**:
- Set `UserSession.CurrentBallotFilter = code`
- Call `CurrentBallotModel.CurrentBallotsInfoList()`
- Return filtered list

**Session State**:
- `UserSession.CurrentBallotFilter` - Persisted across requests

**Notes**:
- Used when multiple tellers at same location
- Each teller assigned computer code (A, B, C, etc.)
- Filter shows only ballots entered by that teller

---

### 15. POST `/Ballots/SortVotes`
**Purpose**: Reorder votes on current ballot  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Request**:
```json
{
  "idList": [505, 501, 503, 502, 504]  // Vote IDs in desired order
}
```

**Response**:
```json
{
  "Success": true,
  "Message": "Votes reordered"
}
```

**Business Logic**:
1. Validate election not finalized
2. Call `CurrentBallotModel.SortVotes(idList, voteCacher)`
3. Update Vote.Position for each vote based on order in idList
4. Save changes

**Side Effects**:
- Updates `Vote.Position` for all votes in list
- Does NOT trigger SignalR (local operation)

**Notes**:
- Used when ballot has votes in wrong order
- Frontend allows drag-and-drop reordering

---

### 16. POST `/Ballots/NewBallot`
**Purpose**: Create a new blank ballot  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with new ballot info

**Response**:
```json
{
  "BallotId": 151,
  "LocationId": 5,
  "StatusCode": "Ok",
  "ComputerCode": "A",
  "Success": true
}
```

**Business Logic**:
1. Call `CurrentBallotModel.StartNewBallotJson()`
2. Create new Ballot record:
   - LocationId = UserSession.CurrentLocation
   - ComputerCode = UserSession.ComputerCode or auto-generated
   - StatusCode = "Ok"
3. Set as current ballot in session
4. Return ballot info

**Side Effects**:
- Inserts into `Ballot` table
- Updates session's current ballot

**SignalR Hub Triggered**:
- **MainHub**: `ballotCreated` - Notify other tellers

**Notes**:
- Called automatically when teller finishes entering previous ballot
- Each ballot starts empty (no votes)

---

### 17. POST `/Ballots/DeleteBallot`
**Purpose**: Delete current ballot  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with success status

**Response**:
```json
{
  "Success": true,
  "Message": "Ballot deleted"
}
```

**Business Logic**:
1. Call `CurrentBallotModel.DeleteBallotJson()`
2. Validate:
   - Ballot exists
   - Election not finalized
3. Delete all votes on ballot
4. Delete ballot record
5. Clear current ballot from session

**Side Effects**:
- Deletes from `Vote` table (all votes on ballot)
- Deletes from `Ballot` table
- Updates session

**SignalR Hub Triggered**:
- **MainHub**: `ballotDeleted`

**Notes**:
- Used when ballot entered by mistake
- Irreversible (no soft delete)

---

## Authorization Attributes Used

| Attribute | Endpoints | Purpose |
|-----------|-----------|---------|
| `[AllowTellersInActiveElection]` | All (controller-level) | Require authenticated teller with active election |

**Note**: No method-level authorization overrides. All endpoints available to both known and guest tellers.

---

## Business Logic Classes Called

| Class | Methods | Purpose |
|-------|---------|---------|
| `BallotModelFactory` | GetForCurrentElection | Get ballot model for normal vs. single-name election |
| `IBallotModel` (interface) | SaveVote, DeleteVote, SetNeedsReview, SwitchToBallot, CurrentBallotInfo, CurrentBallotsInfoList, SortVotes, StartNewBallot, DeleteBallot | All ballot operations |
| `PeopleModel` | BallotSources, Deselected | Voter and ballot listing |
| `LocationModel` | UpdateStatus, UpdateLocationInfo, CurrentBallotLocationInfo, UpdateNumCollected | Location management |
| `ComputerModel` | MoveCurrentComputerIntoLocation | Location switching |
| `VoteCacher` | (used by SortVotes) | Performance optimization for vote updates |

**Ballot Model Implementations**:
- `BallotModelCore` - Normal 9-member elections
- `BallotModelSingle` - Single-name elections (1 position)

---

## SignalR Hubs Triggered

| Hub | Methods | Triggered By |
|-----|---------|--------------|
| `MainHub` | voteAdded, voteUpdated, voteDeleted | SaveVote, DeleteVote |
| `MainHub` | ballotCreated, ballotDeleted | NewBallot, DeleteBallot |
| `MainHub` | locationStatusChanged | UpdateLocationStatus |
| `FrontDeskHub` | voterVoted | SaveVote (may update voter status) |

---

## Database Tables Accessed

| Table | Operations | Purpose |
|-------|------------|---------|
| `Ballot` | SELECT, INSERT, UPDATE, DELETE | Ballot records |
| `Vote` | SELECT, INSERT, UPDATE, DELETE | Individual votes on ballots |
| `Location` | SELECT, UPDATE | Polling location info |
| `Person` | SELECT | Candidate list |
| `Computer` | SELECT, UPDATE | Teller workstation tracking |
| `Election` | SELECT | Current election settings |

---

## Session State Management

**UserSession Properties**:
- `UserSession.CurrentLocation` - Active polling location
- `UserSession.CurrentBallotFilter` - Computer code filter
- `UserSession.CurrentElection` - Active election
- `UserSession.ComputerCode` - Teller's workstation code

**Current Ballot Tracking**:
- Ballot ID stored in session
- Retrieved via `CurrentBallotModel` property

---

## Key Workflows

### Ballot Entry Workflow (Normal Election)
1. Teller navigates to `/Ballots/Index?l=5` (select location)
2. System creates new ballot → POST `/Ballots/NewBallot`
3. Teller enters vote 1 → POST `/Ballots/SaveVote?pid=25&vid=0&position=1`
4. Teller enters votes 2-9 → Repeat SaveVote
5. Teller clicks "Next Ballot" → POST `/Ballots/NewBallot`
6. Repeat for all ballots

### Ballot Entry Workflow (Single-Name Election)
1. Teller navigates to `/Ballots/Index`
2. System auto-loads current ballot
3. Teller enters candidate name → POST `/Ballots/SaveVote?pid=25&vid=0`
4. System auto-advances to next ballot
5. Repeat for all ballots

### Ballot Review Workflow
1. Teller clicks ballot from sidebar
2. POST `/Ballots/SwitchToBallot?ballotId=105`
3. Teller corrects vote → POST `/Ballots/SaveVote?vid=501&pid=30` (update)
4. Or deletes vote → POST `/Ballots/DeleteVote?vid=501`

### Location Reconciliation Workflow
1. Teller enters physical count → POST `/Ballots/UpdateLocationCollected?numCollected=50`
2. System shows: 50 collected, 45 entered, 5 remaining
3. Teller enters missing ballots
4. When complete → POST `/Ballots/UpdateLocationStatus?id=5&status=Closed`

---

## Election Type Differences

### Normal Election (9-member)
- Ballot has up to 9 votes
- Vote positions 1-9
- View: `BallotNormal.cshtml`
- Model: `BallotModelCore`

### Single-Name Election (1-position)
- Ballot has exactly 1 vote
- Vote position always 1
- Auto-advance to next ballot after each vote
- View: `BallotSingle.cshtml`
- Model: `BallotModelSingle`

**Key Difference**: Single-name elections optimize for speed (one keystroke per ballot vs. nine).

---

## Migration Notes for .NET Core

### Recommended .NET Core Structure
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "TellerInActiveElection")]
public class BallotsController : ControllerBase
{
  private readonly IBallotService _ballotService;
  private readonly ILocationService _locationService;
  private readonly IHubContext<MainHub> _mainHub;

  [HttpGet]
  public async Task<ActionResult<BallotDto>> GetCurrentBallot() { ... }

  [HttpPost]
  public async Task<ActionResult<BallotDto>> CreateBallot([FromBody] CreateBallotDto dto) { ... }

  [HttpPut("{ballotId}")]
  public async Task<ActionResult<BallotDto>> UpdateBallot(int ballotId, [FromBody] UpdateBallotDto dto) { ... }

  [HttpDelete("{ballotId}")]
  public async Task<IActionResult> DeleteBallot(int ballotId) { ... }

  [HttpPost("{ballotId}/votes")]
  public async Task<ActionResult<VoteDto>> AddVote(int ballotId, [FromBody] VoteDto vote) { ... }

  [HttpPut("votes/{voteId}")]
  public async Task<ActionResult<VoteDto>> UpdateVote(int voteId, [FromBody] VoteDto vote) { ... }

  [HttpDelete("votes/{voteId}")]
  public async Task<IActionResult> DeleteVote(int voteId) { ... }
}
```

### Key Changes
- RESTful routes (resource-based)
- Async/await for all database operations
- DTOs for request/response
- Separate LocationsController for location operations
- Strongly-typed SignalR hubs
- Stateless API (ballot ID in request, not session)

### Frontend Changes (Vue 3)
- Replace view with Vue 3 SFC: `BallotEntry.vue`
- Pinia store for ballot state
- SignalR composable for real-time updates
- Optimistic UI updates

---

## Performance Considerations

1. **Vote Saving**: High-frequency operation (every keystroke)
   - Must be <100ms response time
   - Consider debouncing in frontend (300ms)
   - Use indexed lookups (Vote.BallotId, Vote.PersonId)

2. **Ballot List Refresh**: Moderate frequency (every ballot)
   - Cache ballot list per location
   - Invalidate cache on ballot changes

3. **SignalR Broadcasting**: Every vote triggers broadcast
   - Limit to election-specific groups
   - Batch updates if multiple votes saved rapidly

4. **Location Switching**: Rare operation
   - Lock validation must be fast
   - Use optimistic locking (timestamp/version)

**Optimization Recommendations**:
- Index: `Vote.BallotId`, `Vote.PersonId`, `Ballot.LocationId`
- Cache: Person list (candidates)
- Batch: Vote position updates during SortVotes
- Connection pool: Ensure adequate DB connections for concurrent tellers

---

## Testing Scenarios

1. **Ballot Entry**
   - Navigate to `/Ballots/Index?l=5` → Ballot entry page loads
   - Enter 9 votes → POST `/Ballots/SaveVote` × 9
   - Verify votes saved with correct positions
   - Verify SignalR broadcasts to other tellers

2. **Vote Correction**
   - Load ballot → POST `/Ballots/SwitchToBallot?ballotId=105`
   - Update vote → POST `/Ballots/SaveVote?vid=501&pid=30`
   - Delete vote → POST `/Ballots/DeleteVote?vid=501`
   - Verify positions re-numbered after delete

3. **Location Management**
   - Switch location → Navigate to `/Ballots/Index?l=6`
   - Update collected count → POST `/Ballots/UpdateLocationCollected?numCollected=50`
   - Close location → POST `/Ballots/UpdateLocationStatus?id=6&status=Closed`
   - Verify monitor page shows updated status

4. **Ballot Filtering**
   - Enter ballots with ComputerCode "A"
   - Apply filter → POST `/Ballots/ChangeBallotFilter?code=A`
   - Verify only "A" ballots shown

5. **Concurrency**
   - Two tellers enter ballots at same location simultaneously
   - Verify no lost votes
   - Verify ballot counts correct
   - Verify SignalR updates both tellers

---

## Error Handling

### Common Errors
1. **Location Locked**: Another teller already in location
   - Redirect to Dashboard with error message
2. **Election Finalized**: Cannot modify ballots
   - Return error JSON: `{ "Message": "Election is finalized. No changes allowed." }`
3. **Invalid Candidate**: Person ID not in election
   - Return error JSON: `{ "Success": false, "Message": "Invalid candidate" }`
4. **Duplicate Vote**: Candidate already voted for on ballot
   - Return error JSON: `{ "Success": false, "Message": "Already voted for this candidate" }`

---

## API Endpoint Summary

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Ballots/Index` | Ballot entry page | Teller |
| GET | `/Ballots/Reconcile` | Reconciliation page | Teller |
| GET | `/Ballots/SortBallots` | Ballot sorting page | Teller |
| POST | `/Ballots/BallotsForLocation` | Get ballots for location | Teller |
| POST | `/Ballots/SaveVote` | Record vote | Teller |
| POST | `/Ballots/DeleteVote` | Delete vote | Teller |
| POST | `/Ballots/NeedsReview` | Flag ballot for review | Teller |
| POST | `/Ballots/SwitchToBallot` | Load different ballot | Teller |
| POST | `/Ballots/UpdateLocationStatus` | Change location status | Teller |
| POST | `/Ballots/UpdateLocationInfo` | Update location notes | Teller |
| POST | `/Ballots/GetLocationInfo` | Get location and ballot data | Teller |
| POST | `/Ballots/UpdateLocationCollected` | Update physical count | Teller |
| POST | `/Ballots/RefreshBallotsList` | Reload ballot list | Teller |
| POST | `/Ballots/ChangeBallotFilter` | Filter by computer code | Teller |
| POST | `/Ballots/SortVotes` | Reorder votes | Teller |
| POST | `/Ballots/NewBallot` | Create ballot | Teller |
| POST | `/Ballots/DeleteBallot` | Delete ballot | Teller |

**Total Endpoints**: 17 (3 GET views, 14 POST JSON APIs)

---

## Security Notes

1. **Location Isolation**: Tellers can only access ballots at their assigned location
2. **Computer Code Tracking**: All ballots tagged with teller's workstation code
3. **Finalization Check**: All modifications blocked after election finalized
4. **Audit Trail**: All vote saves logged (implicit via database timestamp)

---

## Special Considerations

### Single-Name Election Optimizations
- Auto-advance to next ballot after each vote
- No position management (always position 1)
- Faster entry workflow (1 keystroke per ballot vs. 9)

### Multi-Teller Coordination
- SignalR keeps all tellers synchronized
- Location locking prevents conflicts
- Computer code filtering shows each teller their ballots

### Ballot Review and Correction
- Any teller can review/correct any ballot at their location
- Use SwitchToBallot to load ballot
- Updates reflected immediately to all tellers

### Reconciliation Accuracy
- Physical count (NumCollected) vs. entered count (NumEntered)
- Must match before closing location
- Helps prevent lost or duplicate ballots
