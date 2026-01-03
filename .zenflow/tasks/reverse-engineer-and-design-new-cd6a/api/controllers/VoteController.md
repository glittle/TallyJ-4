# VoteController API Documentation

## Overview
**Purpose**: Online voting - voter ballot creation, submission, and management  
**Base Route**: `/Vote`  
**Authorization**: `[AllowVoter]` (class-level) - requires voter authentication  
**Authentication System**: Voter authentication (System 3) - email/SMS/kiosk codes (NO PASSWORDS)

## Controller Details

This controller manages the complete online voting workflow:
- **Voter home page** - List of elections voter can participate in
- **Election joining** - Validate voter eligibility and load ballot
- **Ballot management** - Save draft ballot (pool of votes)
- **Ballot submission** - Lock ballot and mark as submitted
- **SignalR integration** - Real-time updates for voter
- **Login history** - Track voter activity

**Key Business Logic Classes**:
- `OnlineVoteHelper` - Ballot encryption/decryption
- `NotificationHelper` - Send confirmation emails/SMS
- `PeopleModel` - Update voter status
- `PersonCacher` - Cache voter records
- `LogHelper` - Audit logging

**SignalR Hubs**:
- `AllVotersHub` - Broadcast to all voters
- `VoterPersonalHub` - Personal notifications to individual voter

**Session State**:
- `UserSession.VoterId` - Email, phone, or kiosk code
- `UserSession.VoterIdType` - Email / Phone / Kiosk
- `UserSession.CurrentElectionGuid` - Active election
- `UserSession.VoterInElectionPersonGuid` - Voter's Person record GUID
- `UserSession.VoterLoginSource` - Login method (e.g., "Email", "SMS", "Kiosk")

---

## Endpoints

### 1. GET `/Vote/Index` (or `/Vote/`)
**Purpose**: Display voter home page - list of elections  
**HTTP Method**: GET  
**Authorization**: `[AllowVoter]`  
**Returns**: View "VoteHome"

**Response**:
- Renders `Views/Vote/VoteHome.cshtml`
- Shows all elections voter is eligible to participate in

**Business Logic**:
- Client calls `GetVoterElections` to load election list

**SignalR**: 
- Client joins `AllVotersHub` and `VoterPersonalHub` after page load

**UI**: Voter sees list of elections with online voting status (open/closed/submitted)

---

### 2. POST `/Vote/JoinVoterHubs`
**Purpose**: Join SignalR hubs for real-time voter updates  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: void

**Request Parameters**:
- `connId` (string) - SignalR connection ID

**Business Logic**:
```csharp
new AllVotersHub().Join(connId);
new VoterPersonalHub().Join(connId);
```

**SignalR**:
- **AllVotersHub**: Receives broadcasts to all voters in election
- **VoterPersonalHub**: Receives personal notifications (ballot confirmation, deadline reminders)

**Migration Notes**:
- .NET Core SignalR: Join hubs directly in hub methods, not controller endpoints

---

### 3. POST `/Vote/JoinElection`
**Purpose**: Join an election and load ballot interface  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON with election and ballot data

**Request Parameters**:
- `electionGuid` (Guid) - Election to join

**Response** (Voting Window Open):
```json
{
  "open": true,
  "voterName": "John Smith",
  "NumberToElect": 9,
  "OnlineSelectionProcess": "R",  // R=Random, L=List, B=Both
  "RandomizeVotersList": true,
  "registration": "Online",
  "votingInfo": {
    "ElectionGuid": "...",
    "PersonGuid": "...",
    "Status": "Draft",
    "WhenStatus": "2024-01-15T10:30:00Z",
    "ListPool": "[{\"Id\":123,\"Name\":\"Candidate 1\"}]",  // Encrypted
    "PoolLocked": false
  },
  "poolDecryptError": null
}
```

**Response** (Voting Window Closed):
```json
{
  "closed": true,
  "votingInfo": {
    "Status": "Submitted",
    "WhenStatus": "2024-01-15T19:45:00Z"
  }
}
```

