# AfterController API Documentation

## Overview
**Purpose**: Post-election analysis, tally processing, monitoring, and reporting  
**Base Route**: `/After`  
**Authorization**: `[AllowTellersInActiveElection]` at controller level  
**Authentication**: Requires authenticated teller (admin or guest) with active election session

## Context

This controller handles all "after voting" activities:
- Running the tally algorithm to count votes
- Monitoring election progress across multiple tellers
- Generating reports (results, tie-breakers, detailed breakdowns)
- Managing tie-break elections
- Processing online ballots
- Finalizing election results

---

## Endpoints

### 1. GET `/After/Index`
**Purpose**: Display main "After" page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View named "After"

**Business Logic**:
- None (view only)

**Notes**:
- Landing page for post-election activities
- Links to Analyze, Monitor, Reports pages

---

### 2. GET `/After/Analyze`
**Purpose**: Display tally analysis page  
**HTTP Method**: GET  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: View with `ResultsModel`

**Business Logic**:
- Instantiate `ResultsModel` (loads current election results)
- Pass model to view

**Response Model**:
```csharp
ResultsModel {
  // Contains election results, tie information, vote counts
}
```

**Notes**:
- View displays "Run Analyze" button that calls `POST /After/RunAnalyze`
- Guest tellers cannot access (requires Known Teller)

---

