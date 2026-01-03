# TallyJ API Endpoints - Complete Reference

## Overview

This document provides a complete inventory of all API endpoints in the TallyJ ASP.NET Framework v4.8 application across 12 controllers. It includes HTTP methods, routes, authorization requirements, and REST API design recommendations for the .NET Core migration.

**Total Endpoints**: 100+  
**Controllers**: 12 (11 active + 1 disabled)  
**Authentication Systems**: 3 (Admin, Guest Teller, Voter)

---

## Quick Reference by Functional Area

| Area | Controllers | Key Functionality |
|------|-------------|-------------------|
| **Authentication** | AccountController, PublicController | Admin login, guest teller join, voter passwordless auth |
| **Dashboard & Elections** | DashboardController, ElectionsController | Election list, selection, tally status |
| **Front Desk** | BeforeController | Voter registration, roll call display |
| **Ballot Entry** | BallotsController | Ballot entry, vote recording, location tracking |
| **Tallying & Results** | AfterController | Tally execution, reports, tie-breaks |
| **People Management** | PeopleController | Voter/candidate CRUD, voter list for online voting |
| **Online Voting** | VoteController | Voter ballot submission, draft saving |
| **Setup** | SetupController | Election wizard, CSV import, notifications |
| **Public Pages** | PublicController | Home, about, heartbeat, open elections |
| **System Admin** | SysAdminController | Global logs, analytics, monitoring |
| **Legacy** | Manage2Controller | ⚠️ DISABLED - Former voter account management |

---

## Authentication & Authorization Endpoints

### AccountController - Admin Authentication
**Purpose**: Username/password authentication for election organizers

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Account/LogOn` | Display login page | Anonymous |
| POST | `/Account/LogOn` | Authenticate admin with username/password | Anonymous |
| GET | `/Account/LogOff` | Log out admin user | Anonymous |
| GET | `/Account/LogOut` | Alias for LogOff | Anonymous |
| GET | `/Account/Register` | Display registration page | Anonymous |
| POST | `/Account/Register` | Create new admin account | Anonymous |
| GET | `/Account/ChangePassword` | Display change password page | Anonymous |
| POST | `/Account/ChangePassword` | Change current user's password | Session Required |
| GET | `/Account/ChangePasswordSuccess` | Password change success page | Session Required |
| POST | `/Account/IdentitySignout` | Internal OWIN signout helper | Internal |

**Authentication System**: ASP.NET Membership + OWIN Cookie + Claims  
**Session Claims**: `UserName`, `UniqueID` (A:username), `IsKnownTeller`, `IsSysAdmin`  
**Cookie Expiration**: 7 days

### PublicController - Guest Teller & Voter Authentication
**Purpose**: Passwordless authentication for guest tellers and voters

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Public/TellerJoin` | Validate election access code, grant guest teller session | Anonymous |
| POST | `/Public/IssueCode` | Send verification code to voter (email/SMS) | Anonymous |
| POST | `/Public/LoginWithCode` | Verify voter code and authenticate | Anonymous |
| GET | `/Public/OpenElections` | Get list of elections available for guest tellers | Anonymous |

**Guest Teller Auth**: Election access code (passcode) → Claims: `IsGuestTeller=true`, `UniqueID=G:{code}`  
**Voter Auth**: Email/SMS one-time code (6-digit) → Claims: `VoterId`, `VoterIdType` (Email/Phone/Kiosk)

---

## Dashboard & Election Management Endpoints