**Error Response**:
```json
{
  "Error": "Invalid election"
}
```

**Business Logic**:
1. Validates `UserSession.VoterId` is set (voter authenticated)
2. Matches voter to `Person` record in election:
   - **Email voters**: `Person.Email == VoterId`
   - **Phone voters**: `Person.Phone == VoterId`
   - **Kiosk voters**: `Person.KioskCode == VoterId`
3. Checks voting window:
   - `Election.OnlineWhenOpen <= NOW < Election.OnlineWhenClose` → open
   - Otherwise → closed
4. Loads or creates `OnlineVotingInfo` record:
   - If new: Status = "New", ListPool = null
   - If existing: Decrypt `ListPool` if encrypted
5. Sets session state:
   - `UserSession.CurrentElectionGuid`
   - `UserSession.VoterInElectionPersonGuid`
6. Returns election and ballot data

**Data Access**:
- `Person` table (find voter by email/phone/kiosk code)
- `Election` table (voting window check)
- `OnlineVotingInfo` table (ballot status)

**Encryption**:
- `ListPool` is encrypted in database using `EncryptionHelper`
- Decrypted using `OnlineVoteHelper.GetDecryptedListPool()`

**Authentication Validation**:
- If voter's email/phone/kiosk doesn't match any `Person` in election → "Invalid election" error
- This is why voter import CSV must have accurate email/phone data

**Migration Notes**:
- Encryption key management: Use ASP.NET Core Data Protection API
- Consider using per-voter encryption keys (derived from PersonGuid)

---

### 4. POST `/Vote/LeaveElection`
**Purpose**: Leave current election (clear session state)  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON success

**Response**:
```json
{
  "success": true
}
```

**Business Logic**:
- Removes `CurrentElectionGuid` and `VoterInElectionPersonGuid` from session

**Migration Notes**:
- .NET Core JWT: Not needed (client just discards token or navigates away)

---

### 5. POST `/Vote/SavePool`
**Purpose**: Save draft ballot (pool of selected votes)  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON with save result

**Request Parameters**:
- `pool` (string) - JSON array of vote selections

**Pool Format**:
```json
[
  { "Id": 123, "Name": "Candidate 1" },
  { "Id": 456, "Name": "Candidate 2" },
  { "Id": 789, "Name": "Candidate 3" }
]
```

**Response**:
```json
{
  "success": true,
  "newStatus": "Draft"
}
```

**Error Response**:
```json
{
  "Error": "Closed"
}
```

**Business Logic**:
1. Validates voting window is still open
2. Determines ballot status:
   - If `pool == "[]"` → Status = "New"
   - If `pool.length > 0` → Status = "Draft"
3. Encrypts `pool` and stores in `OnlineVotingInfo.ListPool`
4. Updates `OnlineVotingInfo.WhenStatus` to current UTC time
5. Saves to database

**Data Access**:
- Updates `OnlineVotingInfo` table
- Uses `OnlineVoteHelper.SetListPoolEncrypted()`

**Auto-save Pattern**:
- Client typically calls this endpoint every few seconds while voter is selecting candidates
- Allows voter to close browser and resume later

**Encryption**:
- `ListPool` is encrypted before storing (see `OnlineVoteHelper`)

**Migration Notes**:
- Consider debouncing auto-save (wait 3 seconds after last change)
- Use optimistic concurrency (ETag or RowVersion) to detect conflicts

---

### 6. POST `/Vote/LockPool`
**Purpose**: Submit ballot (lock pool and mark as submitted)  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON with lock result

**Request Parameters**:
- `locked` (bool) - true = submit ballot, false = recall ballot

**Response** (Submit):
```json
{
  "success": true,
  "notificationType": "Email",  // or "SMS" or null
  "VotingMethod": "O",  // O=Online
  "ElectionGuid": "...",
  "RegistrationTime": "2024-01-15T19:45:00Z",
  "WhenStatus": "2024-01-15T19:45:00Z",
  "PoolLocked": true
}
```