### 3. GET `/After/Reports`
**Purpose**: Display reports page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]` (controller-level)  
**Returns**: View named "Reports"

**Business Logic**:
- None (view only)

**Notes**:
- View calls `/After/GetReportData` to fetch report content

---

### 4. GET `/After/Presenter`
**Purpose**: Display results presentation view (projector mode)  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View

**Business Logic**:
- None (view only)

**Notes**:
- Used for displaying results on a projector/screen
- Read-only view of final results

---

### 5. GET `/After/ShowTies`
**Purpose**: Display tie-breaker management page  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View with `ResultsModel`

**Business Logic**:
- Instantiate `ResultsModel`
- Pass model to view

**Notes**:
- Displays candidates involved in ties
- Allows entry of tie-break vote counts

---

### 6. GET `/After/Monitor`
**Purpose**: Display real-time election monitoring dashboard  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: View with `MonitorModel`

**Business Logic**:
- Instantiate `MonitorModel`
- Pass model to view

**Response Model**:
```csharp
MonitorModel {
  // Contains:
  // - Connected tellers/computers
  // - Vote entry progress per location
  // - Online voting status
  // - Ballot counts
  // - Real-time activity updates
}
```

**Notes**:
- Used by head teller to monitor multiple tellers working simultaneously
- View polls `/After/RefreshMonitor` for updates
- Critical for coordinating distributed vote entry

---

### 7. POST `/After/RefreshMonitor`
**Purpose**: Get updated monitoring data (AJAX polling endpoint)  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with monitoring data

**Response**:
```json
{
  "Computers": [
    {
      "ComputerCode": "A",
      "LocationName": "Main Hall",
      "BallotCount": 45,
      "LastContact": "2024-01-01T12:34:56",
      "Status": "Active"
    }
  ],
  "Locations": [ /* Location vote counts */ ],
  "OnlineVotingInfo": { /* Online voting status */ },
  "TotalBallots": 150,
  "TotalVotes": 1350
}
```

**Business Logic**:
1. Call `ComputerModel().RefreshLastContact()` to update current computer's timestamp
2. Call `MonitorModel().MonitorInfo` to get updated statistics
3. Return as JSON

**SignalR Integration**:
- Works alongside `MainHub` for real-time updates
- Polling provides fallback if SignalR connection drops

**Notes**:
- Called every 5-10 seconds by frontend
- Updates "Last Contact" for current computer

---

### 8. POST `/After/RunAnalyze`
**Purpose**: Execute tally algorithm to count votes and determine results  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with results

**Response**:
```json
{
  "Results": [
    {
      "PersonId": 123,
      "FullName": "John Smith",
      "VoteCount": 45,
      "Rank": 1,
      "Section": "Elected",
      "IsTied": false,
      "TieBreakGroup": null
    },
    // ...more results
  ],
  "Summary": {
    "TotalBallots": 150,
    "SpoiledBallots": 3,
    "TotalVotes": 1347,
    "NumToElect": 9,
    "NumElected": 9,
    "HasTies": false
  }
}
```

**Business Logic**:
1. Instantiate `ResultsModel`
2. Call `resultsModel.GenerateResults()` which:
   - Retrieves all ballots and votes for current election
   - Runs tally algorithm (documented in `business-logic/tally-algorithms.md`)
   - Detects ties
   - Calculates result sections (Elected, Extra, Other)
   - Saves results to `Result` and `ResultSummary` tables
   - Updates election status
3. Call `resultsModel.GetCurrentResults()` to retrieve formatted results
4. Return as JSON

**SignalR Hub Triggered**:
- **AnalyzeHub**: Broadcasts progress updates during tally
  - `updateResults` - Progress percentage every 10 ballots
  - `analysisComplete` - Final results when complete

**Side Effects**:
- Writes to `Result` table (one row per candidate)
- Writes to `ResultSummary` table (election-level summary)
- Writes to `ResultTie` table (if ties detected)
- Updates `Election.TallyStatus`

**Performance**:
- Processing ~150 ballots with 1350 votes takes ~1-2 seconds
- Algorithm complexity: O(n) where n = number of votes

**Notes**:
- Guest tellers cannot run analyze (requires Known Teller)
- Can be run multiple times (results are recalculated, not appended)
- Must be run after all ballots entered and before finalization

---

### 9. POST `/After/GetTies`
**Purpose**: Retrieve tie information for a specific tie-break group  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with tie details

**Request**:
```
POST /After/GetTies?tieBreakGroup=1
```

**Response**:
```json
{
  "TieBreakGroup": 1,
  "Section": "Elected",
  "Candidates": [
    {
      "PersonId": 101,
      "FullName": "Alice Johnson",
      "VoteCount": 45,
      "TieBreakCount": null
    },
    {
      "PersonId": 102,
      "FullName": "Bob Williams",
      "VoteCount": 45,
      "TieBreakCount": null
    }
  ],
  "Instructions": "Enter tie-break vote counts for these candidates"
}
```

**Business Logic**:
- Call `ResultsModel().GetTies(tieBreakGroup)`
- Retrieve candidates in specified tie group from `ResultTie` table

**Notes**:
- Used by ShowTies page to display candidates needing tie-break
- TieBreakGroup is assigned during RunAnalyze if ties detected

---

### 10. POST `/After/SaveTieCounts`
**Purpose**: Save tie-break vote counts entered by teller  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with success status

**Request**:
```json
{
  "counts": [
    "101:38",  // PersonId:TieBreakCount
    "102:42"
  ]
}
```

**Response**:
```json
{
  "Success": true,
  "Message": "Tie counts saved successfully"
}
```

**Business Logic**:
1. Parse counts list (format: "PersonId:Count")
2. Update `ResultTie.TieBreakCount` for each person
3. Call `ResultsModel().SaveTieCounts(counts)`
4. May trigger re-analysis if all tie-breaks resolved

**Side Effects**:
- Updates `ResultTie` table
- May update `Result` table if re-ranking needed

**Notes**:
- Tie-break counts come from a second physical vote
- Used when original vote results in exact ties
- Guest tellers cannot save tie counts

---

### 11. POST `/After/GetReport`
**Purpose**: Retrieve final election results in JSON format  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JsonResult with complete results

**Response**:
```json
{
  "ElectionName": "2024 LSA Election",
  "ElectionDate": "2024-01-15",
  "NumToElect": 9,
  "TotalBallots": 150,
  "SpoiledBallots": 3,
  "TotalVotes": 1347,
  "Elected": [
    {
      "Rank": 1,
      "FullName": "John Smith",
      "VoteCount": 48,
      "Section": "Elected"
    }
    // ...8 more elected
  ],
  "Extra": [ /* Candidates with votes close to elected */ ],
  "Other": [ /* All other candidates with votes */ ],
  "Ties": [ /* Tie information if any */ ]
}
```

**Business Logic**:
- Call `ResultsModel().FinalResultsJson`
- Retrieves cached/calculated final results

**Notes**:
- Used by Reports page to display results
- Results are read-only after election finalized
- Includes all result sections and tie information

---

### 12. POST `/After/GetReportData`
**Purpose**: Retrieve specific report data by report code  
**HTTP Method**: POST  
**Authorization**: `[AllowTellersInActiveElection]` (commented out - allows all)  
**Returns**: JsonResult with report data

**Request**:
```
POST /After/GetReportData?code=ballots
```

**Report Codes**:
- `ballots` - Ballot-by-ballot detail
- `voters` - Voter participation report
- `locations` - Vote counts by location
- `summary` - Election summary
- `ties` - Tie details

**Response** (example for code="ballots"):
```json
{
  "Ballots": [
    {
      "BallotId": 1,
      "LocationName": "Main Hall",
      "Status": "Ok",
      "Votes": [
        { "FullName": "Alice Johnson", "Position": 1 },
        { "FullName": "Bob Williams", "Position": 2 }
        // ...up to 9 votes
      ]
    }
    // ...more ballots
  ]
}
```

**Business Logic**:
- Call `ResultsModel().GetReportData(code)`
- Generate report based on code

**Notes**:
- Used by Reports page to fetch different report types
- Each report code triggers different data aggregation

---

### 13. POST `/After/UpdateElectionShowAll`
**Purpose**: Toggle display of all candidates vs. only those with votes  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with success status

**Request**:
```
POST /After/UpdateElectionShowAll?showAll=true
```

**Response**:
```json
{
  "Success": true,
  "ShowAll": true
}
```

**Business Logic**:
- Call `ElectionHelper().UpdateElectionShowAllJson(showAll)`
- Update `Election.ShowFullReport` field

**Side Effects**:
- Updates `Election` table

**Notes**:
- Comment in code suggests this may be deprecated ("is this used anymore?")

---

### 14. POST `/After/UpdateListing`
**Purpose**: Toggle whether election appears in public listing  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with success status

**Request**:
```
POST /After/UpdateListing?listOnPage=true
```

**Response**:
```json
{
  "Success": true,
  "ListOnPage": true
}
```

**Business Logic**:
- Call `ElectionHelper().UpdateListOnPageJson(listOnPage)`
- Update `Election.ListForPublic` field

**Side Effects**:
- Updates `Election` table
- May trigger `PublicHub` update to refresh public pages

**Notes**:
- Controls visibility on public-facing pages

---

### 15. POST `/After/SaveOnlineClose`
**Purpose**: Set online voting close time  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with success status

**Request**:
```
POST /After/SaveOnlineClose?when=2024-01-15T18:00:00&est=true
```

**Response**:
```json
{
  "Success": true,
  "CloseTime": "2024-01-15T18:00:00",
  "TimeZone": "EST"
}
```

**Business Logic**:
- Call `ElectionHelper().SaveOnlineClose(when, est)`
- Update `Election.OnlineCloseDateTime` field
- Convert time to UTC if needed

**Side Effects**:
- Updates `Election` table
- May trigger `AllVotersHub` update to show countdown to voters

**Notes**:
- Online voting closes automatically at specified time
- Frontend displays countdown timer to voters

---

### 16. POST `/After/ProcessOnlineBallots`
**Purpose**: Convert raw online votes into counted ballots  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with success status

**Response**:
```json
{
  "Success": true,
  "ProcessedCount": 25,
  "Message": "25 online ballots processed"
}
```

**Business Logic**:
1. Call `ElectionHelper().ProcessOnlineBallots()`
2. For each online voter with `HasVoted=true`:
   - Create `Ballot` record
   - Copy `Vote` records (status changes from `OnlineRaw` to `Ok`)
   - Update vote positions if needed
3. Mark online votes as processed

**Side Effects**:
- Creates `Ballot` records
- Updates `Vote.StatusCode` from `OnlineRaw` to `Ok`
- Updates `OnlineVoter.Processed` flag

**SignalR Hub Triggered**:
- **MainHub**: `onlineVotesProcessed` - Notify tellers of processed count

**Notes**:
- Must be done before running tally analyze
- Online votes have status `OnlineRaw` until processed
- One-time operation per election

---

### 17. POST `/After/SaveManual`
**Purpose**: Manually enter or override election results (for offline tallies)  
**HTTP Method**: POST  
**Authorization**: `[ForAuthenticatedTeller]` (Known tellers only)  
**Returns**: JsonResult with success status

**Request**:
```json
{
  "ElectionGuid": "...",
  "UseOnReports": true,
  "ResultSummaryJson": "{ /* manual results */ }"
}
```

**Response**:
```json
{
  "Success": true,
  "Message": "Manual results saved"
}
```

**Business Logic**:
- Call `ResultsModel().SaveManualResults(manualResults)`
- Save to `ResultSummary` table with manual flag
- Override automated tally results if `UseOnReports=true`

**Side Effects**:
- Updates `ResultSummary` table
- Sets manual results flag

**Notes**:
- Used for elections tallied outside TallyJ (paper-based)
- Manual results displayed instead of automated tally
- Rare use case

---

## Authorization Attributes Used

| Attribute | Endpoints | Purpose |
|-----------|-----------|---------|
| `[AllowTellersInActiveElection]` | All (controller-level) | Require authenticated teller with active election |
| `[ForAuthenticatedTeller]` | RunAnalyze, SaveTieCounts, UpdateListing, SaveOnlineClose, ProcessOnlineBallots, SaveManual | Require Known Teller (not guest) |
| None (commented out) | GetReportData | Originally restricted, now open to all tellers |

---

## Business Logic Classes Called

| Class | Methods | Purpose |
|-------|---------|---------|
| `ResultsModel` | GenerateResults, GetCurrentResults, GetTies, SaveTieCounts, GetReportData, SaveManualResults | Vote tallying and results |
| `MonitorModel` | MonitorInfo | Real-time monitoring data |
| `ComputerModel` | RefreshLastContact | Update computer activity timestamp |
| `ElectionHelper` | UpdateElectionShowAllJson, UpdateListOnPageJson, SaveOnlineClose, ProcessOnlineBallots | Election settings management |

---

## SignalR Hubs Triggered

| Hub | Methods | Triggered By |
|-----|---------|--------------|
| `AnalyzeHub` | updateResults, analysisComplete | RunAnalyze |
| `MainHub` | onlineVotesProcessed | ProcessOnlineBallots |
| `PublicHub` | electionUpdated | UpdateListing |
| `AllVotersHub` | onlineClosing | SaveOnlineClose |

---

## Database Tables Accessed

| Table | Operations | Purpose |
|-------|------------|---------|
| `Ballot` | SELECT | Read ballots for tallying |
| `Vote` | SELECT, UPDATE | Read votes, update status after processing online |
| `Result` | INSERT, UPDATE, DELETE | Store tally results |
| `ResultSummary` | INSERT, UPDATE | Store election-level summary |
| `ResultTie` | INSERT, UPDATE | Store tie information |
| `Election` | SELECT, UPDATE | Read election config, update settings |
| `Person` | SELECT | Read candidate names |
| `Location` | SELECT | Read locations for monitoring |
| `Computer` | SELECT, UPDATE | Track teller activity |
| `OnlineVoter` | SELECT, UPDATE | Process online votes |

---

## Key Workflows

### Tally Workflow
1. Tellers enter all ballots via `/Ballots/Index`
2. Admin calls `/After/ProcessOnlineBallots` (if online voting used)
3. Admin navigates to `/After/Analyze`
4. Admin clicks "Run Analyze" → POST `/After/RunAnalyze`
5. AnalyzeHub broadcasts progress
6. If ties detected → Navigate to `/After/ShowTies`
7. Conduct tie-break vote → POST `/After/SaveTieCounts`
8. Re-run analyze if needed
9. View reports via `/After/Reports` → `/After/GetReportData`
10. Finalize election (separate endpoint)

### Monitoring Workflow
1. Head teller navigates to `/After/Monitor`
2. Frontend polls `/After/RefreshMonitor` every 5 seconds
3. Monitor displays:
   - Active computers/tellers
   - Ballots entered per location
   - Last contact time
   - Online voting status
4. SignalR pushes real-time updates (MainHub, FrontDeskHub)

---

## Migration Notes for .NET Core

### Recommended .NET Core Structure
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "TellerInActiveElection")]
public class ResultsController : ControllerBase
{
  private readonly IResultsService _resultsService;
  private readonly IMonitorService _monitorService;
  private readonly IHubContext<AnalyzeHub> _analyzeHub;

  [HttpPost("analyze")]
  [Authorize(Policy = "KnownTeller")]
  public async Task<ActionResult<ResultsDto>> RunAnalyze() { ... }

  [HttpGet("monitor")]
  public async Task<ActionResult<MonitorDto>> GetMonitorData() { ... }

  [HttpGet("report/{code}")]
  public async Task<ActionResult<ReportDto>> GetReport(string code) { ... }

  [HttpPost("ties")]
  [Authorize(Policy = "KnownTeller")]
  public async Task<IActionResult> SaveTieCounts([FromBody] TieCountsDto dto) { ... }

  [HttpPost("online/process")]
  [Authorize(Policy = "KnownTeller")]
  public async Task<IActionResult> ProcessOnlineBallots() { ... }
}
```

