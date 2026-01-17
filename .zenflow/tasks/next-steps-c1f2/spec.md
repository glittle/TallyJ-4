# Technical Specification: TallyJ4 Phase 3 - Tally Algorithm Implementation

## 1. Technical Context

### 1.1 Technology Stack
- **Runtime**: .NET 10.0
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 10.0
- **Database**: SQL Server (with Sqlite support)
- **Testing**: xUnit, Moq, Microsoft.EntityFrameworkCore.InMemory
- **Logging**: Serilog
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens

### 1.2 Project Structure
```
backend/
├── TallyJ4.Domain/
│   ├── Entities/          # Database entity models
│   │   ├── Result.cs
│   │   ├── ResultSummary.cs
│   │   ├── ResultTie.cs
│   │   ├── Election.cs
│   │   ├── Ballot.cs
│   │   ├── Vote.cs
│   │   └── Person.cs
│   └── Interfaces/        # Domain interfaces
├── Services/
│   ├── ITallyService.cs   # Service interface
│   ├── TallyService.cs    # Service implementation
│   └── Analyzers/         # Algorithm classes
│       ├── ElectionAnalyzerBase.cs
│       ├── ElectionAnalyzerNormal.cs
│       └── ElectionAnalyzerSingleName.cs
├── Controllers/
│   └── ResultsController.cs
├── DTOs/Results/
│   ├── TallyResultDto.cs
│   ├── TallyStatisticsDto.cs
│   └── TieInfoDto.cs
└── EF/Context/
    └── MainDbContext.cs

TallyJ4.Tests/
├── UnitTests/
│   └── TallyServiceTests.cs
└── IntegrationTests/
    └── ResultsControllerTests.cs (to be created)
```

### 1.3 Existing Patterns and Conventions

**Service Layer Pattern**:
- Interface-based services (`ITallyService`)
- Dependency injection via constructor
- Async/await for all database operations
- ILogger for structured logging

**Controller Pattern**:
- Attribute-based routing (`[Route("api/[controller]")]`)
- JWT authorization (`[Authorize]`)
- ActionResult<T> return types
- Try-catch with specific exception handling
- Structured logging with LogInformation/LogWarning/LogError

**DTO Pattern**:
- Separate DTOs for request/response
- AutoMapper for entity-DTO mapping (where applicable)
- Nested DTOs for complex structures

**Testing Pattern**:
- ServiceTestBase for unit tests (in-memory database)
- xUnit with Fact attributes
- Moq for mocking dependencies
- Arrange-Act-Assert pattern

---

## 2. Current Implementation Status

### 2.1 What's Already Implemented ✅

The following components are already in place:

**Core Algorithm Classes**:
1. `ElectionAnalyzerBase` - Base class with template method pattern
   - PrepareForAnalysisAsync() - Loads ballots, votes, people
   - CalculateBallotStatistics() - Computes ballot counts
   - CountVotesAsync() - Abstract method for vote counting
   - FinalizeResultsAndTies() - Ranks candidates, detects ties, assigns sections
   - FinalizeSummariesAsync() - Saves ResultSummary
   - SaveResultsAsync() - Commits to database

2. `ElectionAnalyzerNormal` - Normal election implementation
   - Counts one vote per candidate per ballot
   - Only processes ballots with StatusCode = "Ok"
   - Only counts votes with StatusCode = "Ok"

3. `ElectionAnalyzerSingleName` - Single-name election implementation
   - Uses Vote.SingleNameElectionCount field
   - Allows multiple votes for same candidate on one ballot

**Service Layer**:
- `ITallyService` interface with 4 methods
- `TallyService` implementation
  - CalculateNormalElectionAsync()
  - CalculateSingleNameElectionAsync()
  - GetTallyResultsAsync()
  - GetTallyStatisticsAsync()

**API Layer**:
- `ResultsController` with 4 endpoints:
  - POST `/api/results/election/{electionGuid}/calculate`
  - GET `/api/results/election/{electionGuid}`
  - GET `/api/results/election/{electionGuid}/summary`
  - GET `/api/results/election/{electionGuid}/final`

