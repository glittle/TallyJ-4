# SetupController API Documentation

## Overview
**Purpose**: Election setup wizard, voter import, ballot import, and notification management  
**Base Route**: `/Setup`  
**Authorization**: `[AllowTellersInActiveElection]` (class-level), some methods require `[ForAuthenticatedTeller]`  
**Authentication System**: Requires authenticated teller (admin) - guest tellers have limited access

## Controller Details

This controller manages the complete election setup workflow:
- **Election configuration** (4-step wizard)
- **CSV import** (voters)
- **Ballot import** (scanned ballots from previous elections)
- **People management** (CRUD operations on Person entities)
- **Location management** (voting locations)
- **Email/SMS notifications** (send to voters)
- **Kiosk code generation** (for in-person voting terminals)

**Key Business Logic Classes**:
- `SetupModel` - Election setup wizard
- `ImportCsvModel` - CSV file import (voters)
- `ImportBallotsModel` - Ballot file import (scanned ballots)
- `PeopleModel` - Person CRUD operations
- `LocationModel` - Location management
- `ElectionHelper` - Election configuration
- `EmailHelper` - Email notifications
- `TwilioHelper` - SMS notifications
- `VoterCodeHelper` - Kiosk code generation

**SignalR Hubs**:
- `BallotImportHub` - Real-time ballot import progress

---

## Endpoints

### Election Setup Views

### 1. GET `/Setup/Index`
**Purpose**: Display election setup wizard (4 steps)  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (admin only - guest tellers cannot configure elections)  
**Returns**: View "Setup"

**Response**:
- Renders `Views/Setup/Setup.cshtml` with `SetupModel`
- 4-step wizard: Election info, Voting rules, Online voting, Finalize

**Business Logic**:
- Creates new `SetupModel()` with current election data

**UI**: See `ui-screenshots-analysis.md` - Election Setup 4-step wizard screenshots

---