### Key Changes
- Split into focused controllers (ResultsController, MonitorController, ReportsController)
- RESTful routes (e.g., `/api/results/analyze` instead of `/After/RunAnalyze`)
- Policy-based authorization
- Async/await for database operations
- Strongly-typed SignalR hubs
- Separate service layer for business logic

---

## Testing Scenarios

1. **Run Tally**
   - POST `/After/RunAnalyze` with 150 ballots → Results generated
   - Verify AnalyzeHub progress updates
   - Verify Result, ResultSummary, ResultTie tables updated

2. **Tie Handling**
   - Run analyze with tie scenario → Ties detected
   - GET `/After/GetTies?tieBreakGroup=1` → Tied candidates returned
   - POST `/After/SaveTieCounts` → Tie counts saved
   - Re-run analyze → Winners determined

3. **Monitoring**
   - Multiple tellers enter ballots simultaneously
   - GET `/After/Monitor` → Monitor page loads
   - POST `/After/RefreshMonitor` → Updated counts returned
   - Verify Computer.LastContact updated

4. **Online Ballot Processing**
   - 25 voters vote online
   - POST `/After/ProcessOnlineBallots` → 25 ballots created
   - Verify Vote.StatusCode changed from `OnlineRaw` to `Ok`
   - Run analyze → Online votes included in tally

