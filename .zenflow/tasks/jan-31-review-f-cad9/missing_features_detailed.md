# Detailed Requirements for Missing Features

**Generated:** 2026-01-31  
**Purpose:** Page-by-page detailed specifications for features missing in v4

---

## HIGH PRIORITY FEATURES

### 1. Location Management

**Database Support:** ✅ Location entity exists  
**Backend API:** ❌ No LocationsController  
**Frontend:** ❌ No location pages

#### Pages Required

##### 1.1 LocationsListPage.vue
**Route:** `/elections/:electionId/locations`  
**Purpose:** Manage voting locations for an election

**Components:**
- Header with "Add Location" button
- Table with columns:
  - Name
  - Contact Info
  - Tally Status
  - Ballots Collected
  - Actions (Edit, Delete)
- Search/filter by name
- Sort by name, ballots collected

**API Calls:**
- `GET /api/elections/{electionId}/locations` - List all locations
- `DELETE /api/elections/{electionId}/locations/{locationId}` - Delete location

##### 1.2 LocationDetailPage.vue / LocationFormDialog.vue
**Purpose:** Create/edit location

**Form Fields:**
- Name (required, max 50 chars)
- Contact Info (optional, max 250 chars)
- Longitude (optional, GPS coordinate)
- Latitude (optional, GPS coordinate)
- Sort Order (optional, number)

**API Calls:**
- `POST /api/elections/{electionId}/locations` - Create
- `PUT /api/elections/{electionId}/locations/{locationId}` - Update
- `GET /api/elections/{electionId}/locations/{locationId}` - Get details

**Validation:**
- Name required
- Unique name per election
- Valid GPS coordinates if provided

##### 1.3 Computer Registration (Part of Location)
**Purpose:** Register computers for ballot entry at a location

**Fields:**
- Computer Internal Code (auto-generated or manual)
- Browser Info (auto-detected)
- IP Address (auto-detected)
- Last Activity (timestamp)

**API Calls:**
- `POST /api/elections/{electionId}/locations/{locationId}/computers` - Register computer
- `GET /api/elections/{electionId}/locations/{locationId}/computers` - List computers

#### Backend Requirements

##### LocationsController.cs
```csharp
[Route("api/elections/{electionId}/locations")]
public class LocationsController : ControllerBase
{
    GET    ""                    // List locations
    POST   ""                    // Create location
    GET    "{locationId}"        // Get location
    PUT    "{locationId}"        // Update location
    DELETE "{locationId}"        // Delete location
    POST   "{locationId}/computers"  // Register computer
    GET    "{locationId}/computers"  // List computers
}
```

##### DTOs Required
- `LocationDto`
- `CreateLocationDto`
- `UpdateLocationDto`
- `ComputerDto`
- `RegisterComputerDto`

##### Services
- `LocationService` / `ILocationService`
  - CRUD operations
  - Computer registration
  - Ballot collection tracking

---

### 2. Teller Management

**Database Support:** ✅ Teller entity exists  
**Backend API:** ❌ No TellersController  
**Frontend:** ❌ No teller pages

#### Pages Required

##### 2.1 TellersListPage.vue
**Route:** `/elections/:electionId/tellers`  
**Purpose:** Manage tellers assigned to election

**Components:**
- Header with "Add Teller" button
- Table with columns:
  - Name
  - Role (Head Teller / Teller)
  - Computer Code
  - Actions (Edit, Delete)
- Filter by role

**API Calls:**
- `GET /api/elections/{electionId}/tellers` - List tellers
- `DELETE /api/elections/{electionId}/tellers/{tellerId}` - Remove teller

##### 2.2 TellerFormDialog.vue
**Purpose:** Add/edit teller assignment

**Form Fields:**
- Name (required, max 50 chars)
- Is Head Teller (checkbox)
- Computer Code (optional, 2 chars)

**API Calls:**
- `POST /api/elections/{electionId}/tellers` - Add teller
- `PUT /api/elections/{electionId}/tellers/{tellerId}` - Update teller

**Validation:**
- Name required
- Unique name per election
- Computer code format (2 uppercase letters)