### DashboardController
**Purpose**: Election dashboard, election list, teller management, location selection

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Dashboard/Index` | Main dashboard landing page | AllowTellersInActiveElection |
| GET | `/Dashboard/ElectionList` | Display list of all elections user can access | ForAuthenticatedTeller |
| POST | `/Dashboard/MoreInfoStatic` | Get static election information | ForAuthenticatedTeller |
| POST | `/Dashboard/MoreInfoLive` | Get live election statistics | ForAuthenticatedTeller |
| POST | `/Dashboard/ReloadElections` | Refresh election list | ForAuthenticatedTeller |
| POST | `/Dashboard/UpdateListingForElection` | Toggle public listing for guest tellers | ForAuthenticatedTeller |
| POST | `/Dashboard/LoadV2Election` | Import election from TallyJ V2 (legacy) | AllowTellersInActiveElection |
| POST | `/Dashboard/ChooseLocation` | Set computer's physical location | AllowTellersInActiveElection |
| POST | `/Dashboard/ChooseTeller` | Assign guest teller | AllowTellersInActiveElection |
| POST | `/Dashboard/DeleteTeller` | Remove guest teller | ForAuthenticatedTeller |

### ElectionsController
**Purpose**: Election CRUD, tally status, cache control, SignalR hub connections

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Elections/Index` | Placeholder route (not used) | AllowTellersInActiveElection |
| POST | `/Elections/SelectElection` | Select election as current active | ForAuthenticatedTeller |
| POST | `/Elections/UpdateElectionStatus` | Update election's tally status | ForAuthenticatedTeller |
| POST | `/Elections/CreateElection` | Create new election | ForAuthenticatedTeller |
| POST | `/Elections/JoinImportHub` | Connect to ImportHub (voter import progress) | ForAuthenticatedTeller |
| POST | `/Elections/JoinAnalyzeHub` | Connect to AnalyzeHub (tally progress) | ForAuthenticatedTeller |
| GET | `/Elections/ExportElection` | Export election data to file | ForAuthenticatedTeller |
| POST | `/Elections/DeleteElection` | Delete election | ForAuthenticatedTeller |
| POST | `/Elections/ClearCache` | Clear cached election data | ForAuthenticatedTeller |

**Tally Status Values**: `NotStarted`, `InProgress`, `Finalized`, `Archived`

---

## Front Desk & Roll Call Endpoints

### BeforeController
**Purpose**: Voter registration, roll call display (real-time via SignalR)

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Before/Index` | Default route (not used) | AllowTellersInActiveElection |
| GET | `/Before/FrontDesk` | Display front desk voter registration page | AllowTellersInActiveElection |
| GET | `/Before/RollCall` | Display roll call public display (projector) | AllowTellersInActiveElection |
| POST | `/Before/PeopleForFrontDesk` | Get list of all voters with registration status | AllowTellersInActiveElection |
| POST | `/Before/VotingMethod` | Register voter's voting method (InPerson/Online/MailIn) | AllowTellersInActiveElection |
| POST | `/Before/SetFlag` | Set custom flags on voters (Excused, Ineligible) | AllowTellersInActiveElection |
| POST | `/Before/JoinFrontDeskHub` | Connect to FrontDeskHub (SignalR) | AllowTellersInActiveElection |
| POST | `/Before/JoinRollCallHub` | Connect to RollCallHub (SignalR) | AllowTellersInActiveElection |

**SignalR Hubs**: `FrontDeskHub` (multi-teller coordination), `RollCallHub` (public display updates)  
**Voting Methods**: `InPerson`, `Online`, `MailIn`

---

## Ballot Entry Endpoints

### BallotsController
**Purpose**: Ballot entry, vote recording, ballot management, location tracking

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Ballots/Index` | Display ballot entry page (Normal or Single) | AllowTellersInActiveElection |
| GET | `/Ballots/Reconcile` | Display ballot reconciliation page | AllowTellersInActiveElection |
| GET | `/Ballots/SortBallots` | Display ballot sorting/filtering page | AllowTellersInActiveElection |
| POST | `/Ballots/BallotsForLocation` | Get list of ballots for specific location | AllowTellersInActiveElection |
| POST | `/Ballots/SaveVote` | Record vote for candidate on current ballot | AllowTellersInActiveElection |
| POST | `/Ballots/DeleteVote` | Remove vote from current ballot | AllowTellersInActiveElection |
| POST | `/Ballots/NeedsReview` | Mark ballot as needing review | AllowTellersInActiveElection |
| POST | `/Ballots/SwitchToBallot` | Change current ballot | AllowTellersInActiveElection |
| POST | `/Ballots/UpdateLocationStatus` | Change location status (Open/Closed) | AllowTellersInActiveElection |
| POST | `/Ballots/UpdateLocationInfo` | Update location notes/comments | AllowTellersInActiveElection |
| POST | `/Ballots/GetBallotList` | Get list of ballots with filters | AllowTellersInActiveElection |
| POST | `/Ballots/GetCurrentBallotInfo` | Get current ballot details | AllowTellersInActiveElection |
| POST | `/Ballots/NewBallot` | Create new ballot | AllowTellersInActiveElection |
| POST | `/Ballots/DeleteBallot` | Delete ballot | AllowTellersInActiveElection |

