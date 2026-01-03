# PublicController API Documentation

## Overview
**Purpose**: Public-facing pages, authentication integration, and utility endpoints  
**Base Route**: `/Public`  
**Authorization**: Mixed - mostly `[AllowAnonymous]`, some require teller authentication  
**Authentication System**: Integrates all 3 systems (Admin, Guest Teller, Voter)

## Controller Details

This controller handles:
- **Public pages**: Home, About, Contact, Privacy, Learning, Install
- **Guest teller authentication**: `TellerJoin` - validates election access codes
- **Voter authentication**: `IssueCode`, `LoginWithCode` - passwordless voter login
- **Heartbeat/Pulse**: Client keep-alive for session management
- **SignalR hub joining**: `PublicHub`, `VoterCodeHub`, `MainHub` connections
- **Utilities**: `Warmup`, `GetTimeOffset`, `OpenElections`, `FavIcon`
- **External integrations**: `SmsStatus` webhook (Twilio callback)

**Key Business Logic Classes**:
- `TellerModel` - Guest teller authentication
- `VoterCodeHelper` - Voter passwordless authentication
- `PulseModel` - Heartbeat/session management
- `PublicElectionLister` - List available elections for guest tellers
- `TwilioHelper` - SMS integration
- `PublicHub`, `VoterCodeHub`, `MainHub` - SignalR hubs

---

## Endpoints

### 1. GET `/Public/Index` (or `/Public/`)
**Purpose**: Display home page (landing page for TallyJ)  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: View "Home"

**Response**:
- Renders `Views/Public/Home.cshtml`

**Business Logic**:
- None (static view)

**SignalR**: Client typically joins `PublicHub` after page load

**UI**: See `ui-screenshots-analysis.md` - Landing page screenshot

---

### 2. GET `/Public/About`
**Purpose**: Display "About TallyJ" page  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: View

**Response**:
- Renders `Views/Public/About.cshtml`

---

### 3. GET `/Public/Contact`
**Purpose**: Display contact page  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: View

**Response**:
- Renders `Views/Public/Contact.cshtml`

---

### 4. GET `/Public/Privacy`
**Purpose**: Display privacy policy page  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: View

**Response**:
- Renders `Views/Public/Privacy.cshtml`

---

### 5. GET `/Public/Learning`
**Purpose**: Display learning resources page  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: View

**Response**:
- Renders `Views/Public/Learning.cshtml`

---

### 6. GET `/Public/FavIcon`
**Purpose**: Serve favicon.ico file  
**HTTP Method**: GET  
**Authorization**: `[AllowAnonymous]`  
**Returns**: FilePathResult

**Response**:
- Returns `~/images/favicon.ico` as `image/x-icon`

---

### 7. POST `/Public/Heartbeat`
**Purpose**: Client heartbeat/pulse to maintain session and detect state changes  
**HTTP Method**: POST  
**Authorization**: Anonymous (but uses session)  
**Returns**: JSON with server state updates

**Request Parameters**:
- `info` (PulseInfo object):
  - Client-side state information
  - Last known server state

**Response**: (Determined by `PulseModel.ProcessPulseJson()`)
```json
{
  "needsRefresh": false,
  "electionChanged": false,
  "newMessages": [],
  // ... other state changes
}
```

**Business Logic**:
1. Receives client pulse with current state
2. Compares against server session state
3. Detects changes (election updates, new messages, etc.)
4. Returns instructions to client (refresh, reload, etc.)

**Data Access**:
- Session state
- Database queries via `PulseModel`

**SignalR**: Alternative to polling - SignalR hubs provide real-time updates

**Migration Notes**:
- Legacy pattern - consider replacing with SignalR entirely in .NET Core
- Or use for non-SignalR fallback

---

### 8. GET `/Public/Install`
**Purpose**: Display installation/setup instructions page  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: View

**Response**:
- Renders `Views/Public/Install.cshtml`

---