##### 2.3 Teller Access Code Login (Enhancement to LoginPage.vue)
**Purpose:** Allow tellers to join election using access code

**New Login Flow:**
1. Select "Join as Teller" option
2. Enter election access code (Election.ElectionPasscode)
3. System verifies code and loads election
4. Teller enters their name
5. System creates/finds Teller record
6. Session established with teller permissions

**API Calls:**
- `POST /api/auth/teller-login` - New endpoint
  - Input: `{ electionPasscode: string, tellerName: string }`
  - Output: JWT token + election info + teller info

#### Backend Requirements

##### TellersController.cs
```csharp
[Route("api/elections/{electionId}/tellers")]
public class TellersController : ControllerBase
{
    GET    ""            // List tellers
    POST   ""            // Add teller
    PUT    "{tellerId}"  // Update teller
    DELETE "{tellerId}"  // Remove teller
}
```

##### AuthController Enhancement
```csharp
POST /api/auth/teller-login
{
    electionPasscode: string
    tellerName: string
} -> TellerLoginResponse
```

##### DTOs Required
- `TellerDto`
- `AddTellerDto`
- `UpdateTellerDto`
- `TellerLoginDto`
- `TellerLoginResponse`

##### Services
- `TellerService` / `ITellerService`
  - CRUD operations
  - Access code verification
  - Session management

---

### 3. Advanced Election Configuration

**Database Support:** ✅ All fields in Election entity  
**Backend API:** ✅ ElectionsController exists  
**Frontend:** 🟨 CreateElectionPage only exposes 10/40+ fields

#### Enhancement Required: ElectionFormPage.vue (Expanded)

**Current Fields (10):**
- Name
- Date
- Type
- Mode
- Number to Elect
- Number Extra
- Convenor
- Show Full Report
- List for Public
- Show as Test

**MISSING Fields (30+):**

##### Basic Settings Section
- Election Passcode (required for teller access)
- Linked Election (for tie-breaks)
- Can Vote (default eligibility)
- Can Receive Votes (default candidacy)

##### Online Voting Section
- Enable Online Voting (checkbox)
- When Opens (datetime)
- When Closes (datetime)
- Close Time is Estimate (checkbox)
- Selection Process (dropdown)
- Announced Date (datetime)

##### Communication Section
- Email From Address
- Email From Name
- Email Subject
- Email Text (template)
- SMS Text (template)

##### Voting Methods Section
- Use Call-In Button (checkbox)
- Custom Methods (comma-separated)
- Voting Methods (multi-select checkboxes)
  - In Person
  - Online
  - Mailed In
  - Called In
  - Imported
  - (Custom methods)

##### Display Options Section
- Hide Pre-Ballot Pages (checkbox)
- Mask Voting Method (checkbox)
- Show Full Report (existing)
- List for Public (existing)
- Show as Test (existing)

##### Advanced Section
- Flags (JSON, advanced users only)

#### Form Organization

**Proposed Tab Structure:**
1. **Basic Information** - Name, Date, Type, Mode, Convenor
2. **Election Rules** - Number to Elect, Number Extra, Can Vote, Can Receive
3. **Voting Methods** - In-person, Online, Custom methods
4. **Online Voting** - Online voting configuration
5. **Communication** - Email/SMS templates
6. **Display & Privacy** - Show/hide options, public listing
7. **Advanced** - Passcode, Linked elections, Flags

**Validation:**
- If online voting enabled: Require open/close times, email template
- If custom methods: Validate format
- Passcode: Strong password requirements

---

### 4. Front Desk Registration

**Database Support:** ✅ Person.RegistrationTime, Teller1/Teller2 fields  
**Backend API:** 🟨 FrontDeskHub exists but no controller  
**Frontend:** ❌ No front desk pages

#### Pages Required

##### 4.1 FrontDeskPage.vue
**Route:** `/elections/:electionId/frontdesk`  
**Purpose:** Real-time voter check-in and registration