### 2. GET `/Setup/People`
**Purpose**: Display people management page (edit voter/candidate names)  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]` (both admin and guest tellers)  
**Returns**: View

**Response**:
- Renders `Views/Setup/People.cshtml` with `SetupModel`

**UI**: See `ui-screenshots-analysis.md` - "Edit People's Names" screenshot

---

### 3. GET `/Setup/Notify`
**Purpose**: Display notification management page (send email/SMS to voters)  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (admin only)  
**Returns**: View

**Response**:
- Renders `Views/Setup/Notify.cshtml`

**UI**: See `ui-screenshots-analysis.md` - "Send Notifications" screenshot

---

### Election Configuration

### 4. POST `/Setup/SaveElection`
**Purpose**: Save election configuration (from 4-step wizard)  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `election` (Election object):
  - `Name`, `Convenor`, `DateOfElection`
  - `ElectionType`, `ElectionMode`
  - `NumberToElect`, `NumberExtra`
  - `OnlineWhenOpen`, `OnlineWhenClose`
  - `ShowAsTest`, etc.

**Response**:
```json
{
  "Success": true,
  "Election": {
    // Updated election object
  }
}
```

**Business Logic**:
- Delegates to `ElectionHelper.SaveElection(election)`
- Validates election settings
- Updates database

**Data Access**:
- Updates `Election` table

**SignalR**: May trigger `MainHub` updates for election status changes

**Migration Notes**:
- Validate `OnlineWhenOpen < OnlineWhenClose`
- Validate `NumberToElect > 0`

---

### 5. POST `/Setup/SaveNotification`
**Purpose**: Save email/SMS notification templates  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `emailSubject` (string) - Email subject line
- `emailText` (string) - Email body (HTML)
- `smsText` (string) - SMS message text

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Delegates to `ElectionHelper.SaveNotification(emailSubject, emailText, smsText)`
- Stores templates in `Election` table

**Data Access**:
- Updates `Election.EmailText`, `Election.EmailSubject`, `Election.SmsText`

---

### 6. POST `/Setup/DetermineRules`
**Purpose**: Get voting rules for election type and mode  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with rules

**Request Parameters**:
- `type` (string) - Election type (e.g., "LSA", "NSA", "Unit")
- `mode` (string) - Election mode (e.g., "Normal", "TieBreak")

**Response**:
```json
{
  "NumberToElect": 9,
  "NumberExtra": 2,
  "CanReceiveExtraVotes": true,
  "VotesPerBallot": 9
}
```

**Business Logic**:
- Static method: `ElectionHelper.GetRules(type, mode)`
- Returns Bahá'í electoral rules for given type/mode

**Migration Notes**:
- Rules are hard-coded business logic
- Consider configuration file or database table for flexibility

---

### CSV Import (Voters)

### 7. GET `/Setup/ImportCsv`
**Purpose**: Display CSV import page  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: View

**Request Parameters**:
- `importCsvModel` (ImportCsvModel?) - Optional model

**Response**:
- Renders `Views/Setup/ImportCsv.cshtml`

**UI**: See `ui-screenshots-analysis.md` - CSV Import screenshot

---

### 8. POST `/Setup/Upload`
**Purpose**: Upload CSV file for voter import  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with upload result

**Request**:
- File upload via `Request.Files`

**Response**:
```json
{
  "success": true,
  "rowId": 123,  // ImportFile.C_RowId
  "messages": []
}
```

**Error Response**:
```json
{
  "success": false,
  "rowId": 0,
  "messages": ["Invalid file format", "..."]
}
```

**Business Logic**:
1. Validates file (CSV format, size limits)
2. Stores file in `ImportFile` table
3. Returns file ID for mapping step

**Data Access**:
- Inserts into `ImportFile` table
- Delegates to `ImportCsvModel.ProcessUpload(out rowId)`

---

### 9. POST `/Setup/ReadFields`
**Purpose**: Read CSV headers/fields for column mapping  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with field list

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId

**Response**:
```json
{
  "fields": ["Name", "Email", "Phone", "Area"],
  "sampleData": [
    ["John Smith", "john@example.com", "+1234567890", "District 1"],
    // ... more sample rows
  ]
}
```

**Business Logic**:
- Reads first few rows of CSV
- Returns headers and sample data for mapping UI

**Data Access**:
- `ImportCsvModel.ReadFields(id)`

---

### 10. POST `/Setup/SaveMapping`
**Purpose**: Save CSV column-to-Person field mapping  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId
- `mapping` (List<string>) - Array of Person field names in CSV column order

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Stores mapping in `ImportFile.FileInfo` (JSON)
- Mapping example: `["FullName", "Email", "Phone", "Area", null, "OtherInfo"]`

**Data Access**:
- Updates `ImportFile` table
- Delegates to `ImportCsvModel.SaveMapping(id, mapping)`

---

### 11. POST `/Setup/FileCodePage`
**Purpose**: Set character encoding for CSV file  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId
- `cp` (int) - Code page (e.g., 1252 = Windows-1252, 65001 = UTF-8)

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Stores code page in `ImportFile` for proper encoding when reading

**Data Access**:
- `ImportCsvModel.SaveCodePage(id, cp)`

**Migration Notes**:
- Default to UTF-8 in .NET Core
- Provide encoding selection UI for legacy files

---

### 12. POST `/Setup/FileDataRow`
**Purpose**: Set first data row number (skip header rows)  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId
- `firstDataRow` (int) - Row number where data starts (1-based)

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Stores first data row in `ImportFile`
- Used when importing to skip header/comment rows

**Data Access**:
- `ImportCsvModel.SaveDataRow(id, firstDataRow)`

---

### 13. POST `/Setup/CopyMap`
**Purpose**: Copy mapping from one import file to another  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `from` (int) - Source ImportFile.C_RowId
- `to` (int) - Target ImportFile.C_RowId

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Copies mapping, code page, first data row from one file to another
- Useful for re-importing updated CSV with same format

**Data Access**:
- `ImportCsvModel.CopyMap(from, to)`

---

### 14. POST `/Setup/Import`
**Purpose**: Execute CSV import - insert/update Person records  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with import results

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId

**Response**:
```json
{
  "Success": true,
  "Inserted": 50,
  "Updated": 10,
  "Errors": []
}
```

**Business Logic**:
1. Reads CSV with saved mapping
2. Validates each row
3. Inserts new Person records or updates existing (by name match)
4. Returns summary statistics

**Data Access**:
- Inserts/updates `Person` table
- Delegates to `ImportCsvModel.Import(id)`

**SignalR**: May use `ImportHub` for progress updates on large files

**Migration Notes**:
- Match existing people by FullName or Email
- Consider duplicate detection strategy

---

### 15. GET `/Setup/GetUploadlist`
**Purpose**: Get list of uploaded CSV files for current election  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: HTML partial view

**Response**:
- HTML list of ImportFile records

**Business Logic**:
- Queries `ImportFile` for current election

**Data Access**:
- `ImportCsvModel.GetUploadList()`

**Migration Notes**:
- .NET Core: Return JSON array instead of HTML

---

### 16. GET `/Setup/Download`
**Purpose**: Download previously uploaded CSV file  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: File download

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId

**Response**:
- File download with original filename
- Content-Type: `application/octet-stream`

**Business Logic**:
- Retrieves file from `ImportFile.Contents` (byte array)

**Data Access**:
- Queries `ImportFile` by C_RowId and ElectionGuid

---

### 17. POST `/Setup/DeleteFile`
**Purpose**: Delete uploaded CSV file  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: HTML partial view (updated file list)

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId

**Response**:
- Updated file list HTML

**Business Logic**:
- Deletes `ImportFile` record

**Data Access**:
- `ImportCsvModel.DeleteFile(id)`

---

### Ballot Import (Scanned Ballots)

### 18. GET `/Setup/ImportBallots`
**Purpose**: Display ballot import page (for scanned ballots from previous elections)  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: View

**Request Parameters**:
- `importBallotsModel` (ImportBallotsModel?) - Optional model

**Response**:
- Renders `Views/Setup/ImportBallots.cshtml`

---

### 19. POST `/Setup/JoinBallotImportHub`
**Purpose**: Join SignalR hub for ballot import progress updates  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: void

**Request Parameters**:
- `connId` (string) - SignalR connection ID

**Business Logic**:
- Adds connection to `BallotImportHub`

**SignalR**:
- Receives progress updates during ballot import
- See `signalr/hubs/BallotImportHub.md`

---

### 20. POST `/Setup/UploadBallots`
**Purpose**: Upload ballot file (CSV or XML format)  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with upload result

**Request**:
- File upload via `Request.Files`

**Response**:
```json
{
  "success": true,
  "rowId": 123,
  "messages": []
}
```

**Business Logic**:
- Similar to CSV upload but for ballot data
- Stores file in `ImportFile` table

**Data Access**:
- `ImportBallotsModel.ProcessUpload(out rowId)`

---

### 21. POST `/Setup/GetBallotsPreviewInfo`
**Purpose**: Get preview of ballot file contents  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with preview data

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId
- `forceRefreshCache` (bool) - Force re-parse file

**Response**:
```json
{
  "numBallots": 50,
  "sampleBallots": [
    // Sample ballot data
  ]
}
```

**Business Logic**:
- Parses ballot file
- Returns preview for confirmation

**Data Access**:
- `ImportBallotsModel.GetPreviewInfo(id, forceRefreshCache)`

---

### 22. POST `/Setup/LoadBallotsFile`
**Purpose**: Execute ballot import - create Ballot and Vote records  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with import results

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId

**Response**:
```json
{
  "Success": true,
  "BallotsCreated": 50,
  "VotesCreated": 450
}
```

**Business Logic**:
1. Parses ballot file
2. Creates `Ballot` records
3. Creates `Vote` records
4. Matches candidate names to `Person` records

**Data Access**:
- Inserts into `Ballot` and `Vote` tables
- `ImportBallotsModel.LoadFile(id)`

**SignalR**:
- Broadcasts progress via `BallotImportHub`

---

### 23. POST `/Setup/RemoveImportedInfo`
**Purpose**: Delete all imported ballots and votes  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Deletes all `Ballot` and `Vote` records for current election
- Used to reset election before re-import

**Data Access**:
- `ImportBallotsModel.RemoveImportedInfo()`

---

### 24. POST `/Setup/DeleteBallotsFile`
**Purpose**: Delete uploaded ballot file  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: HTML partial view

**Request Parameters**:
- `id` (int) - ImportFile.C_RowId

**Response**:
- Updated file list HTML

**Business Logic**:
- Deletes `ImportFile` record

**Data Access**:
- `ImportBallotsModel.DeleteFile(id)`

---

### 25. GET `/Setup/GetBallotUploadlist`
**Purpose**: Get list of uploaded ballot files  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: HTML partial view

**Response**:
- HTML list of ballot ImportFile records

**Data Access**:
- `ImportBallotsModel.GetUploadList()`

---

### People Management

### 26. POST `/Setup/SavePerson`
**Purpose**: Create or update a Person record  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `person` (Person object):
  - `C_RowId`, `FullName`, `Email`, `Phone`
  - `Area`, `OtherInfo`
  - `CanReceiveVotes`, `CanVote`
  - `IneligibleReasonGuid`

**Response**:
```json
{
  "Success": true,
  "Person": {
    // Updated person object
  }
}
```

**Business Logic**:
- Validates person data
- Inserts or updates Person record
- Delegates to `PeopleModel.SavePerson(person)`

**Data Access**:
- Inserts/updates `Person` table

**SignalR**: May trigger `FrontDeskHub` updates if person is registered

---

### 27. POST `/Setup/DeletePerson`
**Purpose**: Delete a Person record  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `personId` (int) - Person.C_RowId

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Deletes Person record
- Also deletes related Vote records

**Data Access**:
- `PeopleModel.DeletePerson(personId)`

---

### 28. POST `/Setup/DeleteAllPeople`
**Purpose**: Delete ALL people in current election  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Deletes all `Person` records for current election
- Used to reset election before re-import
- Logs action

**Data Access**:
- `PeopleModel.DeleteAllPeople()`

**Warning**: Destructive operation - consider soft delete or confirmation

---

### Location Management

### 29. POST `/Setup/EditLocation`
**Purpose**: Edit location name  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `id` (int) - Location.C_RowId
- `text` (string) - New location name

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Updates location name

**Data Access**:
- `ContextItems.LocationModel.EditLocation(id, text)`

---

### 30. POST `/Setup/SortLocations`
**Purpose**: Reorder locations (for display order)  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with success/error

**Request Parameters**:
- `ids` (List<int>) - Array of Location.C_RowId in desired order

**Response**:
```json
{
  "Success": true
}
```

**Business Logic**:
- Updates `Location.SortOrder` for each location

**Data Access**:
- `ContextItems.LocationModel.SortLocations(ids)`

---

### Notifications

### 31. POST `/Setup/SendEmail`
**Purpose**: Send email notifications to voters  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with send results

**Request Parameters**:
- `list` (string) - JSON array of Person IDs or "all"

**Response**:
```json
{
  "Success": true,
  "Sent": 50,
  "Failed": 2,
  "Errors": ["Invalid email for John Smith"]
}
```

**Business Logic**:
1. Loads email template from Election
2. Sends email to each person in list
3. Logs results in `Message` table

**Data Access**:
- `EmailHelper.SendHeadTellerEmail(list)`

**Integration**:
- SMTP email service

---

### 32. POST `/Setup/SendSms`
**Purpose**: Send SMS notifications to voters  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with send results

**Request Parameters**:
- `list` (string) - JSON array of Person IDs or "all"

**Response**:
```json
{
  "Success": true,
  "Sent": 50,
  "Failed": 2,
  "Errors": ["Invalid phone for John Smith"]
}
```

**Business Logic**:
1. Loads SMS template from Election
2. Sends SMS to each person in list via Twilio
3. Logs results in `SmsLog` table

**Data Access**:
- `TwilioHelper.SendHeadTellerMessage(list)`

**Integration**:
- Twilio SMS service

---

### 33. GET `/Setup/GetContacts`
**Purpose**: Get list of voter contact information (emails/phones)  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with contact list

**Response**:
```json
{
  "contacts": [
    {
      "PersonId": 123,
      "Name": "John Smith",
      "Email": "john@example.com",
      "Phone": "+1234567890",
      "HasEmail": true,
      "HasPhone": true
    }
  ]
}
```

**Business Logic**:
- Returns all people with contact information

**Data Access**:
- `EmailHelper.GetContacts()`

---

### 34. GET `/Setup/GetContactLog`
**Purpose**: Get log of sent emails/SMS for current election  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with log entries

**Request Parameters**:
- `lastLogId` (int) - Last C_RowId received (for incremental updates)

**Response**:
```json
{
  "log": [
    {
      "C_RowId": 123,
      "AsOf": "2024-01-15T10:30:00Z",
      "Type": "Email",
      "Recipient": "john@example.com",
      "Status": "Sent"
    }
  ]
}
```

**Business Logic**:
- Returns log entries since `lastLogId`

**Data Access**:
- `EmailHelper.GetContactLog(lastLogId)`

---

### 35. GET `/Setup/DownloadContactLog`
**Purpose**: Download contact log as CSV file  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: File download

**Response**:
- CSV file with contact log

**Data Access**:
- `EmailHelper.DownloadContactLog()`

---

### Kiosk Code Generation

### 36. POST `/Setup/GenerateKioskCode`
**Purpose**: Generate one-time kiosk code for in-person voter  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]`  
**Returns**: JSON with kiosk code