### 9. GET `/Public/Warmup`
**Purpose**: Warm up database connection (pre-load on server start)  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: JSON null

**Business Logic**:
```csharp
var dummy = UserSession.GetNewDbContext.Election.FirstOrDefault();
return null;
```

**Purpose**: Forces EF to initialize connection pool and compile queries

**Migration Notes**:
- Useful for cold-start scenarios (Azure App Service, serverless)
- .NET Core: Use `IHostedService` for warmup on startup instead

---

### 10. POST `/Public/TellerJoin`
**Purpose**: **Guest Teller Authentication** - validate election access code and grant session  
**HTTP Method**: POST  
**Authorization**: Anonymous  
**Returns**: JSON with authentication result

**Request Parameters**:
- `electionGuid` (Guid) - Election to join
- `pc` (string) - **Passcode** (election access code)
- `oldCompGuid` (Guid?) - Optional previous computer GUID for re-authentication

**Response**:
```json
{
  "success": true,
  "election": {
    "ElectionGuid": "...",
    "Name": "LSA Election 2024",
    // ... election details
  },
  "teller": {
    "IsGuestTeller": true,
    "ComputerCode": "ABC123"
  }
}
```

**Error Response**:
```json
{
  "Error": "Invalid passcode"
}
```

**Business Logic**:
1. Validates `electionGuid` exists
2. Validates `pc` matches `Election.ElectionPasscode`
3. Creates guest teller session:
   - Sets `UserSession.IsGuestTeller = true`
   - Sets `UserSession.CurrentElectionGuid`
   - Generates computer code if `oldCompGuid` not provided
   - Creates claims-based identity
4. Returns election details and teller info

**Data Access**:
- `TellerModel.GrantAccessToGuestTeller(electionGuid, pc, oldCompGuid)`

**Authentication**:
- See `security/authentication.md` - **System 2: Guest Teller Authentication**
- No user account created
- Session-bound authentication
- Claims: `IsGuestTeller=true`, `UniqueID=G:{ComputerCode}`

**SignalR**: After successful join, client joins `MainHub` for election updates

**UI**: See `ui-screenshots-analysis.md` - "Join as Teller" modal

**Migration Notes**:
- Access code stored in `Election.ElectionPasscode` (plain text)
- Consider hashing passcodes in .NET Core (or keep plain for simplicity)
- Session-based authentication → JWT token with `IsGuestTeller` claim

---

### 11. GET `/Public/GetTimeOffset`
**Purpose**: Calculate time offset between client and server (for countdown timers)  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: JSON with time offset in milliseconds

**Request Parameters**:
- `now` (long) - Client-side timestamp (JavaScript `Date.now()` - milliseconds since epoch)

**Response**:
```json
{
  "timeOffset": 1234  // Server ahead by 1234ms
}
```

**Business Logic**:
```csharp
const double fudgeFactor = .5 * 1000; // 500ms network adjustment
var clientTimeNow = new DateTime(1970, 1, 1).AddMilliseconds(now + fudgeFactor);
var serverTime = DateTime.Now;
var diff = (serverTime - clientTimeNow).TotalMilliseconds;
UserSession.TimeOffsetServerAhead = diff.AsInt();
UserSession.TimeOffsetKnown = true;
```

**Purpose**: 
- Enables accurate countdown timers for online voting windows
- Adjusts for network latency (500ms fudge factor)

**Session State**:
- Stores offset in `UserSession.TimeOffsetServerAhead`

**Migration Notes**:
- Use UTC everywhere in .NET Core (avoid `DateTime.Now`)
- Client-side: Use `Date.now()` and adjust with offset for UI display

---

### 12. GET `/Public/OpenElections`
**Purpose**: Retrieve list of elections available for guest tellers to join  
**HTTP Method**: GET  
**Authorization**: Anonymous  
**Returns**: JSON with HTML list

**Response**:
```json
{
  "html": "<div>...election list HTML...</div>"
}
```