**Response** (Recall):
```json
{
  "success": true,
  "notificationType": null,
  "VotingMethod": null,
  "ElectionGuid": "...",
  "RegistrationTime": null,
  "WhenStatus": "2024-01-15T19:50:00Z",
  "PoolLocked": false
}
```

**Error Responses**:
```json
{ "Error": "Closed" }              // Voting window closed
{ "Error": "Ballot already processed" }
{ "Error": "Pool is empty" }
{ "Error": "Too few votes (3)" }   // Required 9 votes
{ "Error": "Cannot vote" }         // Person.CanVote = false
```

**Business Logic** (Submit - `locked = true`):
1. Validates voting window is still open
2. Ensures `ListPool` is encrypted (upgrade if not)
3. Decrypts and validates pool:
   - Must be valid JSON
   - Must have at least `Election.NumberToElect` votes
4. Sets `OnlineVotingInfo.PoolLocked = true`
5. Sets `OnlineVotingInfo.Status = Submitted`
6. Appends to `OnlineVotingInfo.HistoryStatus`:
   - Example: `";Submitted (Email)|2024-01-15T19:45:00Z"`
7. Updates `Person` record:
   - `Person.HasOnlineBallot = true`
   - `Person.VotingMethod = Online` (or `Kiosk` if kiosk login)
   - `Person.RegistrationTime = NOW`
   - `Person.VotingLocationGuid = OnlineLocation`
   - `Person.EnvNum = null`
   - `Person.KioskCode = ""` (if kiosk - marks as used)
   - Appends to `Person.RegistrationLog`
8. Sends confirmation notification (email or SMS)
9. Updates front desk listing via `PeopleModel.UpdateFrontDeskListing()`
10. Logs action

**Business Logic** (Recall - `locked = false`):
1. Validates voting window is still open
2. Sets `OnlineVotingInfo.PoolLocked = false`
3. Sets `OnlineVotingInfo.Status = Draft`
4. Updates `Person` record:
   - `Person.HasOnlineBallot = false`
   - `Person.VotingMethod = null`
   - `Person.VotingLocationGuid = null`
   - `Person.RegistrationTime = null`
   - Appends to `Person.RegistrationLog`: "Cancel Online"
5. Updates front desk listing

**Data Access**:
- Updates `OnlineVotingInfo` table
- Updates `Person` table
- Invalidates `PersonCacher` cache

**Validation**:
- If `Person.VotingMethod` was set by teller (not Online/Kiosk), voter cannot change it
- If `Status == Processed`, ballot cannot be unlocked

**Notifications**:
- `NotificationHelper.SendWhenBallotSubmitted()` sends email or SMS confirmation
- Returns notification type in response

**SignalR**:
- `PeopleModel.UpdateFrontDeskListing()` triggers `FrontDeskHub` update
- Tellers see voter's status change in real-time

**Migration Notes**:
- Prevent double-submission: Check `Status != Processed` before allowing lock
- Use database transactions for Person + OnlineVotingInfo updates
- Consider idempotency key for submit action

---

### 7. GET `/Vote/GetVoterElections`
**Purpose**: Get list of all elections voter is eligible for  
**HTTP Method**: GET  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON with election list and voter status

**Response**:
```json
{
  "list": [
    {
      "id": "election-guid-1",
      "Name": "LSA Election 2024",
      "Convenor": "John Smith",
      "ElectionType": "L",
      "DateOfElection": "2024-01-15T00:00:00Z",
      "EmailFromAddress": "noreply@tallyj.com",
      "EmailFromName": "TallyJ",
      "OnlineWhenOpen": "2024-01-10T08:00:00Z",
      "OnlineWhenClose": "2024-01-15T20:00:00Z",
      "OnlineCloseIsEstimate": false,
      "person": {
        "name": "John Smith",
        "VotingMethod": "O",
        "RegistrationTime": "2024-01-15T19:45:00Z",
        "PoolLocked": true,
        "Status": "Submitted",
        "WhenStatus": "2024-01-15T19:45:00Z"
      }
    }
  ],
  "emailCodes": "123456,789012",  // Saved email codes for quick login
  // "hasLocalId": false  // (commented out)
}
```