5. **Reports**
   - POST `/After/GetReportData?code=ballots` → Ballot detail returned
   - POST `/After/GetReportData?code=summary` → Summary returned
   - POST `/After/GetReport` → Final results JSON returned

---

## Performance Considerations

1. **Tally Algorithm**: O(n) complexity, ~1-2 seconds for 150 ballots
2. **Monitor Polling**: Every 5 seconds, must be optimized query
3. **Report Generation**: May be slow for large elections (500+ ballots)
4. **SignalR**: Reduces polling frequency, improves real-time UX

**Optimization Recommendations**:
- Cache tally results (invalidate on ballot changes)
- Index Vote.BallotId, Vote.PersonId
- Use SQL views for report aggregations
- Implement background jobs for long-running tallies (>500 ballots)

---

## API Endpoint Summary

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/After/Index` | Main page | Teller |
| GET | `/After/Analyze` | Analyze page | Known Teller |
| GET | `/After/Reports` | Reports page | Teller |
| GET | `/After/Presenter` | Presentation view | Teller |
| GET | `/After/ShowTies` | Tie management | Teller |
| GET | `/After/Monitor` | Monitoring dashboard | Teller |
| POST | `/After/RefreshMonitor` | Get monitor data | Teller |
| POST | `/After/RunAnalyze` | Run tally algorithm | Known Teller |
| POST | `/After/GetTies` | Get tie details | Teller |
| POST | `/After/SaveTieCounts` | Save tie-break counts | Known Teller |
| POST | `/After/GetReport` | Get final results | Teller |
| POST | `/After/GetReportData` | Get specific report | Teller |
| POST | `/After/UpdateElectionShowAll` | Toggle show all | Known Teller |
| POST | `/After/UpdateListing` | Toggle public listing | Known Teller |
| POST | `/After/SaveOnlineClose` | Set online close time | Known Teller |
| POST | `/After/ProcessOnlineBallots` | Process online votes | Known Teller |
| POST | `/After/SaveManual` | Save manual results | Known Teller |

**Total Endpoints**: 17 (6 GET views, 11 POST JSON APIs)