**Business Logic**:
- Delegates to `PublicElectionLister.RefreshAndGetListOfAvailableElections()`
- Returns server-rendered HTML (not JSON data)

**Data Access**:
- Queries `Election` table for elections with access codes enabled

**SignalR**: `PublicHub` broadcasts updates when election list changes

**UI**: Displayed on home page for "Join as Teller" modal

**Migration Notes**:
- .NET Core: Return JSON array of elections instead of HTML
- Vue 3 frontend will render the list client-side

---

### 13. POST `/Public/SmsStatus`
**Purpose**: **Twilio webhook** - receive SMS delivery status callbacks  
**HTTP Method**: POST  
**Authorization**: Anonymous (called by Twilio servers)  
**Returns**: void

**Request Parameters** (from Twilio webhook):
- `smsSid` (string) - Twilio message SID
- `messageStatus` (string) - Status: "sent", "delivered", "failed", etc.
- `to` (string) - Recipient phone number
- `errorCode` (int?) - Error code if failed

**Additional Form Keys** (available in `Request.Form`):
- `ErrorCode`, `SmsSid`, `SmsStatus`, `Body`, `MessageStatus`, `To`, `MessagingServiceSid`, `MessageSid`, `AccountSid`, `From`, `ApiVersion`

**Business Logic**:
```csharp
new TwilioHelper().LogSmsStatus(smsSid, messageStatus, to, errorCode);
```

**Data Access**:
- Updates `SmsLog` table with delivery status

**Integration**:
- See `integrations/sms.md` for Twilio configuration
- Webhook URL must be configured in Twilio dashboard

**Security**:
- Consider validating Twilio signature in .NET Core to prevent spoofing

**Migration Notes**:
- .NET Core: Use `[FromForm]` binding for webhook parameters
- Add Twilio signature validation middleware

---

### 14. POST `/Public/PublicHub`
**Purpose**: Join `PublicHub` SignalR hub and get initial election list  
**HTTP Method**: POST  
**Authorization**: Anonymous  
**Returns**: JSON with HTML election list

**Request Parameters**:
- `connId` (string) - SignalR connection ID

**Response**:
```json
{
  "html": "<div>...election list HTML...</div>"
}
```

**Business Logic**:
1. Calls `new PublicHub().Join(connId)` to add connection to hub
2. Calls `OpenElections()` to return current election list

**SignalR**:
- Adds connection to `PublicHub` group
- Client receives broadcasts when elections are added/updated

**Migration Notes**:
- .NET Core SignalR: Join happens automatically on hub connection
- May not need separate endpoint

---

### 15. POST `/Public/VoterCodeHub`
**Purpose**: Join `VoterCodeHub` SignalR hub for voter authentication status updates  
**HTTP Method**: POST  
**Authorization**: Anonymous  
**Returns**: JSON null

**Request Parameters**:
- `connId` (string) - SignalR connection ID
- `key` (string) - Temporary hub key for voter authentication session

**Business Logic**:
- Calls `new VoterCodeHub().Join(connId, key)`
- Adds connection to voter-specific group

**SignalR**:
- Used during voter login flow
- Hub sends updates when verification code is sent/validated
- See `signalr/hubs/VoterCodeHub.md`

**Migration Notes**:
- Hub key is temporary session identifier (not voter ID)
- Expires after authentication completes

---

### 16. POST `/Public/IssueCode`
**Purpose**: **Voter Authentication** - send verification code via email or SMS  
**HTTP Method**: POST  
**Authorization**: Anonymous  
**Returns**: JSON with success/error status

**Request Parameters**:
- `type` (string) - "login" or "register"
- `method` (string) - "email" or "sms"
- `target` (string) - Email address or phone number
- `hubKey` (string) - Voter authentication session key

**Response**:
```json
{
  "success": true,
  "message": "Code sent to your email"
}
```

**Error Response**:
```json
{
  "Error": "Invalid email address"
}
```