**DTOs**:
- `TallyResultDto` - Complete tally results
- `CandidateResultDto` - Individual candidate result
- `TallyStatisticsDto` - Ballot and vote statistics
- `TieInfoDto` - Tie group information

**Unit Tests** (7 tests):
- Normal election calculation
- Single-name election calculation
- Tie detection
- Section categorization
- Get results
- Get statistics
- Exception handling

### 2.2 What Needs Enhancement 🔧

Based on requirements analysis, the following may need verification or enhancement:

1. **ResultTie Records**: The current implementation detects ties but may not create ResultTie records (FR-10)
2. **Recalculation Safety**: Need to verify that previous Results are cleared before recalculation (FR-18)
3. **Transaction Safety**: Need to ensure tally calculation runs in a transaction (NFR-5)
4. **Integration Tests**: No integration tests for ResultsController endpoints yet (NFR-10)
5. **Edge Case Tests**: Additional unit tests for edge cases (NFR-9)
6. **Logging**: Verify comprehensive logging throughout (NFR-7)
7. **Result Deletion**: Verify old results are cleared on recalculation

---

## 3. Implementation Approach

### 3.1 Core Algorithm Design

The tally algorithm follows a **template method pattern** with the following workflow:

```
AnalyzeAsync() [ElectionAnalyzerBase]
├── 1. PrepareForAnalysisAsync()
│   ├── Load Locations for election
│   ├── Load Ballots for locations
│   ├── Load Votes for ballots
│   ├── Load People for election
│   └── Load or create ResultSummary
│
├── 2. CalculateBallotStatistics()
│   ├── Count total ballots
│   ├── Count spoiled ballots (StatusCode != "Ok")
│   ├── Count ballots needing review
│   └── Calculate spoiled votes
│
├── 3. CountVotesAsync() [abstract - implemented by subclasses]
│   ├── ElectionAnalyzerNormal: Count 1 vote per candidate
│   └── ElectionAnalyzerSingleName: Sum SingleNameElectionCount
│
├── 4. FinalizeResultsAndTies()
│   ├── Group results by vote count
│   ├── Assign ranks (tied candidates get same rank)
│   ├── Detect ties (groups with 2+ candidates)
│   ├── Assign sections (E = Elected, X = Extra, O = Other)
│   ├── Determine tie-break requirements
│   └── Calculate CloseToNext/CloseToPrev
│
├── 5. FinalizeSummariesAsync()
│   └── Update or insert ResultSummary record
│
└── 6. SaveResultsAsync()
    └── Commit transaction
```

### 3.2 Vote Counting Logic

**Normal Election** (ElectionAnalyzerNormal):
```csharp
foreach ballot in valid ballots:
    foreach vote in ballot.votes:
        if vote.StatusCode == "Ok" and person.CanReceiveVotes:
            result[vote.PersonGuid].VoteCount += 1
```

**Single-Name Election** (ElectionAnalyzerSingleName):
```csharp
foreach ballot in valid ballots:
    foreach vote in ballot.votes:
        if vote.StatusCode == "Ok" and person.CanReceiveVotes:
            result[vote.PersonGuid].VoteCount += vote.SingleNameElectionCount ?? 1
```

### 3.3 Ranking and Section Assignment

**Ranking**:
- Group candidates by vote count (descending)
- Assign rank starting at 1
- Tied candidates receive same rank
- Next rank skips by number of tied candidates (e.g., 1, 1, 3, 4)

**Section Assignment**:
- **Elected (E)**: rank ≤ NumberToElect
- **Extra (X)**: NumberToElect < rank ≤ NumberToElect + NumberExtra
- **Other (O)**: rank > NumberToElect + NumberExtra

**Tie-Break Requirement**:
- Tie spans section boundaries → TieBreakRequired = true
- Tie within a section → TieBreakRequired = false

**Example**:
```
NumberToElect = 9, NumberExtra = 2

Rank  Votes  Section  TieBreakRequired
1     45     E        false
2     42     E        false
...
9     28     E        false  (tied)
9     28     E        false  (tied)
11    27     X        true   (spans E/X boundary)
12    25     X        false
13    20     O        false
```

### 3.4 Tie Detection Algorithm