**Layout:**
```
+------------------+--------------------+
| Voter Search     | Voter Details      |
| [Search box]     | Name: John Doe     |
| Results:         | Status: Eligible   |
| - John Doe       | [Check In Button]  |
| - Jane Smith     |                    |
+------------------+--------------------+
| Roll Call                            |
| Total Eligible: 150                  |
| Checked In: 47                       |
| Progress: [████████------] 31%       |
| [View Full Roll Call]                |
+--------------------------------------+
```

**Features:**
1. **Search Voters**
   - Search by name, Bahai ID
   - Display eligible voters
   - Show check-in status

2. **Check-In Process**
   - Select voter
   - Confirm identity
   - Assign envelope number (auto-increment)
   - Record voting method
   - Record teller name(s)
   - Mark registration time
   - Print envelope label (optional)

3. **Real-time Updates**
   - SignalR updates across all front desk stations
   - Live voter count
   - Recent check-ins feed

4. **Roll Call Display**
   - List all voters with check-in status
   - Filter by status (Checked In, Not Yet, Ineligible)
   - Export to PDF/Excel

**API Calls:**
- `GET /api/elections/{electionId}/frontdesk/eligible-voters` - List eligible voters
- `POST /api/elections/{electionId}/frontdesk/checkin` - Check in voter
  ```json
  {
    "personGuid": "...",
    "votingMethod": "InPerson",
    "tellerName": "Alice Smith",
    "votingLocationGuid": "..."
  }
  ```
- `GET /api/elections/{electionId}/frontdesk/rollcall` - Get roll call data
- `GET /api/elections/{electionId}/frontdesk/stats` - Get statistics

**SignalR Events (FrontDeskHub):**
- `PersonCheckedIn` - Broadcast when voter checks in
- `VoterCountUpdated` - Update statistics

#### Backend Requirements

##### FrontDeskController.cs (New)
```csharp
[Route("api/elections/{electionId}/frontdesk")]
public class FrontDeskController : ControllerBase
{
    GET  "eligible-voters"   // List eligible voters
    POST "checkin"           // Check in voter
    GET  "rollcall"          // Roll call data
    GET  "stats"             // Statistics
}
```

##### DTOs Required
- `FrontDeskVoterDto`
- `CheckInVoterDto`
- `RollCallDto`
- `FrontDeskStatsDto`

##### FrontDeskHub Enhancement
- Add `JoinFrontDeskSession(electionGuid)`
- Add `LeaveFrontDeskSession(electionGuid)`
- Broadcast `PersonCheckedIn` event
- Broadcast `VoterCountUpdated` event

---

### 5. Online Voting Portal

**Database Support:** ✅ OnlineVoter, OnlineVotingInfo entities  
**Backend API:** ❌ No OnlineVotingController  
**Frontend:** ❌ No online voting pages

#### Pages Required

##### 5.1 Online Voter Landing Page (Public)
**Route:** `/vote` (public route, no auth)  
**Purpose:** Voter authentication entry point

**Layout:**
```
+----------------------------------------+
| TallyJ Online Voting                   |
+----------------------------------------+
| How would you like to log in?          |
|                                        |
| [📧 Using your email]                  |
| [📱 Using your phone]                  |
| [🔑 Using a code given to you]         |
+----------------------------------------+
```

**Three Login Methods:**

1. **Email Login:**
   - Enter email address
   - System sends verification code via email
   - Enter code to authenticate
   
2. **Phone Login:**
   - Enter phone number
   - Choose SMS or Voice
   - System sends verification code
   - Enter code to authenticate

3. **Direct Code:**
   - Enter pre-provided voting code
   - Authenticate immediately

**API Calls:**
- `POST /api/online-voting/request-code`
  ```json
  {
    "voterId": "email@example.com" | "+12025550103",
    "voterIdType": "E" | "P",
    "deliveryMethod": "email" | "sms" | "voice"
  }
  ```
- `POST /api/online-voting/verify-code`
  ```json
  {
    "voterId": "...",
    "verifyCode": "ABC123"
  }
  ```

##### 5.2 Online Ballot Submission Page
**Route:** `/vote/ballot/:electionId` (authenticated voters only)  
**Purpose:** Submit online ballot

