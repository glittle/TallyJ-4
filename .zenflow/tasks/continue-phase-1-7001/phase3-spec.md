# Technical Specification: TallyJ4 - Phase 3: Tally Algorithm Implementation

## 1. Overview

**Phase**: Phase 3 - Tally Algorithm Implementation  
**Objective**: Implement the core election result calculation algorithms that count votes, detect ties, rank candidates, and generate election results following Bahá'í electoral principles.

**Priority**: CRITICAL - Tally results must match the existing TallyJ system exactly.

---

## 2. Technical Context

### 2.1 Current State (Phase 2 Complete ✅)

**Completed**:
- 8 REST API controllers with full CRUD operations
- DTOs, services, and FluentValidation for all endpoints
- AutoMapper profiles for entity-DTO mapping
- Global error handling and Swagger documentation
- Testing infrastructure with 10+ passing tests
- Results API with basic result retrieval

**Limitations**:
- ❌ No vote counting/tally calculation
- ❌ No tie detection logic
- ❌ No result ranking and categorization
- ❌ Results API returns raw data without calculated statistics

### 2.2 Reference Documentation

**Source**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/business-logic/tally-algorithms.md` (713 lines)

**Key Algorithms Documented**:
1. Normal election tally (LSA 9-member elections)
2. Single-name election tally (single position elections)
3. Tie detection and categorization
4. Ballot validation rules
5. Result sectioning (Elected/Extra/Other)
6. Vote status determination

---

## 3. Implementation Approach

### 3.1 Architecture

**Service Layer**:
```
ITallyService (interface)
  ├─ CalculateNormalElectionAsync(electionGuid)
  ├─ CalculateSingleNameElectionAsync(electionGuid)
  ├─ GetResultsAsync(electionGuid)
  └─ GetResultSummaryAsync(electionGuid)

TallyService (implementation)
  ├─ Uses MainDbContext for data access
  ├─ Uses ILogger for diagnostics
  ├─ Returns TallyResultDto with statistics
```

**Algorithm Classes**:
```
ElectionAnalyzerBase (abstract)
  ├─ PrepareForAnalysis()
  ├─ CalculateBallotStatistics()
  ├─ FinalizeResultsAndTies()
  ├─ FinalizeSummaries()
  └─ SaveResults()

ElectionAnalyzerNormal : ElectionAnalyzerBase
  └─ CountVotes() [override]

ElectionAnalyzerSingleName : ElectionAnalyzerBase
  └─ CountVotes() [override]
```

### 3.2 Core Algorithm Flow

**Normal Election Tally**:
1. **Prepare**: Load ballots, votes, people, clear previous results
2. **Calculate Statistics**: Count spoiled ballots, total votes
3. **Count Votes**: Iterate valid ballots → valid votes → increment Result.VoteCount
4. **Detect Ties**: Group by vote count, assign tie groups
5. **Categorize**: Assign sections (Elected/Extra/Other), determine tie-break requirements
6. **Finalize**: Create/update ResultSummary with statistics
7. **Save**: Persist to database

**Single-Name Election Tally**:
- Same flow but uses `Vote.SingleNameElectionCount` field
- Allows multiple votes for same person on one ballot

### 3.3 Tie Detection Logic

**Tie Scenarios**:
1. **Tie within Elected** → No tie-break needed (all elected)
2. **Tie within Extra** → No tie-break needed (all in extra section)
3. **Tie spanning Elected/Extra boundary** → Tie-break REQUIRED
4. **Tie spanning Extra/Other boundary** → Tie-break REQUIRED

**Implementation**:
- Group results by VoteCount
- Assign ranks (1 = highest)
- Mark `IsTied = true` for groups with count > 1
- Assign `TieBreakGroup` number
- Set `TieBreakRequired = true` if tied candidates span multiple sections

### 3.4 Result Sectioning

**Sections**:
- **Elected**: Ranks 1 to `NumberToElect`
- **Extra**: Ranks `NumberToElect + 1` to `NumberToElect + NumberExtra`
- **Other**: Remaining candidates

---

## 4. Source Code Structure

### 4.1 New Files

```
backend/
├── Services/
│   ├── ITallyService.cs                    [NEW]
│   ├── TallyService.cs                     [NEW]
│   └── Analyzers/                          [NEW]
│       ├── ElectionAnalyzerBase.cs         [NEW]
│       ├── ElectionAnalyzerNormal.cs       [NEW]
│       └── ElectionAnalyzerSingleName.cs   [NEW]
├── DTOs/
│   └── Results/
│       ├── TallyResultDto.cs               [NEW]
│       ├── TallyStatisticsDto.cs           [NEW]
│       └── TieInfoDto.cs                   [NEW]
└── Controllers/
    └── ResultsController.cs                [MODIFY]