**SignalR Hub**: `MainHub` (real-time vote entry updates to all tellers)  
**Ballot Types**: `Normal` (9-member LSA), `SingleName` (1-position)  
**Ballot Status**: `Ok`, `NeedsReview`, `Spoiled`

---

## Tallying & Results Endpoints

### AfterController
**Purpose**: Tally execution, results reporting, tie-break management, monitoring

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/After/Index` | Display main "After" page | AllowTellersInActiveElection |
| GET | `/After/Analyze` | Display tally analysis page | ForAuthenticatedTeller |
| GET | `/After/Reports` | Display reports page | AllowTellersInActiveElection |
| GET | `/After/Presenter` | Display results presentation (projector) | AllowTellersInActiveElection |
| GET | `/After/ShowTies` | Display tie-breaker management page | AllowTellersInActiveElection |
| GET | `/After/Monitor` | Display real-time election monitoring dashboard | AllowTellersInActiveElection |
| POST | `/After/RefreshMonitor` | Get updated monitoring data (AJAX polling) | AllowTellersInActiveElection |
| POST | `/After/RunAnalyze` | Execute tally algorithm to count votes | ForAuthenticatedTeller |
| POST | `/After/GetTies` | Retrieve tie information for tie-break group | AllowTellersInActiveElection |
| POST | `/After/SaveTieCounts` | Save tie-break vote counts | ForAuthenticatedTeller |
| POST | `/After/GetReport` | Retrieve final election results (JSON) | AllowTellersInActiveElection |
| POST | `/After/GetReportData` | Retrieve specific report by code | AllowTellersInActiveElection |

**SignalR Hubs**: `AnalyzeHub` (tally progress), `MainHub` (monitoring updates)  
**Report Codes**: `ballots`, `voters`, `locations`, `summary`, `ties`  
**Tally Algorithm**: Documented in `business-logic/tally-algorithms.md`

---

## People/Voter Management Endpoints

### PeopleController
**Purpose**: Voter/candidate retrieval and management

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/People/Index` | Placeholder route | AllowTellersInActiveElection |
| GET | `/People/GetAll` | Retrieve all people with vote statistics (for tellers) | AllowTellersInActiveElection |
| GET | `/People/GetForVoter` | Retrieve eligible candidates (for online voters) | AllowVoter |
| GET | `/People/GetDetail` | Retrieve detailed info for single person | AllowTellersInActiveElection |

**Note**: People CRUD operations (create/update/delete) are in SetupController

---

## Online Voting Endpoints

### VoteController
**Purpose**: Online voter ballot management, submission, draft saving

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Vote/Index` | Display voter home page (election list) | AllowVoter |
| POST | `/Vote/JoinVoterHubs` | Join SignalR hubs (AllVotersHub, VoterPersonalHub) | AllowVoter |
| POST | `/Vote/JoinElection` | Join election and load ballot interface | AllowVoter |
| POST | `/Vote/LeaveElection` | Leave current election | AllowVoter |
| POST | `/Vote/SavePool` | Save draft ballot (pool of votes) | AllowVoter |
| POST | `/Vote/LockPool` | Submit ballot (lock) or recall (unlock) | AllowVoter |
| GET | `/Vote/GetVoterElections` | Get list of all elections voter is eligible for | AllowVoter |
| POST | `/Vote/GetLoginHistory` | Get voter's login history | AllowVoter |

**SignalR Hubs**: `AllVotersHub` (broadcast to all voters), `VoterPersonalHub` (personal notifications)  
**Ballot Status**: `New`, `Draft`, `Submitted`, `Processed`  
**Encryption**: `ListPool` (vote selections) is encrypted in database

---

## Setup & Configuration Endpoints

### SetupController
**Purpose**: Election setup wizard, voter import, ballot import, notification management

#### Setup Views
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Setup/Index` | Display 4-step election setup wizard | ForAuthenticatedTeller |
| GET | `/Setup/People` | Display people management page | AllowTellersInActiveElection |
| GET | `/Setup/Notify` | Display notification management page | ForAuthenticatedTeller |