**Layout:**
```
+----------------------------------------+
| [Election Name]                        |
| Date: [Date]                           |
| Convenor: [Name]                       |
+----------------------------------------+
| Vote for 9 persons:                    |
|                                        |
| 1. [Autocomplete: Search by name]      |
| 2. [Autocomplete: Search by name]      |
| 3. [Autocomplete: Search by name]      |
| ...                                    |
| 9. [Autocomplete: Search by name]      |
+----------------------------------------+
| [Submit Ballot] [Cancel]               |
+----------------------------------------+
| ⚠️ You can only vote once!             |
+----------------------------------------+
```

**Features:**
1. Display election information
2. Autocomplete from eligible candidates
3. Validate ballot (no duplicates, correct count)
4. Confirm submission
5. Prevent multiple submissions (check HasOnlineBallot)
6. Thank you / confirmation page

**API Calls:**
- `GET /api/online-voting/elections/{electionId}` - Get election details
- `GET /api/online-voting/elections/{electionId}/candidates` - List eligible candidates
- `POST /api/online-voting/elections/{electionId}/submit-ballot`
  ```json
  {
    "votes": [
      { "personGuid": "...", "rank": 1 },
      { "personGuid": "...", "rank": 2 }
    ]
  }
  ```
- `GET /api/online-voting/my-votes/{electionId}` - Check if already voted

**SignalR Events (PublicHub):**
- `OnlineVoteSubmitted` - Update live voter count

##### 5.3 Online Voting Admin Configuration (Enhancement to ElectionFormPage)
**Purpose:** Configure online voting settings

See "Advanced Election Configuration" section above.

#### Backend Requirements

##### OnlineVotingController.cs (New)
```csharp
[Route("api/online-voting")]
public class OnlineVotingController : ControllerBase
{
    POST "request-code"                    // Request verification code
    POST "verify-code"                     // Verify code and authenticate
    GET  "elections/{electionId}"          // Get election info (public)
    GET  "elections/{electionId}/candidates" // List candidates
    POST "elections/{electionId}/submit-ballot" // Submit ballot
    GET  "my-votes/{electionId}"           // Check voting status
}
```

##### DTOs Required
- `OnlineVoterLoginDto`
- `RequestCodeDto`
- `VerifyCodeDto`
- `OnlineElectionInfoDto`
- `OnlineCandidateDto`
- `SubmitOnlineBallotDto`
- `OnlineVoteStatusDto`

##### Services
- `OnlineVotingService` / `IOnlineVotingService`
  - Code generation and validation
  - Email/SMS delivery (integration with external service)
  - Ballot submission
  - Duplicate vote prevention

##### External Services Integration
- **Email Service:** SendGrid, Amazon SES, or SMTP
- **SMS Service:** Twilio for SMS and Voice calls

---

### 6. Ballot Import

**Database Support:** ✅ ImportFile entity exists  
**Backend API:** 🟨 ImportController exists (for people)  
**Frontend:** ❌ No ballot import UI

#### Pages Required

##### 6.1 BallotImportPage.vue
**Route:** `/elections/:electionId/import/ballots`  
**Purpose:** Bulk import ballots from CSV/Excel

**Layout:**
```
+----------------------------------------+
| Import Ballots                         |
+----------------------------------------+
| Step 1: Upload File                    |
| [📁 Choose File] ballots.csv           |
| [Upload]                               |
+----------------------------------------+
| Step 2: Map Fields                     |
| CSV Column -> TallyJ Field             |
| Column A    -> Ballot Number           |
| Column B    -> Status Code             |
| Column C    -> Computer Code           |
| Column D    -> Teller Name             |
| Columns E-M -> Votes (9 positions)     |
+----------------------------------------+
| Step 3: Preview & Confirm              |
| Found 150 ballots                      |
| [Show Preview]                         |
| [Import Ballots]                       |
+----------------------------------------+
| Step 4: Import Progress                |
| [████████------] 53/150 (35%)          |
| Errors: 2                              |
| [View Error Report]                    |
+----------------------------------------+
```

**Features:**
1. CSV/Excel file upload
2. Field mapping interface
3. Preview data
4. Real-time import progress (SignalR)
5. Error handling and reporting
6. Rollback on critical errors

