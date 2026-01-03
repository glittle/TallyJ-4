# SysAdminController API Documentation

## Overview
**Purpose**: System administration - global logs, election monitoring, and analytics  
**Base Route**: `/SysAdmin`  
**Authorization**: `[ForSysAdmin]` (class-level) - requires admin with `IsSysAdmin` claim  
**Authentication System**: Admin authentication (System 1) with special SysAdmin privilege

## Controller Details

This controller provides system-wide administrative functions:
- **Main activity log** - All system activity across all elections
- **Online voting analytics** - Track online voting usage across elections
- **Election list** - All elections with statistics
- **Unconnected voters** - Orphaned voter records without matching Person records

**Key Features**:
- Cross-election queries (not limited to current election)
- Complex joins for analytics
- Export capabilities
- System health monitoring

**Authorization**:
- User must be authenticated admin (`AspNetUsers` account)
- User's `Comment` field must equal `"SysAdmin"` (see `security/authentication.md`)

---

## Endpoints

### 1. GET `/SysAdmin/Index`
**Purpose**: Display system administration dashboard  
**HTTP Method**: GET  
**Authorization**: `[ForSysAdmin]`  
**Returns**: View

**Response**:
- Renders `Views/SysAdmin/Index.cshtml`
- Dashboard with 4 tabs: Main Log, Online Voting Log, Election List, Unconnected Voters

**UI**: See `ui-screenshots-analysis.md` - System Administration screenshot (4 tabs)

---

### 2. GET `/SysAdmin/GetMainLog`
**Purpose**: Retrieve system activity log with search/filter capabilities  
**HTTP Method**: GET  
**Authorization**: `[ForSysAdmin]`  
**Returns**: JSON with log entries

**Request Parameters**:
- `searchText` (string) - Search in log details (SQL LIKE pattern)
- `searchName` (string) - Search in voter ID, election convenor, or election name
- `lastRowId` (int) - Last C_RowId received (for pagination, default: 0)
- `numToShow` (int) - Number of entries to return (default: 50)
- `fromDate` (DateTime?) - Filter start date
- `toDate` (DateTime?) - Filter end date

**Response**:
```json
{
  "logLines": [
    {
      "C_RowId": 12345,
      "AsOf": "2024-01-15T10:30:00Z",
      "Details": "Voter logged in",
      "ElectionName": "LSA Election 2024",
      "HostAndVersion": "v3.5.0",
      "VoterId": "voter@example.com",
      "ComputerCode": "ABC123"
    }
  ],
  "Success": true
}
```

**Business Logic**:
1. Joins `C_Log` with `Election` table (left join)
2. Filters by `lastRowId` for pagination (only rows with `C_RowId < lastRowId`)
3. Filters by date range if provided (UTC dates)
4. Searches `Details` field using SQL `PATINDEX` (% wildcards)
5. Searches `VoterId`, `Convenor`, `Name` fields
6. Orders by `C_RowId DESC` (newest first)
7. Returns top N entries

**Data Access**:
- `C_Log` table (main activity log)
- `Election` table (election names)

**SQL Patterns**:
```csharp
// Pattern matching with PATINDEX
SqlFunctions.PatIndex(searchText, j.l.Details) > 0
```

**Migration Notes**:
- Use EF Core LINQ equivalent: `EF.Functions.Like()`
- Consider full-text search for better performance
- Pagination: Use `lastRowId` for cursor-based pagination (better than OFFSET/SKIP)

---

### 3. GET `/SysAdmin/GetOnlineVotingLog`
**Purpose**: Retrieve analytics for online voting across all elections  
**HTTP Method**: GET  
**Authorization**: `[ForSysAdmin]`  
**Returns**: JSON with online voting statistics

**Request Parameters**:
- `numToShow` (int) - Number of elections to return (default: 50)

**Response**:
```json
{
  "logLines": [
    {
      "Name": "LSA Election 2024",
      "Convenor": "John Smith",
      "Email": "admin@example.com [Owner], helper@example.com [Teller]",
      "TallyStatus": "Finalized",
      "OnlineWhenOpen": "2024-01-10T08:00:00Z",
      "OnlineWhenClose": "2024-01-15T20:00:00Z",
      "NumberToElect": 9,
      "Activated": 50,
      "Submitted": 45,
      "Processed": 44,
      "First": "2024-01-10T08:15:00Z",
      "MostRecent": "2024-01-15T19:45:00Z"
    }
  ],
  "Success": true
}
```