#### Election Configuration
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Setup/SaveElection` | Save election configuration | ForAuthenticatedTeller |
| POST | `/Setup/SaveNotification` | Save email/SMS notification templates | ForAuthenticatedTeller |
| POST | `/Setup/DetermineRules` | Get voting rules for election type/mode | AllowTellersInActiveElection |

#### CSV Import (Voters)
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Setup/ImportCsv` | Display CSV import page | ForAuthenticatedTeller |
| POST | `/Setup/Upload` | Upload CSV file for import | ForAuthenticatedTeller |
| POST | `/Setup/ReadFields` | Read CSV headers for column mapping | ForAuthenticatedTeller |
| POST | `/Setup/SaveMapping` | Save CSV column-to-Person field mapping | ForAuthenticatedTeller |
| POST | `/Setup/FileCodePage` | Set character encoding for CSV | ForAuthenticatedTeller |
| POST | `/Setup/FileDataRow` | Set first data row (skip headers) | ForAuthenticatedTeller |
| POST | `/Setup/CopyMap` | Copy mapping from one import to another | ForAuthenticatedTeller |
| POST | `/Setup/Import` | Execute CSV import | ForAuthenticatedTeller |
| POST | `/Setup/GetImportHistory` | Get list of previous imports | ForAuthenticatedTeller |

#### Ballot Import
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Setup/ImportBallots` | Upload and import scanned ballot file | ForAuthenticatedTeller |
| POST | `/Setup/GetBallotImportStatus` | Get import progress | ForAuthenticatedTeller |
| POST | `/Setup/JoinBallotImportHub` | Connect to BallotImportHub (SignalR) | ForAuthenticatedTeller |

#### People Management (CRUD)
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Setup/SavePerson` | Create or update person record | AllowTellersInActiveElection |
| POST | `/Setup/DeletePerson` | Delete person record | ForAuthenticatedTeller |
| POST | `/Setup/SetEligibility` | Set person's voting eligibility | AllowTellersInActiveElection |
| POST | `/Setup/MergePeople` | Merge duplicate person records | ForAuthenticatedTeller |

#### Location Management
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Setup/SaveLocation` | Create or update location | ForAuthenticatedTeller |
| POST | `/Setup/DeleteLocation` | Delete location | ForAuthenticatedTeller |
| GET | `/Setup/GetLocations` | Get list of locations | AllowTellersInActiveElection |

#### Notifications
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Setup/SendNotifications` | Send email/SMS to voters | ForAuthenticatedTeller |
| POST | `/Setup/GetNotificationStatus` | Get sending progress | ForAuthenticatedTeller |

#### Kiosk Codes
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Setup/GenerateKioskCodes` | Generate kiosk codes for in-person voting | ForAuthenticatedTeller |
| GET | `/Setup/DownloadKioskCodes` | Download kiosk codes as file | ForAuthenticatedTeller |

---

## Public Pages & Utilities

### PublicController
**Purpose**: Public-facing pages, utilities, webhooks

#### Public Pages
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/Public/Index` | Home/landing page | Anonymous |
| GET | `/Public/About` | About TallyJ page | Anonymous |
| GET | `/Public/Contact` | Contact page | Anonymous |
| GET | `/Public/Privacy` | Privacy policy page | Anonymous |
| GET | `/Public/Learning` | Learning resources page | Anonymous |
| GET | `/Public/Install` | Installation instructions page | Anonymous |
| GET | `/Public/FavIcon` | Serve favicon.ico | Anonymous |

#### Utilities
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Public/Heartbeat` | Client heartbeat/pulse for session management | Anonymous |
| GET | `/Public/Warmup` | Warm up database connection (server start) | Anonymous |
| GET | `/Public/GetTimeOffset` | Calculate time offset (server vs client) | Anonymous |

#### SignalR Hub Joining
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Public/PublicHub` | Join PublicHub and get election list | Anonymous |
| POST | `/Public/VoterCodeHub` | Join VoterCodeHub (voter auth status) | Anonymous |
| POST | `/Public/MainHub` | Join MainHub (election updates) | AllowTellersInActiveElection |