**Error Response**:
```json
{
  "Error": "Invalid request"
}
```

**Business Logic**:
1. Gets `VoterId` from session (email, phone, or kiosk code)
2. Finds all `Person` records where:
   - `Person.Email == VoterId` OR
   - `Person.Phone == VoterId` OR
   - `Person.KioskCode == VoterId`
   - AND `Person.CanVote == true`
3. Joins with `Election` table
4. Left joins with `OnlineVotingInfo` to get ballot status
5. Orders by:
   - `OnlineWhenClose DESC` (most recent first)
   - `DateOfElection DESC`
   - `C_RowId` (tie-breaker)
6. Returns list with person-specific status

**Additional Data**:
- `emailCodes`: Retrieved from `OnlineVoter.EmailCodes` (saved codes for quick re-login)

**Data Access**:
- `Person` table (find all elections voter is in)
- `Election` table (election details)
- `OnlineVotingInfo` table (ballot status)
- `OnlineVoter` table (email codes)

**Use Cases**:
- Voter home page shows all elections
- Voter can see which elections they've already voted in
- Voter can resume draft ballots

**Migration Notes**:
- Consider pagination if voter is in 100+ elections (unlikely but possible)

---

### 8. GET `/Vote/GetLoginHistory`
**Purpose**: Get voter's recent login activity (audit log)  
**HTTP Method**: GET  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON with login history

**Response**:
```json
{
  "list": [
    {
      "AsOf": "2024-01-15T19:45:00Z",
      "ElectionName": "LSA Election 2024",
      "Details": "Submitted Ballot"
    },
    {
      "AsOf": "2024-01-15T10:30:00Z",
      "ElectionName": "LSA Election 2024",
      "Details": "Logged in via Email"
    }
  ]
}
```