**Business Logic**:
1. Joins `Election`, `JoinElectionUser`, `Memberships`, `OnlineVotingInfo` tables
2. Filters to elections with `OnlineWhenOpen != null` (online voting enabled)
3. Orders by `OnlineWhenClose DESC` (most recent first)
4. Aggregates:
   - **Activated**: Count of all `OnlineVotingInfo` records
   - **Submitted**: Count where `Status == Submitted`
   - **Processed**: Count where `Status == Processed`
   - **First**: Earliest `WhenStatus` timestamp
   - **MostRecent**: Latest `WhenStatus` timestamp
5. Returns admin emails with roles

**Data Access**:
- `Election` table
- `JoinElectionUser` table (election admins)
- `Memberships` table (admin emails)
- `OnlineVotingInfo` table (voter ballot status)

**Use Cases**:
- Monitor online voting adoption
- Identify elections with issues (low submission rates)
- Analyze voting patterns (when do voters submit?)

**Migration Notes**:
- Consider caching this query (expensive joins)
- Add indexes on `OnlineVotingInfo.Status` and `OnlineVotingInfo.WhenStatus`

---

### 4. GET `/SysAdmin/GetElectionList`
**Purpose**: Retrieve list of all elections with statistics  
**HTTP Method**: GET  
**Authorization**: `[ForSysAdmin]`  
**Returns**: JSON with election list

**Request Parameters**:
- `numToShow` (int) - Number of elections to return (default: 50)

**Response**:
```json
{
  "logLines": [
    {
      "C_RowId": 123,
      "Name": "LSA Election 2024",
      "Convenor": "John Smith",
      "DateOfElection": "2024-01-15T00:00:00Z",
      "Email": "admin@example.com [Owner], helper@example.com [Teller]",
      "ElectionType": "L",
      "ElectionMode": "N",
      "ShowAsTest": false,
      "TallyStatus": "Finalized",
      "NumberToElect": 9,
      "NumOnline": 45,
      "NumBallots": 120,
      "NumPeople": 200,
      "RecentActivity": "2024-01-16T10:30:00Z"
    }
  ],
  "Success": true
}
```

**Business Logic**:
1. Complex multi-join query:
   - `Election` → `JoinElectionUser` → `Memberships` (admin emails)
   - `Election` → `OnlineVotingInfo` (online ballot count)
   - `Election` → `Person` (voter count)
   - `Election` → `Location` → `Ballot` → `Vote` (ballot count)
   - `Election` → `C_Log` (recent activity)
2. Orders by `DateOfElection DESC`, then `Name`
3. Returns top N elections

**Aggregations**:
- **NumOnline**: Count of `OnlineVotingInfo` records
- **NumBallots**: 
  - For single-name elections: Sum of `Vote.SingleNameElectionCount`
  - For multi-name elections: Count of `Ballot` records
- **NumPeople**: Count of `Person` records
- **RecentActivity**: Max `C_Log.AsOf` timestamp

**Data Access**:
- `Election`, `JoinElectionUser`, `Memberships`, `OnlineVotingInfo`, `Person`, `Location`, `Ballot`, `Vote`, `C_Log` tables

**Use Cases**:
- Monitor all elections in system
- Identify inactive elections
- Track election statistics

**Migration Notes**:
- Very expensive query - consider materialized views or caching
- Add indexes on foreign keys

---

### 5. GET `/SysAdmin/GetUnconnectedVoters`
**Purpose**: Find orphaned `OnlineVoter` records not matched to any `Person` record  
**HTTP Method**: GET  
**Authorization**: `[ForSysAdmin]`  
**Returns**: JSON with unconnected voter list

**Response**:
```json
{
  "logLines": [
    {
      "C_RowId": 123,
      "Email": "orphan@example.com",
      "Phone": null,
      "Country": "US",
      "WhenRegistered": "2024-01-10T10:30:00Z",
      "WhenLastLogin": "2024-01-15T15:45:00Z"
    }
  ],
  "Success": true
}
```

**Business Logic**:
1. Find all `OnlineVoter` records with `VoterIdType` = Email or Phone
2. Attempt to match to `Person` records:
   - Email voters: Match `OnlineVoter.VoterId` = `Person.Email`
   - Phone voters: Match `OnlineVoter.VoterId` = `Person.Phone`
3. Return only unmatched voters

**Query Strategy**:
```csharp
// Find connected voters (Email match)
var connectedVotersEmail = dbContext.OnlineVoter
  .Join(dbContext.Person, ov => ov.VoterId, p => p.Email, ...)
  
// Find connected voters (Phone match)
var connectedVotersPhone = dbContext.OnlineVoter
  .Join(dbContext.Person, ov => ov.VoterId, p => p.Phone, ...)

// Union of connected IDs
var connectedIds = connectedVotersEmail.Union(connectedVotersPhone)

// Return voters NOT in connected list
var unconnected = dbContext.OnlineVoter
  .Where(ov => !connectedIds.Contains(ov.C_RowId))
```