**Request Parameters**:
- `personId` (int) - Person.C_RowId

**Response**:
```json
{
  "Success": true,
  "Code": "ABC123"
}
```

**Error Response**:
```json
{
  "Message": "Person not found"
}
```

**Business Logic**:
1. Generates unique 6-character code
2. Stores in `Person.KioskCode`
3. Code is one-time use (cleared after login)

**Data Access**:
- `VoterCodeHelper("").GenerateKioskCode(personId, out errorMessage)`

**Authentication**:
- See `security/authentication.md` - **System 3: Voter Authentication (Kiosk mode)**

**Migration Notes**:
- Kiosk code should expire after short time (e.g., 1 hour)
- Consider QR code generation for easy scanning

---

## Data Models Referenced

### SetupModel
- **Purpose**: Election setup wizard data

### ImportCsvModel
- **Methods**: Upload, ReadFields, SaveMapping, Import, DeleteFile, GetUploadList

### ImportBallotsModel
- **Methods**: Upload, GetPreviewInfo, LoadFile, RemoveImportedInfo, DeleteFile

### PeopleModel
- **Methods**: SavePerson, DeletePerson, DeleteAllPeople

### LocationModel
- **Methods**: EditLocation, SortLocations

### ElectionHelper
- **Static**: GetRules(type, mode)
- **Methods**: SaveElection, SaveNotification