#### Webhooks
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/Public/SmsStatus` | Twilio webhook - SMS delivery status | Anonymous (Twilio) |

---

## System Administration Endpoints

### SysAdminController
**Purpose**: System-wide administration, logs, analytics (SysAdmin only)

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/SysAdmin/Index` | Display system administration dashboard | ForSysAdmin |
| GET | `/SysAdmin/GetMainLog` | Retrieve system activity log (search/filter) | ForSysAdmin |
| GET | `/SysAdmin/GetOnlineVotingLog` | Retrieve online voting analytics across elections | ForSysAdmin |
| GET | `/SysAdmin/GetElectionList` | Retrieve list of all elections with statistics | ForSysAdmin |
| GET | `/SysAdmin/GetUnconnectedVoters` | Find orphaned voter records | ForSysAdmin |

**Authorization**: User must have `IsSysAdmin` claim (set via `AspNetUsers.Comment = "SysAdmin"`)

---

## Legacy/Disabled Controller

### Manage2Controller
**Status**: ⚠️ **ENTIRE CONTROLLER DISABLED** (397 lines commented out)  
**Original Purpose**: Voter account management (password, 2FA, phone, external logins)  
**Why Disabled**: TallyJ uses passwordless authentication for voters (no accounts, no passwords)  
**Migration**: Do NOT port to .NET Core

---

## REST API Design Recommendations for .NET Core

### Base URL Structure
```
https://api.tallyj.com/v1
```

### Resource-Based Routes (RESTful)

#### Authentication
```
POST   /api/auth/admin/login
POST   /api/auth/admin/register
POST   /api/auth/admin/logout
POST   /api/auth/admin/change-password

POST   /api/auth/guest-teller/join
POST   /api/auth/voter/request-code
POST   /api/auth/voter/verify-code
POST   /api/auth/voter/logout
```

#### Elections
```
GET    /api/elections
POST   /api/elections
GET    /api/elections/{electionId}
PUT    /api/elections/{electionId}
DELETE /api/elections/{electionId}
PATCH  /api/elections/{electionId}/status

GET    /api/elections/{electionId}/locations
POST   /api/elections/{electionId}/locations
GET    /api/elections/{electionId}/tellers
POST   /api/elections/{electionId}/tellers
```

#### People (Voters/Candidates)
```
GET    /api/elections/{electionId}/people
POST   /api/elections/{electionId}/people
GET    /api/elections/{electionId}/people/{personId}
PUT    /api/elections/{electionId}/people/{personId}
DELETE /api/elections/{electionId}/people/{personId}

POST   /api/elections/{electionId}/people/import
GET    /api/elections/{electionId}/people/export
```

#### Ballots
```
GET    /api/elections/{electionId}/ballots
POST   /api/elections/{electionId}/ballots
GET    /api/elections/{electionId}/ballots/{ballotId}
PUT    /api/elections/{electionId}/ballots/{ballotId}
DELETE /api/elections/{electionId}/ballots/{ballotId}

POST   /api/elections/{electionId}/ballots/{ballotId}/votes
DELETE /api/elections/{electionId}/ballots/{ballotId}/votes/{voteId}
```

#### Front Desk
```
GET    /api/elections/{electionId}/front-desk/voters
POST   /api/elections/{electionId}/front-desk/voters/{personId}/register
POST   /api/elections/{electionId}/front-desk/voters/{personId}/flag
```

#### Tallying & Results
```
POST   /api/elections/{electionId}/tally/run
GET    /api/elections/{electionId}/results
GET    /api/elections/{electionId}/results/report/{reportType}
GET    /api/elections/{electionId}/ties
POST   /api/elections/{electionId}/ties/{tieId}/resolve
```

#### Online Voting (Voter-Scoped)
```
GET    /api/voter/elections
POST   /api/voter/elections/{electionId}/join
GET    /api/voter/elections/{electionId}/candidates
POST   /api/voter/elections/{electionId}/ballot/save
POST   /api/voter/elections/{electionId}/ballot/submit
POST   /api/voter/elections/{electionId}/ballot/recall
```