**Use Cases**:
- Identify voter authentication issues
- Find voters who registered but aren't in any election
- Data cleanup

**Why This Happens**:
- Voter registered with email/phone not matching any `Person.Email` or `Person.Phone`
- Typos in voter import CSV
- Voter changed email/phone after import

**Migration Notes**:
- Consider LEFT JOIN approach for better performance
- Add index on `Person.Email` and `Person.Phone`

---

## Data Models Referenced

All queries are direct LINQ-to-Entities, no business logic classes used.

**Tables Queried**:
- `C_Log` - Main activity log
- `Election` - Elections
- `JoinElectionUser` - Election admins/tellers
- `Memberships` - User accounts (ASP.NET Membership)
- `OnlineVotingInfo` - Online ballot status
- `Person` - Voters/candidates
- `Location`, `Ballot`, `Vote` - Ballot data
- `OnlineVoter` - Voter authentication records
- `SmsLog` - SMS delivery log (not directly queried in this controller)

---

## Authorization Details

### `[ForSysAdmin]` Attribute
- Requires authenticated admin user
- Requires `Comment` field = "SysAdmin" in `AspNetUsers` table
- See `security/authentication.md` for details

**Example Authorization Logic** (from attribute):
```csharp
var isSysAdmin = User.FindFirst("IsSysAdmin")?.Value == "true";
if (!isSysAdmin) return Unauthorized();
```

**Setting SysAdmin**:
- Manually set `AspNetUsers.Comment = "SysAdmin"` in database
- Or create admin management UI (not in current system)

---

## Session Dependencies

None - queries are global (not scoped to current election)

---

## Integration Points

### Used By:
- **System Administration Dashboard** - SysAdmin tab

### Calls To:
- Direct database queries (no business logic layer)

---

## .NET Core Migration Recommendations

### API Design
```csharp
// System logs
GET    /api/admin/logs                         // GetMainLog
GET    /api/admin/online-voting-analytics      // GetOnlineVotingLog
GET    /api/admin/elections                    // GetElectionList
GET    /api/admin/unconnected-voters           // GetUnconnectedVoters
```

### Authorization
```csharp
[Authorize(Policy = "SystemAdministrator")]
public class AdminController : ControllerBase
{
    // Endpoints
}
```

**Policy Definition**:
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdministrator", policy =>
        policy.RequireClaim("IsSysAdmin", "true"));
});
```

### Performance Optimizations

#### 1. Add Indexes
```sql
CREATE INDEX IX_C_Log_AsOf ON C_Log(AsOf DESC);
CREATE INDEX IX_C_Log_ElectionGuid ON C_Log(ElectionGuid);
CREATE INDEX IX_OnlineVotingInfo_Status ON OnlineVotingInfo(Status);
CREATE INDEX IX_Person_Email ON Person(Email);
CREATE INDEX IX_Person_Phone ON Person(Phone);
```

#### 2. Use Async/Await
```csharp
public async Task<IActionResult> GetMainLog(...)
{
    var logLines = await query.ToListAsync();
    // ...
}
```

#### 3. Consider Caching
```csharp
// Cache election list for 5 minutes
var cacheKey = "admin:elections";
if (!_cache.TryGetValue(cacheKey, out List<ElectionDto> elections))
{
    elections = await GetElectionsFromDatabase();
    _cache.Set(cacheKey, elections, TimeSpan.FromMinutes(5));
}
```

#### 4. Use Materialized Views
For `GetElectionList`, consider creating a database view with pre-computed statistics:
```sql
CREATE VIEW vw_ElectionStatistics AS
SELECT 
    e.C_RowId,
    e.Name,
    COUNT(DISTINCT p.C_RowId) AS NumPeople,
    COUNT(DISTINCT b.C_RowId) AS NumBallots,
    -- ... other aggregations
FROM Election e
LEFT JOIN Person p ON e.ElectionGuid = p.ElectionGuid
LEFT JOIN Ballot b ON e.ElectionGuid = b.ElectionGuid
-- ...
GROUP BY e.C_RowId, e.Name, ...
```

Then query the view instead of performing joins:
```csharp
var elections = await _context.ElectionStatistics
    .OrderByDescending(e => e.DateOfElection)
    .Take(numToShow)
    .ToListAsync();
```

---

## Response DTOs

```csharp
// Main log entry
public class LogEntryDto
{
    public int C_RowId { get; set; }
    public DateTime AsOf { get; set; }
    public string Details { get; set; }
    public string ElectionName { get; set; }
    public string HostAndVersion { get; set; }
    public string VoterId { get; set; }
    public string ComputerCode { get; set; }
}