**CSV Format:**
```csv
BallotNum,Status,Computer,Teller,Vote1,Vote2,Vote3,...Vote9
1,OK,A,John Doe,Alice Smith,Bob Jones,...
2,OK,A,John Doe,Charlie Brown,Diana Prince,...
```

**API Calls:**
- `POST /api/elections/{electionId}/import/ballots/upload` - Upload file
- `POST /api/elections/{electionId}/import/ballots/preview` - Preview import
- `POST /api/elections/{electionId}/import/ballots/execute` - Execute import
- `GET /api/elections/{electionId}/import/ballots/{importId}/status` - Get status

**SignalR Events (BallotImportHub):**
- `ImportProgress` - Update progress bar
- `ImportComplete` - Notify completion
- `ImportError` - Report errors

#### Backend Requirements

##### Enhancement to ImportController.cs
```csharp
[Route("api/elections/{electionId}/import/ballots")]
public partial class ImportController
{
    POST "upload"           // Upload CSV/Excel
    POST "preview"          // Preview import
    POST "execute"          // Execute import
    GET  "{importId}/status" // Get import status
}
```

##### DTOs Required
- `BallotImportFileDto`
- `BallotImportPreviewDto`
- `BallotImportExecuteDto`
- `BallotImportStatusDto`
- `BallotImportErrorDto`

##### Services
- Enhancement to `ImportService`
  - Parse ballot CSV
  - Map fields
  - Validate ballots
  - Create Ballot and Vote entities
  - Error reporting

---

### 7. Audit Log UI

**Database Support:** ✅ Log entity exists  
**Backend API:** ❌ No AuditLogsController  
**Frontend:** ❌ No audit log pages

#### Pages Required

##### 7.1 AuditLogsPage.vue
**Route:** `/elections/:electionId/audit-logs` or `/admin/audit-logs`  
**Purpose:** View system activity and audit trail

**Layout:**
```
+----------------------------------------+
| Audit Logs                             |
+----------------------------------------+
| Filters:                               |
| User: [Dropdown: All Users]            |
| Action: [Dropdown: All Actions]        |
| Entity: [Dropdown: All Entities]       |
| Date Range: [Start] to [End]           |
| [Apply Filters] [Clear]                |
+----------------------------------------+
| Timestamp | User | Action | Entity | Details |
| 2026-01-31 14:23 | admin | UPDATE | Election | Changed name |
| 2026-01-31 14:22 | admin | CREATE | Person | Added John Doe |
| 2026-01-31 14:20 | teller1 | CREATE | Ballot | Entered ballot #47 |
+----------------------------------------+
| [Export to CSV]                        |
+----------------------------------------+
```

**Features:**
1. Filter by user, action type, entity type, date range
2. View detailed changes (JSON diff)
3. Export to CSV
4. Pagination for large datasets

**API Calls:**
- `GET /api/elections/{electionId}/audit-logs` - List logs
  - Query params: `userId`, `action`, `entityType`, `startDate`, `endDate`, `page`, `pageSize`
- `GET /api/audit-logs/{logId}` - Get log details
- `GET /api/audit-logs/export` - Export to CSV

#### Backend Requirements

##### AuditLogsController.cs (New)
```csharp
[Route("api")]
public class AuditLogsController : ControllerBase
{
    GET "elections/{electionId}/audit-logs" // Election-specific logs
    GET "audit-logs"                        // System-wide logs (admin only)
    GET "audit-logs/{logId}"                // Log details
    GET "audit-logs/export"                 // Export CSV
}
```

##### DTOs Required
- `AuditLogDto`
- `AuditLogFilterDto`
- `AuditLogDetailsDto`

##### Services
- `AuditLogService` / `IAuditLogService`
  - Query logs with filters
  - Format change details
  - Export functionality

##### Middleware
- **AuditLoggingMiddleware** - Automatically log all API requests
  - Log user, action, entity, timestamp, IP address
  - Capture request/response for sensitive operations
  - Store in Log entity

---

### 8. Public Display Mode