**Business Logic**:
1. Validates target (email or phone format)
2. Generates 6-digit verification code
3. Creates/updates `OnlineVoter` record
4. Sends code via email (SMTP) or SMS (Twilio)
5. Broadcasts status via `VoterCodeHub`

**Data Access**:
- `VoterCodeHelper(hubKey).IssueCode(type, method, target)`
- Creates/updates `OnlineVoter` table

**Authentication**:
- See `security/authentication.md` - **System 3: Voter Authentication**
- No password - one-time codes only

**SignalR**:
- Broadcasts to `VoterCodeHub` with status updates
- UI shows "Code sent" message

**UI**: See `ui-screenshots-analysis.md` - "Vote Online" modal

**Migration Notes**:
- Rate limiting required (prevent spam)
- Consider using cloud email service (SendGrid, AWS SES) instead of SMTP
- Store hashed codes in database (not plain text)

---

### 17. POST `/Public/LoginWithCode`
**Purpose**: **Voter Authentication** - validate verification code and grant voter session  
**HTTP Method**: POST  
**Authorization**: Anonymous  
**Returns**: JSON with authentication result

**Request Parameters**:
- `code` (string) - 6-digit verification code
- `hubKey` (string) - Voter authentication session key

**Response**:
```json
{
  "success": true,
  "voterId": "voter@example.com",
  "voterIdType": "E",  // E=Email, P=Phone, K=Kiosk
  "elections": [
    // List of elections this voter is in
  ]
}
```

**Error Response**:
```json
{
  "Error": "Invalid code"
}
```

**Business Logic**:
1. Validates verification code against `OnlineVoter` record
2. Checks code expiration (typically 15 minutes)
3. Creates voter session:
   - Sets `UserSession.VoterId` (email or phone)
   - Sets `UserSession.VoterIdType` (Email/Phone/Kiosk)
   - Creates claims-based identity
4. Returns list of elections voter is in

**Data Access**:
- `VoterCodeHelper(hubKey).LoginWithCode(code)`
- Queries `OnlineVoter` and `Person` tables

**Authentication**:
- See `security/authentication.md` - **System 3: Voter Authentication**
- Passwordless authentication
- Session-based with claims: `VoterId`, `VoterIdType`, `IsVoter=true`

**SignalR**:
- Broadcasts success via `VoterCodeHub`

**Migration Notes**:
- .NET Core: Issue JWT token with voter claims
- Token expiration should match online voting window
- Consider refresh tokens for long voting windows

---

### 18. POST `/Public/JoinMainHub`
**Purpose**: Join `MainHub` SignalR hub for election status updates  
**HTTP Method**: POST  
**Authorization**: None (manual validation)  
**Returns**: void

**Request Parameters**:
- `connId` (string) - SignalR connection ID
- `electionGuid` (string) - Election GUID to join

**Business Logic**:
```csharp
if (UserSession.CurrentElectionGuid == Guid.Empty) return;
var guid = electionGuid.AsGuid();
if (guid != UserSession.CurrentElectionGuid) return;
new MainHub().Join(connId);
```

**Validation**:
- Checks user has current election in session
- Validates requested election matches session election
- Silently ignores invalid requests (no error response)

**SignalR**:
- Adds connection to election-specific group in `MainHub`
- Receives real-time updates for election status changes

**Migration Notes**:
- .NET Core SignalR: Use hub methods with `[Authorize]` instead of controller endpoints

---

### 19. POST `/Public/JoinMainHubAll`
**Purpose**: Join `MainHub` for multiple elections (known teller multi-election monitoring)  
**HTTP Method**: POST  
**Authorization**: None (manual validation)  
**Returns**: void

**Request Parameters**:
- `connId` (string) - SignalR connection ID
- `electionGuidList` (string) - Comma-separated election GUIDs

**Business Logic**:
```csharp
if (!UserSession.IsKnownTeller) return;
new MainHub().JoinAll(connId, electionGuidList);
```

**Validation**:
- Only known tellers (authenticated admins) can join multiple elections
- Guest tellers cannot use this endpoint