// Online voting analytics
public class OnlineVotingAnalyticsDto
{
    public string Name { get; set; }
    public string Convenor { get; set; }
    public string Email { get; set; }
    public string TallyStatus { get; set; }
    public DateTime OnlineWhenOpen { get; set; }
    public DateTime OnlineWhenClose { get; set; }
    public int NumberToElect { get; set; }
    public int Activated { get; set; }
    public int Submitted { get; set; }
    public int Processed { get; set; }
    public DateTime? First { get; set; }
    public DateTime? MostRecent { get; set; }
}

// Election list entry
public class ElectionSummaryDto
{
    public int C_RowId { get; set; }
    public string Name { get; set; }
    public string Convenor { get; set; }
    public DateTime DateOfElection { get; set; }
    public string Email { get; set; }
    public string ElectionType { get; set; }
    public string ElectionMode { get; set; }
    public bool ShowAsTest { get; set; }
    public string TallyStatus { get; set; }
    public int NumberToElect { get; set; }
    public int NumOnline { get; set; }
    public int NumBallots { get; set; }
    public int NumPeople { get; set; }
    public DateTime? RecentActivity { get; set; }
}

// Unconnected voter
public class UnconnectedVoterDto
{
    public int C_RowId { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Country { get; set; }
    public DateTime WhenRegistered { get; set; }
    public DateTime? WhenLastLogin { get; set; }
}
```

---

## Testing Scenarios

1. **GetMainLog**:
   - No filters → return 50 most recent entries
   - Search by text → return matching entries
   - Search by name → return entries for voter/election
   - Date range filter → return entries in range
   - Pagination → return next 50 entries

2. **GetOnlineVotingLog**:
   - Return elections with online voting enabled
   - Verify counts (Activated, Submitted, Processed)
   - Verify date sorting (most recent first)

3. **GetElectionList**:
   - Return all elections with statistics
   - Verify ballot count (single-name vs multi-name)
   - Verify recent activity timestamp

4. **GetUnconnectedVoters**:
   - Create `OnlineVoter` with email not matching any `Person` → returned
   - Create `OnlineVoter` with matching email → NOT returned
   - Create `OnlineVoter` with phone not matching any `Person` → returned

5. **Authorization**:
   - Non-SysAdmin user → 403 Forbidden
   - SysAdmin user → 200 OK

---

## API Call Examples

### JavaScript (Current System)
```javascript
// Get main log
$.get('/SysAdmin/GetMainLog', {
  searchText: 'error',
  numToShow: 50,
  fromDate: '2024-01-01',
  toDate: '2024-01-31'
}, function(data) {
  var logLines = data.logLines;
  // Display in table
});

// Get online voting analytics
$.get('/SysAdmin/GetOnlineVotingLog', {
  numToShow: 100
}, function(data) {
  var analytics = data.logLines;
  // Display statistics
});

// Get election list
$.get('/SysAdmin/GetElectionList', {
  numToShow: 50
}, function(data) {
  var elections = data.logLines;
  // Display in table
});

// Get unconnected voters
$.get('/SysAdmin/GetUnconnectedVoters', function(data) {
  var voters = data.logLines;
  // Display orphaned voters
});
```

### TypeScript + Axios (Vue 3 Migration)
```typescript
// Admin service
async getMainLog(params: {
  searchText?: string;
  searchName?: string;
  lastRowId?: number;
  numToShow?: number;
  fromDate?: string;
  toDate?: string;
}): Promise<LogEntryDto[]> {
  const response = await axios.get('/api/admin/logs', { params });
  return response.data.logLines;
}

async getOnlineVotingAnalytics(numToShow: number = 50): Promise<OnlineVotingAnalyticsDto[]> {
  const response = await axios.get('/api/admin/online-voting-analytics', {
    params: { numToShow }
  });
  return response.data.logLines;
}

async getElectionList(numToShow: number = 50): Promise<ElectionSummaryDto[]> {
  const response = await axios.get('/api/admin/elections', {
    params: { numToShow }
  });
  return response.data.logLines;
}

async getUnconnectedVoters(): Promise<UnconnectedVoterDto[]> {
  const response = await axios.get('/api/admin/unconnected-voters');
  return response.data.logLines;
}
```

---

## Related Documentation

- **Database**: See `database/entities.md` for C_Log, Election, OnlineVotingInfo schemas
- **Authentication**: See `security/authentication.md` for SysAdmin authorization
- **UI**: See `ui-screenshots-analysis.md` for System Administration dashboard
- **Controllers**: Cross-references all other controllers via logging