**Database Support:** ✅ Election.ListForPublic field  
**Backend API:** 🟨 PublicController exists, limited  
**Frontend:** 🟨 Basic public pages, no full-screen mode

#### Pages Required

##### 8.1 PublicDisplayPage.vue
**Route:** `/public/display/:electionId` (full-screen mode)  
**Purpose:** Large display for public viewing of live results

**Layout (Full Screen):**
```
+----------------------------------------+
| [Election Name]                        |
| [Date] | [Convenor]                    |
+----------------------------------------+
|           ELECTION RESULTS             |
|                                        |
| ELECTED (9 positions)                  |
| 1. Alice Smith            127 votes    |
| 2. Bob Jones             124 votes    |
| 3. Charlie Brown         118 votes    |
| ...                                    |
| 9. Iris Wilson           89 votes     |
+----------------------------------------+
| ADDITIONAL NAMES (2 positions)         |
| 10. Jack Thompson        88 votes     |
| 11. Karen White          87 votes     |
+----------------------------------------+
| Total Ballots: 150                     |
| Valid Ballots: 148                     |
| Tally Status: Finalized                |
+----------------------------------------+
| Auto-refresh: ON | Last updated: 14:25 |
+----------------------------------------+
```

**Features:**
1. Full-screen mode (F11 or dedicated button)
2. Large, readable text
3. Auto-refresh every 30 seconds (configurable)
4. Real-time updates via SignalR
5. Show only if election is public (ListForPublic = true)
6. Clean, distraction-free design
7. Optional QR code for mobile access

**Display Options:**
- Show/hide additional names
- Show/hide vote counts
- Show/hide ballot statistics
- Theme: Light/Dark mode
- Refresh interval: 10s/30s/60s/Manual

**API Calls:**
- `GET /api/public/elections/{electionId}/display` - Get display data
  ```json
  {
    "electionName": "...",
    "dateOfElection": "...",
    "results": [...],
    "stats": { "totalBallots": 150, ... },
    "tallyStatus": "Finalized"
  }
  ```

**SignalR Events (PublicHub):**
- `ElectionResultsUpdated` - Refresh display when results change
- Join group: `public-display-{electionGuid}`

#### Backend Requirements

##### PublicController Enhancement
```csharp
[Route("api/public")]
public class PublicController : ControllerBase
{
    GET "elections"                    // List public elections
    GET "elections/{electionId}"       // Get election info
    GET "elections/{electionId}/display" // Get display data
    GET "elections/{electionId}/results" // Get results
}
```

##### DTOs Required
- `PublicElectionDisplayDto`
- `PublicElectionListDto`
- `PublicResultDto`

##### PublicHub Enhancement
- Add `JoinPublicDisplay(electionGuid)`
- Add `LeavePublicDisplay(electionGuid)`
- Broadcast `ElectionResultsUpdated` when tally completes

---

## MEDIUM PRIORITY FEATURES

### 9. Password Reset

**Backend API:** ❌ No password reset endpoints  
**Frontend:** ❌ No password reset pages

#### Pages Required

##### 9.1 ForgotPasswordPage.vue
**Route:** `/forgot-password`

**Layout:**
```
+----------------------------------------+
| Reset Your Password                    |
+----------------------------------------+
| Enter your email address:              |
| [Email input]                          |
| [Send Reset Link]                      |
+----------------------------------------+
```

##### 9.2 ResetPasswordPage.vue
**Route:** `/reset-password/:token`

**Layout:**
```
+----------------------------------------+
| Create New Password                    |
+----------------------------------------+
| New Password: [Password input]         |
| Confirm Password: [Password input]     |
| [Reset Password]                       |
+----------------------------------------+
```

#### Backend Requirements

##### AccountController Enhancement
```csharp
POST /api/account/forgot-password
POST /api/account/reset-password
GET  /api/account/validate-reset-token
```

---

### 10. Two-Factor Authentication

**Database Support:** ✅ TwoFactorToken entity exists  
**Backend API:** ❌ No 2FA endpoints  
**Frontend:** ❌ No 2FA pages

#### Pages Required