**SignalR**:
- Adds connection to multiple election groups
- Used for admin monitoring dashboard

**Migration Notes**:
- Known tellers = authenticated admins (not guest tellers)
- See `security/authentication.md` for distinction

---

## Data Models Referenced

### TellerModel
- **Method**: `GrantAccessToGuestTeller(electionGuid, passcode, oldCompGuid)` - Validates access code and creates session

### VoterCodeHelper
- **Constructor**: `VoterCodeHelper(string hubKey)` - Hub key for SignalR updates
- **Method**: `IssueCode(type, method, target)` - Send verification code
- **Method**: `LoginWithCode(code)` - Validate code and authenticate voter
- **Method**: `GenerateKioskCode(personId, out error)` - Generate kiosk code (called from SetupController)

### PulseModel
- **Method**: `ProcessPulseJson(PulseInfo)` - Process heartbeat and return state changes

### PublicElectionLister
- **Method**: `RefreshAndGetListOfAvailableElections()` - Get HTML list of open elections

### TwilioHelper
- **Method**: `LogSmsStatus(smsSid, messageStatus, to, errorCode)` - Log SMS delivery status

### Hubs
- `PublicHub` - Home page updates (election list changes)
- `VoterCodeHub` - Voter authentication status updates
- `MainHub` - Election status updates

---

## Authorization Details

### Anonymous Endpoints
Most endpoints are anonymous or have manual validation:
- Public pages (Index, About, Contact, etc.)
- `TellerJoin` - Creates authentication session
- `IssueCode`, `LoginWithCode` - Creates voter session
- `SmsStatus` - Twilio webhook

### Manual Validation
- `JoinMainHub` - Checks session state
- `JoinMainHubAll` - Checks `IsKnownTeller`

---

## Session Dependencies

- `UserSession.CurrentElectionGuid` - Current election
- `UserSession.VoterId` - Voter email/phone/kiosk code
- `UserSession.VoterIdType` - Email/Phone/Kiosk
- `UserSession.IsKnownTeller` - Authenticated admin flag
- `UserSession.IsGuestTeller` - Guest teller flag
- `UserSession.TimeOffsetServerAhead` - Client/server time difference

---

## Integration Points

### Used By:
- **Home Page** - Public election list, teller join, voter login
- **Vote Online Modal** - Voter authentication flow
- **Join as Teller Modal** - Guest teller authentication
- **All Pages** - Heartbeat/pulse for session keep-alive

### Calls To:
- `TellerModel` - Guest teller authentication
- `VoterCodeHelper` - Voter authentication
- `TwilioHelper` - SMS integration
- `PublicElectionLister` - Election list
- `PublicHub`, `VoterCodeHub`, `MainHub` - SignalR hubs

### External Integrations:
- **Twilio** - SMS delivery status webhook
- **SMTP** - Email verification codes
- **SignalR** - Real-time updates

---

## .NET Core Migration Recommendations

### API Design
```csharp
// Public pages - return Vue 3 SPA shell
GET    /                              // Home (Vue 3 app)
GET    /about                         // About page
GET    /contact                       // Contact page

// Authentication endpoints
POST   /api/auth/teller/join          // TellerJoin
POST   /api/auth/voter/issue-code     // IssueCode
POST   /api/auth/voter/login          // LoginWithCode

// Utility endpoints
GET    /api/elections/open            // OpenElections (JSON, not HTML)
GET    /api/util/time-offset          // GetTimeOffset
GET    /api/util/warmup               // Warmup

// Webhooks
POST   /api/webhooks/twilio/sms       // SmsStatus
```

### Authorization
```csharp
[AllowAnonymous]
public async Task<IActionResult> TellerJoin([FromBody] TellerJoinRequest request) { }

[AllowAnonymous]
public async Task<IActionResult> IssueCode([FromBody] IssueCodeRequest request) { }
```

### SignalR Hub Joining
```csharp
// .NET Core SignalR - join in hub methods, not controller
public class PublicHub : Hub
{
    public async Task JoinElectionList()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "PublicElectionList");
    }
}
```