**Business Logic**:
1. Gets `UniqueId` from session (voter's unique identifier)
2. Queries `C_Log` table where `VoterId == UniqueId`
3. Filters to last 14 days
4. Filters out schema errors (internal logging)
5. Joins with `Election` table for election name
6. Orders by `AsOf DESC` (newest first)
7. Returns top 19 entries

**Data Access**:
- `C_Log` table (activity log)
- `Election` table (election names)

**Migration Notes**:
- UniqueId format: `"V:{email}"` or `"V:{phone}"` (see `security/authentication.md`)

---

### 9. POST `/Vote/SaveEmailCodes`
**Purpose**: Save email verification codes for quick re-login  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON success

**Request Parameters**:
- `emailCodes` (string) - Comma-separated list of 6-digit codes

**Response**:
```json
{
  "saved": true
}
```

**Business Logic**:
- Updates `OnlineVoter.EmailCodes` for current voter
- Allows voter to save codes for quick login without requesting new code each time

**Data Access**:
- Updates `OnlineVoter` table

**Security Consideration**:
- These are NOT active verification codes
- These are user-saved codes for convenience (like bookmarks)
- Actual authentication still requires valid code sent via email/SMS

**Migration Notes**:
- Consider removing this feature (potential security confusion)
- Or clearly label as "saved codes" vs "active codes"

---

### 10. POST `/Vote/SendTestMessage`
**Purpose**: Send test email/SMS to voter (verify notification settings)  
**HTTP Method**: POST  
**Authorization**: `[AllowVoter]`  
**Returns**: JSON with send result

**Response**:
```json
{
  "sent": true,
  "Error": null
}
```

**Error Response**:
```json
{
  "sent": false,
  "Error": "Email not configured"
}
```

**Business Logic**:
- Sends test message to voter's email or phone
- Uses `NotificationHelper.SendVoterTestMessage()`

**Data Access**:
- None (sends message only)

**Migration Notes**:
- Useful for voter to verify contact information is correct

---

## Data Models Referenced

### OnlineVoteHelper
- **Method**: `GetDecryptedListPool(votingInfo, out error)` - Decrypt ballot pool
- **Method**: `SetListPoolEncrypted(votingInfo, pool)` - Encrypt and save ballot pool

### NotificationHelper
- **Method**: `SendWhenBallotSubmitted(person, election, out notificationType, out error)` - Send confirmation

### PeopleModel
- **Method**: `UpdateFrontDeskListing(person, votingMethodRemoved)` - Trigger SignalR update

### PersonCacher
- **Method**: `AllForThisElection` - Get all people in election
- **Method**: `UpdateItemAndSaveCache(person)` - Update cache after person change

### LogHelper
- **Method**: `Add(message, isError)` - Write to C_Log

### EncryptionHelper
- **Static**: `IsEncrypted(text)` - Check if text is encrypted

---

## Authorization Details

### `[AllowVoter]` Attribute
- Requires voter authentication (email/SMS/kiosk code)
- See `security/authentication.md` - **System 3: Voter Authentication**

**Session Requirements**:
- `UserSession.VoterId` - Email, phone, or kiosk code
- `UserSession.VoterIdType` - "E" (Email), "P" (Phone), "K" (Kiosk)

---

## Session Dependencies

- `UserSession.VoterId` - Voter identifier (email/phone/kiosk code)
- `UserSession.VoterIdType` - Voter ID type
- `UserSession.CurrentElectionGuid` - Active election (set by JoinElection)
- `UserSession.VoterInElectionPersonGuid` - Voter's Person record GUID
- `UserSession.VoterLoginSource` - Login method (e.g., "Email", "SMS", "Kiosk")
- `UserSession.UniqueId` - Prefixed voter ID (e.g., "V:voter@example.com")

---

## Integration Points

### Used By:
- **Online Voting Interface** - Voter ballot selection and submission
- **Voter Home Page** - List of elections

### Calls To:
- `OnlineVoteHelper` - Ballot encryption/decryption
- `NotificationHelper` - Email/SMS confirmations
- `PeopleModel` - Update voter status
- `PersonCacher` - Cache management
- `LogHelper` - Audit logging
- `AllVotersHub`, `VoterPersonalHub` - SignalR updates

### External Integrations:
- **SMTP** - Email confirmations
- **Twilio** - SMS confirmations

---

## Ballot Encryption

### Why Encrypt?
- Protect voter privacy
- Prevent ballot tampering
- Comply with electoral secrecy principles

### Encryption Strategy
- `ListPool` (ballot selections) is encrypted using `EncryptionHelper`
- Encryption key is application-wide (not per-voter)
- Decrypted only when:
  - Voter loads ballot (to resume draft)
  - Voter submits ballot (to validate vote count)
  - Teller processes ballot (after voting closes)

### .NET Core Migration
```csharp
// Use ASP.NET Core Data Protection API
public class BallotEncryptionService
{
    private readonly IDataProtector _protector;

    public BallotEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("TallyJ.BallotPool");
    }

    public string Encrypt(string plaintext)
    {
        return _protector.Protect(plaintext);
    }

    public string Decrypt(string ciphertext)
    {
        return _protector.Unprotect(ciphertext);
    }
}
```

---

## Ballot Lifecycle States

### OnlineVotingInfo.Status Values

| Status | Meaning | Actions Available |
|--------|---------|-------------------|
| **New** | No votes selected yet | Select candidates, save draft |
| **Draft** | Votes selected but not submitted | Edit pool, submit, clear |
| **Submitted** | Ballot locked by voter | Recall (if window still open) |
| **Processed** | Ballot counted by teller | None (final state) |

### OnlineVotingInfo.PoolLocked Values

| PoolLocked | Meaning |
|------------|---------|
| `false` | Draft ballot - voter can edit |
| `true` | Submitted ballot - voter cannot edit (unless recalled) |

### Person.VotingMethod Values (Related to Online Voting)

| Value | Meaning | Set By |
|-------|---------|--------|
| `null` | Not registered | N/A |
| `"O"` | Registered online | Voter (LockPool) |
| `"K"` | Registered via kiosk | Voter (LockPool with kiosk login) |
| Other values | Registered by teller | Teller (Front Desk) |

---

## .NET Core Migration Recommendations

### API Design
```csharp
// Voter elections
GET    /api/voter/elections                          // GetVoterElections
GET    /api/voter/login-history                      // GetLoginHistory

// Election ballot management
POST   /api/voter/elections/{electionId}/join        // JoinElection
POST   /api/voter/elections/{electionId}/leave       // LeaveElection
POST   /api/voter/elections/{electionId}/ballot/save // SavePool
POST   /api/voter/elections/{electionId}/ballot/submit // LockPool (locked=true)
POST   /api/voter/elections/{electionId}/ballot/recall // LockPool (locked=false)

// Utilities
POST   /api/voter/test-notification                  // SendTestMessage
POST   /api/voter/save-codes                         // SaveEmailCodes
```

### Authorization
```csharp
[Authorize(Policy = "Voter")]
public class VoterController : ControllerBase
{
    // Endpoints
}
```

### Response DTOs
```csharp
public class JoinElectionResponse
{
    public bool Open { get; set; }
    public bool Closed { get; set; }
    public string VoterName { get; set; }
    public int NumberToElect { get; set; }
    public string OnlineSelectionProcess { get; set; }
    public bool RandomizeVotersList { get; set; }
    public string Registration { get; set; }
    public OnlineVotingInfoDto VotingInfo { get; set; }
    public string PoolDecryptError { get; set; }
}

public class SavePoolResponse
{
    public bool Success { get; set; }
    public string NewStatus { get; set; }
    public string Error { get; set; }
}

public class LockPoolResponse
{
    public bool Success { get; set; }
    public string NotificationType { get; set; }
    public string VotingMethod { get; set; }
    public Guid ElectionGuid { get; set; }
    public DateTime? RegistrationTime { get; set; }
    public DateTime WhenStatus { get; set; }
    public bool PoolLocked { get; set; }
    public string Error { get; set; }
}

public class VoterElectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Convenor { get; set; }
    public string ElectionType { get; set; }
    public DateTime DateOfElection { get; set; }
    public DateTime? OnlineWhenOpen { get; set; }
    public DateTime? OnlineWhenClose { get; set; }
    public bool OnlineCloseIsEstimate { get; set; }
    public VoterStatusDto Person { get; set; }
}

public class VoterStatusDto
{
    public string Name { get; set; }
    public string VotingMethod { get; set; }
    public DateTime? RegistrationTime { get; set; }
    public bool? PoolLocked { get; set; }
    public string Status { get; set; }
    public DateTime? WhenStatus { get; set; }
}
```

### Async/Await
```csharp
[HttpPost("elections/{electionId}/ballot/submit")]
public async Task<IActionResult> SubmitBallot(Guid electionId)
{
    var result = await _ballotService.SubmitBallotAsync(electionId, GetVoterId());
    return Ok(result);
}
```

### Transaction Management
```csharp
// Submit ballot requires updating both OnlineVotingInfo and Person
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Update OnlineVotingInfo
    votingInfo.PoolLocked = true;
    votingInfo.Status = OnlineBallotStatusEnum.Submitted;
    
    // Update Person
    person.HasOnlineBallot = true;
    person.VotingMethod = VotingMethodEnum.Online;
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## Testing Scenarios

1. **JoinElection**:
   - Voting window open → return ballot data
   - Voting window closed → return closed status
   - Voter not in election → error
   - Voter email/phone doesn't match → error

2. **SavePool**:
   - Save draft with 3 votes → Status = Draft
   - Save empty pool → Status = New
   - Save while window closed → error

3. **LockPool (Submit)**:
   - Submit with enough votes → Status = Submitted, Person.VotingMethod = Online
   - Submit with too few votes → error
   - Submit with empty pool → error
   - Submit after window closed → error
   - Submit already processed ballot → error

4. **LockPool (Recall)**:
   - Recall submitted ballot → Status = Draft, Person.VotingMethod = null
   - Recall after window closed → error
   - Recall processed ballot → error

5. **GetVoterElections**:
   - Voter in multiple elections → return all
   - Voter with email + phone → return elections for both identifiers
   - Voter with kiosk code → return elections for that kiosk code

6. **Encryption**:
   - Save pool → encrypted in database
   - Load pool → decrypted for voter
   - Decrypt error → return error message

7. **Notifications**:
   - Submit ballot → email/SMS confirmation sent
   - Notification failure → logged but ballot still submitted

8. **SignalR Updates**:
   - Submit ballot → FrontDeskHub receives update
   - Tellers see voter status change in real-time

---

## API Call Examples

### JavaScript (Current System)
```javascript
// Join election
$.post('/Vote/JoinElection', {
  electionGuid: '...'
}, function(data) {
  if (data.open) {
    var votingInfo = data.votingInfo;
    var pool = JSON.parse(votingInfo.ListPool);
    // Show ballot interface
  } else if (data.closed) {
    alert('Voting is closed');
  }
});

// Save draft ballot
$.post('/Vote/SavePool', {
  pool: JSON.stringify([
    { Id: 123, Name: 'Candidate 1' },
    { Id: 456, Name: 'Candidate 2' }
  ])
}, function(data) {
  if (data.success) {
    console.log('Draft saved');
  }
});

// Submit ballot
$.post('/Vote/LockPool', {
  locked: true
}, function(data) {
  if (data.success) {
    alert('Ballot submitted successfully!');
    if (data.notificationType) {
      alert('Confirmation sent via ' + data.notificationType);
    }
  } else {
    alert(data.Error);
  }
});

// Get voter elections
$.get('/Vote/GetVoterElections', function(data) {
  var elections = data.list;
  // Display election list
});
```

### TypeScript + Axios (Vue 3 Migration)
```typescript
// Voter service
async joinElection(electionId: string): Promise<JoinElectionResponse> {
  const response = await axios.post(`/api/voter/elections/${electionId}/join`);
  return response.data;
}

async saveBallot(electionId: string, pool: Vote[]): Promise<SavePoolResponse> {
  const response = await axios.post(
    `/api/voter/elections/${electionId}/ballot/save`,
    { pool: JSON.stringify(pool) }
  );
  return response.data;
}

async submitBallot(electionId: string): Promise<LockPoolResponse> {
  const response = await axios.post(
    `/api/voter/elections/${electionId}/ballot/submit`
  );
  return response.data;
}

async recallBallot(electionId: string): Promise<LockPoolResponse> {
  const response = await axios.post(
    `/api/voter/elections/${electionId}/ballot/recall`
  );
  return response.data;
}

async getVoterElections(): Promise<VoterElectionDto[]> {
  const response = await axios.get('/api/voter/elections');
  return response.data.list;
}
```

---

## Related Documentation

- **Database**: See `database/entities.md` for OnlineVotingInfo, Person schemas
- **Authentication**: See `security/authentication.md` - System 3: Voter Authentication
- **SignalR**: See `signalr/hubs/AllVotersHub.md`, `signalr/hubs/VoterPersonalHub.md`
- **UI**: See `ui-screenshots-analysis.md` for online voting screenshots
- **Controllers**: See `PublicController.md` for voter authentication (IssueCode, LoginWithCode)
- **Business Logic**: See `business-logic/tally-algorithms.md` for how ballots are processed