```csharp
var groupedByVotes = results
    .GroupBy(r => r.VoteCount)
    .OrderByDescending(g => g.Key);

foreach (var group in groupedByVotes)
{
    var isTied = group.Count() > 1;
    
    if (isTied)
    {
        var sections = group.Select(r => r.Section).Distinct();
        var tieBreakRequired = sections.Count() > 1;
        
        foreach (var result in group)
        {
            result.IsTied = true;
            result.TieBreakGroup = tieGroupNumber;
            result.TieBreakRequired = tieBreakRequired;
        }
        
        // Create ResultTie record if needed
        if (tieBreakRequired)
        {
            CreateResultTieRecord(group);
        }
        
        tieGroupNumber++;
    }
}
```

### 3.5 Statistics Calculation

**Ballot Statistics** (from ResultSummary):
- `TotalBallots` = BallotsReceived + SpoiledBallots
- `BallotsReceived` = Ballots with StatusCode = "Ok"
- `SpoiledBallots` = Ballots with StatusCode != "Ok"
- `BallotsNeedingReview` = Ballots flagged for review

**Vote Statistics**:
- `TotalVotes` = TotalBallots × NumberToElect
- `ValidVotes` = TotalVotes - SpoiledVotes
- `InvalidVotes` = SpoiledVotes

**Election Context**:
- `NumVoters` = Count of People with CanVote = true
- `NumEligibleCandidates` = Count of People with CanReceiveVotes = true
- `NumberToElect` = From Election.NumberToElect
- `NumberExtra` = From Election.NumberExtra

---

## 4. Data Model Changes

### 4.1 Existing Entities

All required entities already exist:

**Result** (primary tally results):
- ElectionGuid, PersonGuid (foreign keys)
- VoteCount (nullable int)
- Rank (int)
- Section (string: "E", "X", "O")
- IsTied, TieBreakGroup, TieBreakRequired (nullable bool/int)
- CloseToPrev, CloseToNext (nullable bool)
- IsTieResolved, TieBreakCount (for future tie resolution)

**ResultSummary** (election statistics):
- ElectionGuid (foreign key)
- ResultType (string)
- BallotsReceived, SpoiledBallots, BallotsNeedingReview
- TotalVotes, SpoiledVotes
- NumVoters, NumEligibleToVote
- Various ballot type counts (Mailed, DroppedOff, InPerson, Online, etc.)

**ResultTie** (tie group records):
- ElectionGuid, TieBreakGroup (composite unique index)
- TieBreakRequired (bool)
- NumToElect, NumInTie (int)
- IsResolved (bool, for future use)

### 4.2 No Schema Changes Required ✅

The existing schema fully supports Phase 3 requirements. No migrations needed.

---

## 5. API Contract

### 5.1 Existing Endpoints

All required endpoints are already implemented:

#### POST `/api/results/election/{electionGuid}/calculate`
**Purpose**: Calculate or recalculate tally results  
**Authorization**: JWT (Admin or Teller)  
**Query Parameters**:
- `electionType` (optional): "normal" (default) or "singlename"

**Response** (200 OK):
```json
{
  "electionGuid": "uuid",
  "electionName": "string",
  "calculatedAt": "datetime",
  "statistics": {
    "totalBallots": 100,
    "ballotsReceived": 98,
    "spoiledBallots": 2,
    "ballotsNeedingReview": 3,
    "totalVotes": 882,
    "validVotes": 870,
    "invalidVotes": 12,
    "numVoters": 100,
    "numEligibleCandidates": 30,
    "numberToElect": 9,
    "numberExtra": 2
  },
  "results": [
    {
      "personGuid": "uuid",
      "fullName": "Smith, John",
      "voteCount": 85,
      "rank": 1,
      "section": "E",
      "isTied": false,
      "tieBreakGroup": null,
      "tieBreakRequired": false,
      "closeToNext": false,
      "closeToPrev": false
    }
  ],
  "ties": [
    {
      "tieBreakGroup": 3,
      "voteCount": 45,
      "tieBreakRequired": true,
      "section": "E",
      "candidateNames": ["Johnson, Mary", "Williams, Bob"]
    }
  ]
}
```