### EmailHelper
- **Methods**: SendHeadTellerEmail, GetContacts, GetContactLog, DownloadContactLog

### TwilioHelper
- **Methods**: SendHeadTellerMessage

### VoterCodeHelper
- **Methods**: GenerateKioskCode

---

## Authorization Details

### Class-Level Authorization
- `[AllowTellersInActiveElection]` - Both admin and guest tellers can access most endpoints

### Method-Level Authorization
- `[ForAuthenticatedTeller]` - Admin only (no guest tellers):
  - SaveElection, SaveNotification
  - All import operations
  - SavePerson, DeletePerson, DeleteAllPeople
  - SendEmail, SendSms
  - GenerateKioskCode

### Authorization Matrix
| Endpoint | Admin | Guest Teller |
|----------|-------|--------------|
| Index (Setup wizard) | ✅ | ❌ |
| People (view) | ✅ | ✅ |
| Notify | ✅ | ❌ |
| SaveElection | ✅ | ❌ |
| Import CSV | ✅ | ❌ |
| SavePerson | ✅ | ❌ |
| DeletePerson | ✅ | ❌ |
| SendEmail/SMS | ✅ | ❌ |

---

## Session Dependencies

- `UserSession.CurrentElection` - Current election
- `UserSession.CurrentElectionGuid` - Current election GUID