```

### 4.2 Modified Files

- `backend/Controllers/ResultsController.cs` - Add tally calculation endpoints
- `backend/Program.cs` - Register ITallyService
- `backend/Services/ResultService.cs` - Enhance with tally integration

### 4.3 Test Files

```
TallyJ4.Tests/
├── UnitTests/
│   ├── TallyServiceTests.cs                [NEW]
│   ├── ElectionAnalyzerNormalTests.cs      [NEW]
│   └── TieDetectionTests.cs                [NEW]
└── IntegrationTests/
    └── ResultsControllerTallyTests.cs      [NEW]
```

---

## 5. Data Model Changes

**No schema changes required** - All necessary fields exist:

**Result Entity**:
- `VoteCount` - Vote count for candidate
- `Rank` - Result ranking (1 = highest)
- `IsTied` - Is this result tied with others?
- `TieBreakGroup` - Tie group number
- `TieBreakRequired` - Does this tie need resolution?
- `Section` - "Elected", "Extra", or "Other"
- `ForceShowInOther` - Force display in reports
- `CloseToNext` - Vote count close to next candidate
- `CloseToPrev` - Vote count close to previous candidate

**ResultSummary Entity**:
- `NumBallots` - Total ballots
- `SpoiledBallots` - Invalid ballots
- `InvalidVotes` - Invalid votes
- `TotalVotes` - Total votes cast
- `BallotsNeedingReview` - Ballots with changed names
- (Plus other statistical fields)

---

## 6. Implementation Tasks

### Phase 3.1: Tally Service Foundation
**Objective**: Create service layer and base analyzer class

**Tasks**:
1. Create `ITallyService` interface
2. Create `TallyService` implementation
3. Create `ElectionAnalyzerBase` abstract class
4. Create DTOs: `TallyResultDto`, `TallyStatisticsDto`
5. Register service in `Program.cs`

**Verification**:
- Build succeeds
- Service can be injected into controllers

---

### Phase 3.2: Normal Election Tally Algorithm
**Objective**: Implement standard LSA election tally

**Tasks**:
1. Create `ElectionAnalyzerNormal` class
2. Implement `PrepareForAnalysis()` method
3. Implement `CalculateBallotStatistics()` method
4. Implement `CountVotes()` method
5. Implement vote status determination logic

**Verification**:
- Service counts votes correctly
- Only valid ballots/votes are counted
- Result records created/updated properly

---

### Phase 3.3: Tie Detection and Ranking
**Objective**: Implement tie detection and result categorization

**Tasks**:
1. Implement `FinalizeResultsAndTies()` method
2. Implement ranking logic (sort by vote count)
3. Implement tie detection (group by vote count)
4. Implement section assignment (Elected/Extra/Other)
5. Implement tie-break requirement detection

**Verification**:
- Ties detected correctly
- Sections assigned properly
- Tie-break requirements identified correctly

---

### Phase 3.4: Result Summary Generation
**Objective**: Generate election statistics

**Tasks**:
1. Implement `FinalizeSummaries()` method
2. Calculate ballot statistics
3. Calculate vote statistics
4. Create/update ResultSummary entity
5. Implement `SaveResults()` method

**Verification**:
- ResultSummary created with accurate counts
- Database changes persisted

---

### Phase 3.5: Single-Name Election Support
**Objective**: Implement single-position election tally

**Tasks**:
1. Create `ElectionAnalyzerSingleName` class
2. Implement `CountVotes()` override using `SingleNameElectionCount`
3. Handle multiple votes for same candidate on ballot

**Verification**:
- Single-name elections tally correctly
- Vote counts sum properly

---

### Phase 3.6: Results API Enhancement
**Objective**: Add tally calculation endpoints

**Tasks**:
1. Add `POST /api/results/election/{guid}/calculate` endpoint
2. Add `GET /api/results/election/{guid}/summary` endpoint
3. Update existing endpoints to include calculated data
4. Add proper validation and error handling

**Verification**:
- Endpoints return correct data
- Validation prevents invalid requests
- Errors handled gracefully

---

### Phase 3.7: Comprehensive Testing
**Objective**: Ensure tally accuracy through extensive tests

**Tasks**:
1. Create `TallyServiceTests` - Unit tests for service methods
2. Create `ElectionAnalyzerNormalTests` - Algorithm correctness
3. Create `TieDetectionTests` - All tie scenarios
4. Create test data with known results
5. Test edge cases (0 votes, all tied, etc.)

**Verification**:
- All tests pass
- Edge cases handled correctly
- Results match expected outcomes

---

## 7. Test Scenarios

### 7.1 Normal Election Scenarios

**Scenario 1: Standard 9-member LSA election**
- 30 voters, 25 ballots
- 15 candidates
- No ties
- Expected: Top 9 elected, next 3 in extra, rest in other

**Scenario 2: Tie within elected**
- Candidates rank 7-8 tied with same votes
- Expected: Both marked as tied, both elected, no tie-break required

**Scenario 3: Tie crossing elected/extra boundary**
- Candidates rank 9-10 tied
- Expected: Both marked as tied, tie-break REQUIRED

**Scenario 4: Spoiled ballots**
- 5 spoiled ballots out of 30
- Expected: Only 25 ballots counted, statistics accurate

**Scenario 5: Invalid votes**
- Some votes for ineligible candidates
- Expected: Invalid votes excluded from count

### 7.2 Edge Cases

- Election with 0 ballots
- Election with all tied candidates
- Election with single candidate
- Ballot with 0 valid votes
- Candidate receiving votes on every ballot

---

## 8. Success Criteria

**Must Have**:
1. ✅ Normal election tally calculates correctly
2. ✅ Single-name election tally calculates correctly
3. ✅ Tie detection identifies all tie scenarios
4. ✅ Result sections assigned properly
5. ✅ Tie-break requirements determined accurately
6. ✅ ResultSummary statistics are correct
7. ✅ All unit tests pass (15+ tests)
8. ✅ Results match expected outcomes for all test scenarios
9. ✅ Build succeeds with 0 errors

**Nice to Have** (Future Phases):
- Progress reporting via SignalR (Phase 5)
- Performance optimization for 1000+ ballot elections
- Tally comparison tool (old vs. new results)

---

## 9. Risk Mitigation

**Risk**: Tally results don't match existing system  
**Mitigation**: Extensive test cases with known results, manual verification

**Risk**: Performance issues with large elections  
**Mitigation**: Use in-memory processing, batch operations

**Risk**: Complex tie scenarios not handled  
**Mitigation**: Comprehensive tie detection tests covering all scenarios

---

## 10. Notes

- This phase does NOT include SignalR progress updates (deferred to Phase 5)
- This phase does NOT include tie-break election resolution (future phase)
- Focus is on core algorithm correctness, not UI/real-time features
- Algorithm must be deterministic and repeatable