**Error Responses**:
- 404 Not Found: Election not found
- 401 Unauthorized: Not authenticated
- 500 Internal Server Error: Calculation failed

#### GET `/api/results/election/{electionGuid}`
**Purpose**: Retrieve calculated results  
**Authorization**: JWT  
**Response**: Same as calculate endpoint (200 OK or 404)

#### GET `/api/results/election/{electionGuid}/summary`
**Purpose**: Retrieve statistics only  
**Authorization**: JWT  
**Response** (200 OK):
```json
{
  "totalBallots": 100,
  "ballotsReceived": 98,
  "spoiledBallots": 2,
  "ballotsNeedingReview": 3,
  "totalVotes": 882,
  "validVotes": 870,
  "invalidVotes": 12,
  "numVoters": 100,
  "numEligibleCandidates": 30,
  "numberToElect": 9,
  "numberExtra": 2
}
```

#### GET `/api/results/election/{electionGuid}/final`
**Purpose**: Retrieve only Elected (E) and Extra (X) results  
**Authorization**: JWT  
**Response**: TallyResultDto with filtered results (200 OK or 404)

### 5.2 No Breaking Changes ✅

All endpoints maintain compatibility with Phase 2 API contracts.

---

## 6. Source Code Structure Changes

### 6.1 Existing Files (No Changes Needed)

The following files are already implemented and functional:

**Domain Layer**:
- `TallyJ4.Domain/Entities/Result.cs` ✅
- `TallyJ4.Domain/Entities/ResultSummary.cs` ✅
- `TallyJ4.Domain/Entities/ResultTie.cs` ✅

**Service Layer**:
- `Services/ITallyService.cs` ✅
- `Services/TallyService.cs` ✅
- `Services/Analyzers/ElectionAnalyzerBase.cs` ✅
- `Services/Analyzers/ElectionAnalyzerNormal.cs` ✅
- `Services/Analyzers/ElectionAnalyzerSingleName.cs` ✅

**Controller Layer**:
- `Controllers/ResultsController.cs` ✅

**DTO Layer**:
- `DTOs/Results/TallyResultDto.cs` ✅
- `DTOs/Results/TallyStatisticsDto.cs` ✅
- `DTOs/Results/TieInfoDto.cs` ✅

**Test Layer**:
- `TallyJ4.Tests/UnitTests/TallyServiceTests.cs` ✅ (7 tests)

### 6.2 Enhancements Needed

The following enhancements may be required:

1. **ElectionAnalyzerBase.cs**: 
   - Add ResultTie record creation in FinalizeResultsAndTies()
   - Clear previous Results before recalculation
   - Wrap AnalyzeAsync() in transaction

2. **TallyServiceTests.cs**:
   - Add edge case tests (FR-14 to FR-18)
   - Add tie-break requirement tests
   - Add recalculation idempotency test

3. **ResultsControllerTests.cs** (new file):
   - Add integration tests for all 4 endpoints
   - Test authentication/authorization
   - Test error handling

---

## 7. Delivery Phases

### Phase 3.1: Verification and Testing ✅ (Current Phase)
**Goal**: Verify existing implementation meets all requirements

**Tasks**:
1. Code review of analyzer classes
2. Verify ResultTie record creation
3. Verify recalculation clears old results
4. Add transaction wrapper
5. Add edge case unit tests (8+ new tests)
6. Create integration tests for ResultsController

**Deliverables**:
- 15+ unit tests passing
- 5+ integration tests passing
- Code review documentation

**Verification**:
- Run: `dotnet test`
- Review test coverage
- Manual API testing via Swagger

### Phase 3.2: Enhancements (If Needed)
**Goal**: Fix any gaps identified in Phase 3.1

**Tasks**:
1. Implement missing ResultTie creation
2. Add transaction support to AnalyzeAsync()
3. Implement result clearing on recalculation
4. Fix any failing tests

**Deliverables**:
- All tests passing
- All NFRs met

**Verification**:
- Run: `dotnet test`
- Performance test with 100 ballots
- Accuracy test with known results

### Phase 3.3: Documentation and Validation
**Goal**: Ensure phase is production-ready