---

## Integration Points

### Used By:
- **Election Setup Wizard** - 4-step configuration
- **CSV Import** - Voter import workflow
- **Ballot Import** - Scanned ballot import
- **Edit People's Names** - Person management
- **Send Notifications** - Email/SMS to voters

### Calls To:
- `SetupModel`, `ImportCsvModel`, `ImportBallotsModel` - Business logic
- `PeopleModel`, `LocationModel` - Data access
- `EmailHelper`, `TwilioHelper` - External integrations
- `BallotImportHub`, `ImportHub` - SignalR progress updates

### External Integrations:
- **SMTP** - Email notifications
- **Twilio** - SMS notifications

---

## .NET Core Migration Recommendations

### API Design
```csharp
// Election setup
POST   /api/elections/{electionId}                     // SaveElection
GET    /api/elections/{electionId}/rules/{type}/{mode} // DetermineRules

// CSV import
POST   /api/elections/{electionId}/import/csv/upload   // Upload
GET    /api/elections/{electionId}/import/csv/{id}     // ReadFields
POST   /api/elections/{electionId}/import/csv/{id}/map // SaveMapping
POST   /api/elections/{electionId}/import/csv/{id}/import // Import

// People
POST   /api/elections/{electionId}/people              // SavePerson
DELETE /api/elections/{electionId}/people/{id}         // DeletePerson

// Notifications
POST   /api/elections/{electionId}/notifications/email // SendEmail
POST   /api/elections/{electionId}/notifications/sms   // SendSms

// Kiosk codes
POST   /api/elections/{electionId}/people/{id}/kiosk-code // GenerateKioskCode
```