### Response DTOs
```csharp
public class TellerJoinResponse
{
    public bool Success { get; set; }
    public string Token { get; set; }  // JWT token
    public ElectionDto Election { get; set; }
    public string Error { get; set; }
}

public class VoterLoginResponse
{
    public bool Success { get; set; }
    public string Token { get; set; }  // JWT token
    public List<ElectionDto> Elections { get; set; }
    public string Error { get; set; }
}
```

### Remove Heartbeat/Pulse
- Replace with SignalR for real-time updates
- Or use periodic polling with shorter intervals (10-30 seconds)

---

## Testing Scenarios

1. **Guest Teller Authentication**:
   - Valid election GUID + correct passcode → success
   - Invalid passcode → error
   - Expired election → error (if applicable)

2. **Voter Authentication**:
   - Issue email code → code sent, stored in DB
   - Issue SMS code → Twilio called, code sent
   - Login with valid code → success, session created
   - Login with expired code → error
   - Login with invalid code → error

3. **SignalR Hub Joining**:
   - PublicHub → receives election list updates
   - VoterCodeHub → receives code sent/validated updates
   - MainHub → receives election status updates

4. **Twilio Webhook**:
   - Delivered status → logged in DB
   - Failed status → logged with error code

5. **Time Offset**:
   - Client 1 second behind server → offset = +1000ms
   - Client 1 second ahead of server → offset = -1000ms

---

## API Call Examples

### JavaScript (Current System)
```javascript
// Join as teller
$.post('/Public/TellerJoin', {
  electionGuid: '...',
  pc: 'ABC123',
  oldCompGuid: null
}, function(data) {
  if (data.success) {
    // Redirect to dashboard
  } else {
    alert(data.Error);
  }
});

// Voter - issue code
$.post('/Public/IssueCode', {
  type: 'login',
  method: 'email',
  target: 'voter@example.com',
  hubKey: '...'
}, function(data) {
  if (data.success) {
    alert('Code sent!');
  }
});

// Voter - login with code
$.post('/Public/LoginWithCode', {
  code: '123456',
  hubKey: '...'
}, function(data) {
  if (data.success) {
    // Redirect to voter home
  } else {
    alert(data.Error);
  }
});

// Get time offset
$.get('/Public/GetTimeOffset', { now: Date.now() }, function(data) {
  var offset = data.timeOffset;
  // Adjust countdown timers
});
```

### TypeScript + Axios (Vue 3 Migration)
```typescript
// Auth service
async joinAsTeller(electionGuid: string, passcode: string): Promise<TellerJoinResponse> {
  const response = await axios.post('/api/auth/teller/join', {
    electionGuid,
    passcode
  });
  if (response.data.token) {
    // Store JWT token
    localStorage.setItem('token', response.data.token);
  }
  return response.data;
}

async issueVoterCode(method: 'email' | 'sms', target: string): Promise<void> {
  await axios.post('/api/auth/voter/issue-code', {
    method,
    target
  });
}

async loginWithCode(code: string): Promise<VoterLoginResponse> {
  const response = await axios.post('/api/auth/voter/login', { code });
  if (response.data.token) {
    localStorage.setItem('voterToken', response.data.token);
  }
  return response.data;
}

async getTimeOffset(): Promise<number> {
  const response = await axios.get('/api/util/time-offset', {
    params: { now: Date.now() }
  });
  return response.data.timeOffset;
}
```

---

## Related Documentation

- **Authentication**: See `security/authentication.md` for all 3 authentication systems
- **SignalR**: See `signalr/hubs-overview.md` for PublicHub, VoterCodeHub, MainHub
- **Integrations**: See `integrations/sms.md` for Twilio, `integrations/email.md` for SMTP
- **UI**: See `ui-screenshots-analysis.md` for home page, login modals
- **Controllers**: See `AccountController.md` for admin authentication, `VoteController.md` for voter flows