**Tasks**:
1. Add XML documentation comments
2. Update Swagger documentation
3. Validate against legacy system (if available)
4. Performance testing

**Deliverables**:
- Updated API documentation
- Performance test results
- Accuracy validation report

**Verification**:
- Build succeeds: `dotnet build`
- No warnings
- Swagger UI displays correctly

---

## 8. Verification Approach

### 8.1 Unit Testing Strategy

**Test Framework**: xUnit + Moq + InMemory Database

**Test Categories**:
1. **Normal Election Tests**:
   - Basic vote counting
   - Spoiled ballot handling
   - Invalid vote handling
   - Result ranking
   - Section assignment

2. **Single-Name Election Tests**:
   - SingleNameElectionCount summing
   - Multiple votes for same candidate

3. **Tie Detection Tests**:
   - Ties within Elected section (no tie-break)
   - Ties within Extra section (no tie-break)
   - Ties within Other section (no tie-break)
   - Ties spanning E/X boundary (tie-break required)
   - Ties spanning X/O boundary (tie-break required)
   - All candidates tied

4. **Edge Case Tests**:
   - Zero ballots
   - Single candidate
   - Ballot with zero valid votes
   - Recalculation idempotency

5. **Statistics Tests**:
   - Ballot counts
   - Vote counts
   - Average votes per ballot

**Target**: 15+ unit tests (currently 7)

### 8.2 Integration Testing Strategy

**Test Framework**: Microsoft.AspNetCore.Mvc.Testing + xUnit

**Test Scenarios**:
1. POST /calculate with valid election
2. POST /calculate with invalid election (404)
3. POST /calculate without authentication (401)
4. GET /results with existing results
5. GET /summary with calculated results
6. GET /final returns only E and X sections

**Target**: 6+ integration tests

### 8.3 Manual Testing via Swagger

**Test Workflow**:
1. Login as admin@tallyj.test
2. Get existing election GUID
3. POST /calculate with election GUID
4. GET /results to verify
5. GET /summary to verify statistics
6. GET /final to verify filtered results
7. POST /calculate again (test recalculation)

### 8.4 Performance Testing

**Test Scenario**: Calculate results for 100 ballots

**Acceptance Criteria**: < 1 second (NFR-1)

**Test Approach**:
```csharp
[Fact]
public async Task CalculateNormalElectionAsync_With100Ballots_CompletesInUnder1Second()
{
    var election = await CreateTestElectionAsync();
    var location = await CreateTestLocationAsync(election.ElectionGuid);
    var people = await CreateTestPeopleAsync(election.ElectionGuid, 30);
    var ballots = await CreateTestBallotsAsync(location.LocationGuid, 100);
    await CreateTestVotesAsync(ballots, people);
    
    var stopwatch = Stopwatch.StartNew();
    var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);
    stopwatch.Stop();
    
    Assert.True(stopwatch.ElapsedMilliseconds < 1000);
}
```

### 8.5 Accuracy Validation

**Approach**: Compare with known results

**Test Data Sources**:
1. Seeded test data in database
2. Manually calculated results
3. Legacy TallyJ results (if available)

**Validation Points**:
- Vote counts match expected
- Ranks match expected
- Sections match expected
- Ties detected correctly
- TieBreakRequired flags correct
- Statistics accurate

---

## 9. Dependencies and Integration Points

### 9.1 Upstream Dependencies (External)
- **EF Core 10.0**: Database operations
- **ASP.NET Core Identity**: Authentication (existing)
- **Serilog**: Logging (existing)
- **AutoMapper**: DTO mapping (if needed)

### 9.2 Internal Dependencies
- **Election entity**: NumberToElect, NumberExtra, ElectionType
- **Person entity**: CanReceiveVotes, CombinedInfo
- **Ballot entity**: StatusCode, LocationGuid
- **Vote entity**: PersonGuid, StatusCode, SingleNameElectionCount
- **Location entity**: ElectionGuid

### 9.3 Downstream Dependencies
- **Phase 4 (Frontend)**: Will consume these API endpoints
- **Phase 5 (SignalR)**: May broadcast tally progress
- **Reporting**: Will use tally results for reports