### Authorization
```csharp
[Authorize(Policy = "AuthenticatedTeller")]
public async Task<IActionResult> SaveElection(Guid electionId, [FromBody] ElectionDto election) { }

[Authorize(Policy = "TellerInActiveElection")]
public async Task<IActionResult> GetPeople(Guid electionId) { }
```

### File Upload
```csharp
[HttpPost("upload")]
public async Task<IActionResult> UploadCsv(IFormFile file)
{
    // Use IFormFile instead of Request.Files
    if (file == null || file.Length == 0)
        return BadRequest("No file uploaded");
    
    // Process file
    using var stream = file.OpenReadStream();
    // ...
}
```

### Async/Await
- All import operations should be async
- Use background jobs for large imports (Hangfire, Azure Functions)

---

## Testing Scenarios

1. **Election Setup**:
   - Save election with valid settings → success
   - Save election with invalid dates → error

2. **CSV Import**:
   - Upload valid CSV → file stored
   - Map columns → mapping saved
   - Import → people created
   - Re-import same file → people updated (not duplicated)

3. **Ballot Import**:
   - Upload ballot file → file stored
   - Load ballots → ballots and votes created
   - Remove imported info → ballots deleted

4. **People Management**:
   - Save new person → created
   - Save existing person → updated
   - Delete person → deleted

5. **Notifications**:
   - Send email to all → emails sent
   - Send SMS to specific people → SMS sent

6. **Authorization**:
   - Admin can access all endpoints
   - Guest teller CANNOT access SaveElection, Import, etc.

---

## Related Documentation

- **Database**: See `database/entities.md` for Election, Person, ImportFile schemas
- **Authentication**: See `security/authentication.md` for teller authentication
- **SignalR**: See `signalr/hubs/BallotImportHub.md`
- **Integrations**: See `integrations/email.md`, `integrations/sms.md`
- **UI**: See `ui-screenshots-analysis.md` for setup wizard screenshots