##### 10.1 Enable2FAPage.vue (in Profile Settings)
**Purpose:** Enable/disable 2FA

**Options:**
1. Authenticator App (TOTP)
2. SMS-based 2FA

##### 10.2 Verify2FAPage.vue
**Purpose:** Enter 2FA code during login

#### Backend Requirements

##### AuthController Enhancement
```csharp
POST /api/auth/enable-2fa
POST /api/auth/verify-2fa
POST /api/auth/disable-2fa
GET  /api/auth/2fa-status
```

---

### 11. Email/SMS Notification System

**Database Support:** ✅ SmsLog entity, email fields in Election  
**Backend API:** ❌ No notification endpoints  
**Frontend:** ❌ No notification configuration UI

#### Features Required

1. **Email Service Integration**
   - Configure SMTP or SendGrid
   - Template management
   - Send voter invitations
   - Send result notifications

2. **SMS Service Integration**
   - Twilio integration
   - SMS templates
   - Send verification codes
   - Send reminders

3. **Notification Templates**
   - Voter invitation email
   - Online voting reminder
   - Result announcement
   - Custom messages

#### Backend Requirements

##### NotificationsController.cs (New)
```csharp
[Route("api/elections/{electionId}/notifications")]
public class NotificationsController : ControllerBase
{
    POST "send-email"           // Send email to voters
    POST "send-sms"             // Send SMS to voters
    GET  "templates"            // List templates
    POST "test-email"           // Send test email
    POST "test-sms"             // Send test SMS
}
```

##### Services
- `EmailService` / `IEmailService`
- `SmsService` / `ISmsService`
- Integration with SendGrid, Twilio

---

## LOW PRIORITY FEATURES

### 12. Kiosk Mode

**Database Support:** ✅ Person.KioskCode field  
**Backend API:** ❌ No kiosk endpoints  
**Frontend:** ❌ No kiosk pages

Self-service voting stations where voters enter their own ballots using a kiosk code.

### 13. Historical Election Comparisons

**Backend API:** ❌ No historical analysis  
**Frontend:** ❌ No comparison pages

Compare results across multiple elections over time.

### 14. Statistical Analysis

**Backend API:** ❌ No analytics  
**Frontend:** ❌ No analytics pages

Advanced analytics: voting patterns, participation rates, geographic distribution.

### 15. Custom Report Templates

**Backend API:** ❌ No custom reports  
**Frontend:** ❌ No report builder

Allow administrators to create custom report templates with drag-and-drop fields.

---

## Implementation Priority Order

### PHASE C1: Location Management (3-4 days)
- LocationsController
- LocationsListPage
- LocationFormDialog
- Computer registration

### PHASE C2: Teller Management (3-4 days)
- TellersController
- TellersListPage
- TellerFormDialog
- Teller access code login

### PHASE C3: Advanced Election Configuration (2-3 days)
- Expand ElectionFormPage
- Add tabs for all field groups
- Enhanced validation

### PHASE C4: Front Desk Registration (3-4 days)
- FrontDeskController
- FrontDeskPage
- Real-time SignalR integration
- Roll call display

### PHASE C5: Online Voting Portal (4-5 days)
- OnlineVotingController
- Voter landing page
- Authentication workflows
- Ballot submission page
- Email/SMS integration

### PHASE C6: Ballot Import (2-3 days)
- ImportController enhancement
- BallotImportPage
- Real-time progress

### PHASE C7: Audit Log UI (2-3 days)
- AuditLogsController
- AuditLogsPage
- Audit middleware

### PHASE C8: Public Display Mode (2-3 days)
- PublicDisplayPage
- Full-screen mode
- Auto-refresh
- Real-time updates

---

## Total Effort Estimate

**HIGH PRIORITY: 22-29 days (4.5-6 weeks)**
**MEDIUM PRIORITY: 8-12 days (1.5-2.5 weeks)**
**LOW PRIORITY: 8-12 days (1.5-2.5 weeks)**

**TOTAL: 38-53 days (8-11 weeks) for all features**

With Phase B (critical fixes) and Phase D (UI polish), total project timeline: **10-15 weeks** to production-ready.