---

## 10. Error Handling and Logging

### 10.1 Error Scenarios

**Election Not Found**:
```csharp
if (election == null)
{
    throw new ArgumentException($"Election {electionGuid} not found");
}
```
Controller returns 404 with message.

**Database Errors**:
Caught by GlobalExceptionHandler, logged, returns 500.

**Calculation Errors**:
Logged and re-thrown to be handled by GlobalExceptionHandler.

### 10.2 Logging Strategy

**Current Logging** (already implemented):
```csharp
Logger.LogInformation("Starting normal election tally calculation for election {ElectionGuid}", electionGuid);
Logger.LogInformation("Loaded {BallotCount} ballots, {VoteCount} votes, {PeopleCount} people", ...);
Logger.LogInformation("Processed {BallotCount} ballots ({VoteCount} votes)", ...);
Logger.LogInformation("Finalized {ResultCount} results, {TieCount} tie groups", ...);
Logger.LogInformation("Completed tally calculation for election {ElectionGuid}", electionGuid);
```

**Error Logging**:
```csharp
Logger.LogWarning(ex, "Election {ElectionGuid} not found", electionGuid);
Logger.LogError(ex, "Error calculating tally for election {ElectionGuid}", electionGuid);
```

**Sufficient for NFR-7** ✅

---

## 11. Performance Considerations

### 11.1 Database Optimization

**Batch Loading** (already implemented):
- Load all ballots in one query
- Load all votes in one query
- Load all people in one query

**Indexes** (already exist):
- `IX_Result_Election` on Result.ElectionGuid
- `IX_VoteBallot` on Vote.BallotGuid
- `IX_VotePerson` on Vote.PersonGuid
- `IX_Ballot_Location` on Ballot.LocationGuid

**Transaction Management**:
- Single transaction for entire calculation
- SaveChangesAsync() called only twice (during CountVotesAsync and at end)

### 11.2 Memory Optimization

**In-Memory Processing**:
- All entities loaded into Lists for processing
- Avoid repeated database queries
- Trade-off: Higher memory usage for better performance

**Expected Memory Usage**: ~10 MB for 1,000 ballots with 9 votes each

### 11.3 Performance Targets

| Scenario | Target | Expected |
|----------|--------|----------|
| 100 ballots | < 1s | ~200ms |
| 1,000 ballots | < 10s | ~2s |
| Database queries | Minimize | 6-8 queries total |

---

## 12. Security Considerations

### 12.1 Authentication and Authorization

**Current Implementation** ✅:
- All endpoints require JWT authentication (`[Authorize]`)
- Uses ASP.NET Core Identity
- Role-based access (Admin, Teller roles expected)

**Authorization Rules**:
- Only authenticated users can calculate tallies
- Only authenticated users can view results
- No role restrictions on viewing results (may be enhanced in future)

### 12.2 Data Validation

**Input Validation**:
- ElectionGuid format validated by ASP.NET Core routing
- Election existence validated in service layer
- No user input in calculation (all data from database)

**No SQL Injection Risk**: All queries use EF Core parameterization

### 12.3 Audit Trail

**Logging**:
- All tally calculations logged with election GUID
- User context available via HTTP context (not currently logged)

**Future Enhancement**: Add UserId to log context for full audit trail

---

## 13. Testing Verification Commands

### 13.1 Build and Test
```bash
cd backend
dotnet build
dotnet test --verbosity normal
```

**Expected Output**: All tests passing, 0 errors, 0 warnings

### 13.2 Run Application
```bash
cd backend
dotnet run
```

**Verify**: 
- App starts on http://localhost:5000
- Swagger UI available at http://localhost:5000/swagger

### 13.3 Manual API Testing

**Using curl**:
```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"TestPass123!"}' \
  | jq -r '.accessToken')

# Get first election GUID from seeded data
# (Use Swagger or database query to find actual GUID)

# Calculate tally
curl -X POST "http://localhost:5000/api/results/election/{GUID}/calculate" \
  -H "Authorization: Bearer $TOKEN" \
  | jq .

# Get results
curl -X GET "http://localhost:5000/api/results/election/{GUID}" \
  -H "Authorization: Bearer $TOKEN" \
  | jq .

# Get summary
curl -X GET "http://localhost:5000/api/results/election/{GUID}/summary" \
  -H "Authorization: Bearer $TOKEN" \
  | jq .
```