#### System Administration
```
GET    /api/admin/logs
GET    /api/admin/analytics/online-voting
GET    /api/admin/elections
GET    /api/admin/unconnected-voters
```

### HTTP Status Codes

| Code | Usage |
|------|-------|
| 200 OK | Successful GET, PUT, PATCH |
| 201 Created | Successful POST (resource created) |
| 204 No Content | Successful DELETE |
| 400 Bad Request | Invalid request data |
| 401 Unauthorized | Not authenticated |
| 403 Forbidden | Authenticated but not authorized |
| 404 Not Found | Resource doesn't exist |
| 409 Conflict | Resource conflict (e.g., duplicate) |
| 422 Unprocessable Entity | Validation errors |
| 500 Internal Server Error | Server error |

### Response Format (JSON)

**Success Response**:
```json
{
  "data": { /* resource or array */ },
  "meta": {
    "timestamp": "2024-01-15T10:30:00Z",
    "requestId": "uuid"
  }
}
```

**Error Response**:
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid election configuration",
    "details": [
      {
        "field": "NumberToElect",
        "message": "Must be between 1 and 50"
      }
    ]
  },
  "meta": {
    "timestamp": "2024-01-15T10:30:00Z",
    "requestId": "uuid"
  }
}
```

### Pagination
```
GET /api/elections/{electionId}/people?page=2&limit=50
```

**Response**:
```json
{
  "data": [ /* people array */ ],
  "meta": {
    "page": 2,
    "limit": 50,
    "total": 250,
    "totalPages": 5
  }
}
```

### Filtering & Sorting
```
GET /api/elections/{electionId}/people?filter[canVote]=true&sort=-name
```

### API Versioning

**Option 1: URL Path** (Recommended)
```
/api/v1/elections
```

**Option 2: Header**
```
Accept: application/vnd.tallyj.v1+json
```

### Authentication

**JWT Bearer Token**:
```
Authorization: Bearer <token>
```

**Token Claims**:
```json
{
  "sub": "user-id",
  "role": "Admin|GuestTeller|Voter",
  "electionId": "guid",
  "exp": 1234567890
}
```

### CORS Configuration
```csharp
services.AddCors(options =>
{
    options.AddPolicy("TallyJCors", builder =>
    {
        builder.WithOrigins("https://tallyj.com", "https://app.tallyj.com")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
```

---

## SignalR Hub Summary

### Hubs for Real-Time Updates

| Hub | Purpose | Groups | Events |
|-----|---------|--------|--------|
| **MainHub** | Election updates, ballot entry coordination | `Election_{guid}` | `voteAdded`, `voteUpdated`, `statusChanged` |
| **FrontDeskHub** | Voter registration updates | `FrontDesk_{electionId}` | `UpdatePerson`, `RefreshPeople` |
| **RollCallHub** | Public roll call display | `RollCall_{electionId}` | `UpdateRollCall`, `RefreshRollCall` |
| **AnalyzeHub** | Tally progress updates | `Analyze_{electionId}` | `AnalyzeProgress`, `AnalyzeComplete` |
| **PublicHub** | Public page election list | `Public` | `ElectionsUpdated` |
| **AllVotersHub** | Broadcast to all voters | `AllVoters_{electionId}` | `ElectionClosed`, `Announcement` |
| **VoterPersonalHub** | Personal voter notifications | `Voter_{personId}` | `BallotConfirmation`, `DeadlineReminder` |
| **VoterCodeHub** | Voter authentication status | `VoterAuth_{key}` | `CodeSent`, `CodeVerified` |
| **ImportHub** | CSV import progress | `Import_{electionId}` | `ImportProgress`, `ImportComplete` |
| **BallotImportHub** | Ballot import progress | `BallotImport_{electionId}` | `ImportProgress`, `ImportComplete` |

### .NET Core SignalR Migration
```csharp
// Hub
public class MainHub : Hub
{
    public async Task JoinElection(string electionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Election_{electionId}");
    }

    public async Task NotifyVoteAdded(string electionId, object vote)
    {
        await Clients.Group($"Election_{electionId}").SendAsync("VoteAdded", vote);
    }
}

// Client (TypeScript)
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/main")
    .build();

await connection.start();
await connection.invoke("JoinElection", electionId);

connection.on("VoteAdded", (vote) => {
    // Update UI
});
```

---

## Authorization Policies for .NET Core

### Policy Definitions
```csharp
services.AddAuthorization(options =>
{
    // Admin with active election
    options.AddPolicy("TellerInActiveElection", policy =>
        policy.RequireClaim("IsKnownTeller", "true")
              .RequireClaim("CurrentElectionGuid"));

    // Admin or Guest Teller with active election
    options.AddPolicy("AllowTellersInActiveElection", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("IsKnownTeller", "true") ||
            context.User.HasClaim("IsGuestTeller", "true")));

    // Authenticated Voter
    options.AddPolicy("Voter", policy =>
        policy.RequireClaim("VoterId"));

    // System Administrator
    options.AddPolicy("SystemAdministrator", policy =>
        policy.RequireClaim("IsSysAdmin", "true"));
});
```

### Usage in Controllers
```csharp
[Authorize(Policy = "TellerInActiveElection")]
public class BallotsController : ControllerBase
{
    [HttpGet("{electionId}/ballots")]
    public async Task<IActionResult> GetBallots(Guid electionId) { }
}
```

---

## API Security Best Practices

### 1. Input Validation
- Use data annotations on DTOs
- Validate all GUIDs and IDs
- Sanitize user input (prevent XSS)

### 2. Authentication
- Use JWT tokens with short expiration (15 min access + 7 day refresh)
- Implement token refresh flow
- Store refresh tokens securely (httpOnly cookie or secure storage)

### 3. Authorization
- Always validate user has access to election/resource
- Check `electionId` in claims matches route parameter
- Implement resource-based authorization for sensitive operations

### 4. Rate Limiting
```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});
```

### 5. HTTPS Only
- Enforce HTTPS in production
- Use HSTS headers
- Set secure flag on cookies

### 6. Audit Logging
- Log all authentication attempts
- Log ballot entry/submission
- Log tally execution
- Log admin configuration changes

---

## Performance Recommendations

### 1. Caching
- Cache `PersonCacher`, `VoteCacher`, `ElectionCacher` using `IMemoryCache`
- Cache key pattern: `election:{electionId}:people`
- Invalidate on updates

### 2. Database Optimization
- Add indexes on foreign keys
- Add indexes on `Election.ElectionGuid`, `Person.Email`, `Person.Phone`
- Use async/await for all database operations
- Use compiled queries for frequently-executed queries

### 3. Pagination
- Paginate `/api/elections/{electionId}/people` if 1000+ voters
- Use cursor-based pagination for logs (`lastRowId`)

### 4. Response Compression
```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
```

### 5. SignalR Optimization
- Use `SendAsync` instead of `Invoke` when client response not needed
- Batch SignalR messages when updating multiple records
- Use MessagePack protocol for smaller payloads

---

## Related Documentation

- **Controller Details**: `api/controllers/*.md` - Individual controller documentation
- **Authentication**: `security/authentication.md` - 3 authentication systems
- **Authorization**: `security/authorization.md` - Authorization rules
- **SignalR**: `signalr/hubs-overview.md` - Hub documentation
- **Database**: `database/entities/*.md` - Entity documentation
- **Tally Algorithm**: `business-logic/tally-algorithms.md` - Vote counting logic

---

## Migration Checklist

- [ ] Map all 100+ endpoints to RESTful routes
- [ ] Implement JWT authentication for all 3 auth systems
- [ ] Define authorization policies
- [ ] Create response DTOs for all endpoints
- [ ] Implement pagination for list endpoints
- [ ] Set up SignalR hubs with .NET Core SignalR
- [ ] Add input validation on all DTOs
- [ ] Implement rate limiting
- [ ] Add comprehensive API documentation (Swagger/OpenAPI)
- [ ] Write integration tests for critical endpoints (auth, ballot submit, tally)
- [ ] Set up API versioning
- [ ] Configure CORS
- [ ] Implement audit logging
- [ ] Optimize database queries
- [ ] Add response caching where appropriate

---

**Documentation Generated**: 2026-01-02  
**Source**: TallyJ ASP.NET Framework v4.8  
**Target**: .NET Core 8+ with Vue 3 frontend