---

## 14. Acceptance Criteria Verification

| Requirement | Implementation Status | Verification Method |
|-------------|----------------------|---------------------|
| FR-1: Normal election tally | ✅ Implemented | ElectionAnalyzerNormal tests |
| FR-2: Single-name election tally | ✅ Implemented | ElectionAnalyzerSingleName tests |
| FR-3: Ballot statistics | ✅ Implemented | TallyStatisticsDto tests |
| FR-4: Vote statistics | ✅ Implemented | TallyStatisticsDto tests |
| FR-5: Rank candidates | ✅ Implemented | Section categorization test |
| FR-6: Section assignment | ✅ Implemented | Section categorization test |
| FR-7: Section display logic | ✅ Implemented | GET /final endpoint |
| FR-8: Detect ties | ✅ Implemented | Tie detection test |
| FR-9: Tie-break requirement | ✅ Implemented | Code review needed |
| FR-10: Record ties | 🔧 Needs verification | Add ResultTie creation test |
| FR-11: Calculate results endpoint | ✅ Implemented | Integration test needed |
| FR-12: Get results endpoint | ✅ Implemented | Integration test needed |
| FR-13: Get summary endpoint | ✅ Implemented | Integration test needed |
| FR-14: Handle zero ballots | 🔧 Needs test | Add edge case test |
| FR-15: Handle all tied | 🔧 Needs test | Add edge case test |
| FR-16: Handle single candidate | 🔧 Needs test | Add edge case test |
| FR-17: Ballot with zero votes | 🔧 Needs test | Add edge case test |
| FR-18: Recalculation safety | 🔧 Needs verification | Add idempotency test |
| NFR-1: Tally speed < 1s | 🔧 Needs test | Add performance test |
| NFR-3: 100% accuracy | 🔧 Needs validation | Manual validation with known results |
| NFR-4: Idempotency | 🔧 Needs test | Add recalculation test |
| NFR-5: Transaction safety | 🔧 Needs implementation | Wrap in transaction |
| NFR-6: Code organization | ✅ Implemented | Code review |
| NFR-7: Logging | ✅ Implemented | Log output review |
| NFR-9: 15+ unit tests | 🔧 7 tests exist | Add 8+ more tests |
| NFR-10: Integration tests | 🔧 Needs creation | Create ResultsControllerTests.cs |

**Legend**: ✅ Complete | 🔧 Needs work

---

## 15. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Missing ResultTie records | Medium | High | Add creation logic and tests |
| Results not cleared on recalc | Medium | High | Add deletion before insert |
| Transaction rollback fails | Low | High | Test error scenarios |
| Performance under load | Low | Medium | Add performance test |
| Accuracy mismatch | Low | High | Validate against legacy |
| Test coverage gaps | High | Medium | Add edge case tests |

---

## 16. Next Steps (Planning Phase)

After this specification is approved, the Planning phase will:

1. Break down tasks into concrete implementation steps
2. Define test scenarios for each edge case
3. Create ResultTie record creation logic
4. Add transaction wrapper
5. Implement result clearing on recalculation
6. Write 8+ additional unit tests
7. Write 6+ integration tests
8. Perform manual validation
9. Update plan.md with progress

---

## 17. References

- **Requirements**: `.zenflow/tasks/next-steps-c1f2/requirements.md`
- **Existing Code**: `backend/Services/Analyzers/`, `backend/Controllers/ResultsController.cs`
- **Tests**: `TallyJ4.Tests/UnitTests/TallyServiceTests.cs`
- **Domain Entities**: `backend/TallyJ4.Domain/Entities/`
- **Phase 2 Report**: `report.md`
- **Reverse Engineering Docs**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/business-logic/tally-algorithms.md`

---

**Document Status**: Ready for Planning Phase  
**Next Step**: Create detailed implementation plan in `plan.md`  
**Estimated Effort**: 2-3 days (mostly testing and validation)
