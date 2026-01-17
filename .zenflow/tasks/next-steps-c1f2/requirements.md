# Product Requirements Document: TallyJ4 Phase 3 - Tally Algorithm Implementation

## 1. Background

**Project**: TallyJ4 - Election management and ballot tallying system for Bahá'í communities  
**Phase**: Phase 3 - Tally Algorithm Implementation  
**Date**: January 17, 2026  
**Status**: Requirements Definition

### 1.1 Current State

**Completed Phases**:
- ✅ **Phase 1**: Database Foundation (16 entities, seeded test data, ASP.NET Identity)
- ✅ **Phase 2**: REST API Layer (8 controllers, 40+ endpoints, DTOs, validation, Swagger)

**Current Capabilities**:
- Create and manage elections
- Register voters and candidates (People)
- Create ballots and record votes
- Assign tellers to elections
- Import data from CSV
- View election logs
- Basic result retrieval (no calculations)

**Current Limitations**:
- ❌ No vote counting or tally calculation
- ❌ No tie detection
- ❌ No result ranking or categorization
- ❌ Cannot determine election winners
- ❌ No statistical summary of election results

### 1.2 Business Need

Bahá'í elections follow specific spiritual principles and require accurate, transparent vote tallying. The system must:

1. **Count votes accurately** according to Bahá'í electoral principles
2. **Detect ties** and identify which ties require resolution
3. **Rank candidates** by vote count
4. **Categorize results** into Elected, Extra, and Other sections
5. **Generate statistics** for election reporting
6. **Support multiple election types**:
   - Normal elections (e.g., 9-member Local Spiritual Assembly)
   - Single-name elections (e.g., single position elections)

### 1.3 Strategic Importance

The tally algorithm is the **core business logic** of TallyJ. Without it:
- The system cannot determine election outcomes
- Manual vote counting is required (defeats the purpose)
- Election results cannot be verified or audited
- Reporting and transparency are not possible

**Success Criteria**: Tally results must match the legacy TallyJ system exactly for the same input data.

---

## 2. User Personas

### 2.1 Primary Users

**Persona 1: Election Administrator**
- **Role**: Manages elections from creation to finalization
- **Goals**: Accurate tallies, clear results, transparent reporting
- **Needs**:
  - Ability to calculate election results on demand
  - View vote counts and rankings
  - Identify ties that require resolution
  - Generate summary statistics for reporting
  - Verify results accuracy

**Persona 2: Head Teller**
- **Role**: Oversees vote counting process
- **Goals**: Ensure fair and accurate tallying
- **Needs**:
  - Trigger tally calculation after ballot entry
  - Review preliminary results
  - Identify ballots requiring review
  - Detect data entry errors (vote count mismatches)

**Persona 3: Election Observer/Auditor**
- **Role**: Verifies election integrity
- **Goals**: Transparent, auditable results
- **Needs**:
  - View final results with vote counts
  - See ties and how they were resolved
  - Access summary statistics (total ballots, spoiled ballots, etc.)

### 2.2 Secondary Users

**Community Members** (view-only access in future phases):
- View final election results
- See elected members
- Understand election statistics

---

## 3. Functional Requirements

### 3.1 Vote Counting

**FR-1: Normal Election Tally**
- **Description**: Calculate results for standard elections (e.g., 9-member LSA)
- **Input**: Election GUID, existing ballots and votes
- **Process**:
  1. Load all ballots for the election
  2. Identify valid ballots (status = "Ok")
  3. For each valid ballot, count valid votes (status = "Ok")
  4. Increment vote count for each candidate (Person)
  5. Store results in Result table
- **Output**: Result records with vote counts for each candidate
- **Business Rules**:
  - Only count ballots with StatusCode = "Ok"
  - Only count votes with StatusCode = "Ok"
  - Ignore spoiled ballots
  - Ignore invalid votes (status != "Ok")

**FR-2: Single-Name Election Tally**
- **Description**: Calculate results for single-position elections
- **Input**: Election GUID (where ElectionType indicates single-name)
- **Process**: Same as FR-1 but use `Vote.SingleNameElectionCount` field
- **Business Rules**:
  - One ballot can have multiple votes for the same candidate
  - Sum `SingleNameElectionCount` for each candidate

**FR-3: Ballot Statistics**
- **Description**: Calculate ballot-level statistics
- **Metrics**:
  - Total ballots submitted
  - Valid ballots (StatusCode = "Ok")
  - Spoiled ballots (StatusCode = "Spoiled")
  - Ballots needing review (StatusCode = "Review")
- **Output**: Store in ResultSummary table

**FR-4: Vote Statistics**
- **Description**: Calculate vote-level statistics
- **Metrics**:
  - Total votes cast
  - Valid votes
  - Invalid votes
  - Average votes per ballot
- **Output**: Store in ResultSummary table

### 3.2 Result Ranking and Categorization

**FR-5: Rank Candidates**
- **Description**: Assign rank numbers based on vote count
- **Process**:
  1. Sort candidates by vote count (descending)
  2. Assign Rank = 1 to highest vote count
  3. Increment rank for each subsequent candidate
  4. Tied candidates receive the same rank
- **Output**: Result.Rank field populated

**FR-6: Section Assignment**
- **Description**: Categorize results into Elected, Extra, Other sections
- **Business Rules**:
  - **Elected**: Candidates with Rank 1 through `Election.NumberToElect`
  - **Extra**: Candidates with Rank `NumberToElect + 1` through `NumberToElect + NumberExtra`
  - **Other**: All remaining candidates
- **Output**: Result.Section field populated

**FR-7: Section Display Logic**
- **Description**: Determine which sections to show in reports
- **Business Rules**:
  - Always show Elected section
  - Show Extra section if `Election.NumberExtra > 0`
  - Show Other section if requested or if needed for clarity

### 3.3 Tie Detection

**FR-8: Detect Ties**
- **Description**: Identify candidates with identical vote counts
- **Process**:
  1. Group candidates by vote count
  2. Groups with 2+ candidates are ties
  3. Assign TieBreakGroup number
  4. Mark IsTied = true
- **Output**: Result.IsTied, Result.TieBreakGroup fields populated

**FR-9: Tie-Break Requirement Detection**
- **Description**: Determine which ties require resolution
- **Business Rules**:
  - **Tie within Elected section**: No tie-break needed (all elected)
  - **Tie within Extra section**: No tie-break needed
  - **Tie within Other section**: No tie-break needed
  - **Tie spanning Elected/Extra boundary**: Tie-break REQUIRED
  - **Tie spanning Extra/Other boundary**: Tie-break REQUIRED
- **Output**: Result.TieBreakRequired field populated
- **Example**: If rank 9 and 10 are tied in a 9-member election, tie-break is required

**FR-10: Record Ties**
- **Description**: Create ResultTie records for ties requiring resolution
- **Process**: For each tie requiring resolution:
  1. Create ResultTie record
  2. Link to Result records in the tie
  3. Mark TieBreakRequired = true
- **Output**: ResultTie table populated

### 3.4 Result Calculation API

**FR-11: Calculate Results Endpoint**
- **Endpoint**: `POST /api/results/election/{electionGuid}/calculate`
- **Authorization**: Authenticated users (Admin or Teller role)
- **Process**:
  1. Validate election exists
  2. Determine election type (normal vs. single-name)
  3. Execute appropriate tally algorithm
  4. Return tally results and statistics
- **Response**: TallyResultDto with statistics and result summary

**FR-12: Get Results with Calculations**
- **Endpoint**: `GET /api/results/election/{electionGuid}`
- **Authorization**: Authenticated users
- **Process**: Return result records with vote counts, ranks, sections, tie indicators
- **Response**: List of ResultDto with full calculation data

**FR-13: Get Result Summary**
- **Endpoint**: `GET /api/results/election/{electionGuid}/summary`
- **Authorization**: Authenticated users
- **Process**: Return ResultSummary with ballot and vote statistics
- **Response**: ResultSummaryDto

### 3.5 Edge Cases

**FR-14: Handle Zero Ballots**
- **Scenario**: Election with no ballots submitted
- **Behavior**: Return empty results, statistics show 0 ballots

**FR-15: Handle All Tied Results**
- **Scenario**: All candidates receive same vote count
- **Behavior**: Mark all as tied, determine tie-break requirement based on section boundaries

**FR-16: Handle Single Candidate**
- **Scenario**: Election with only one candidate
- **Behavior**: Single candidate elected (or in appropriate section)

**FR-17: Handle Ballot with Zero Valid Votes**
- **Scenario**: Ballot submitted but all votes are invalid
- **Behavior**: Count ballot in statistics, but contribute 0 votes to candidates

**FR-18: Recalculation Safety**
- **Scenario**: Calculate results multiple times (e.g., after data corrections)
- **Behavior**:
  - Clear previous Result records for election
  - Recalculate from scratch
  - Preserve audit trail (log recalculations)

---

## 4. Non-Functional Requirements

### 4.1 Performance

**NFR-1: Tally Speed**
- **Requirement**: Calculate results for 100 ballots in < 1 second
- **Target**: Support elections up to 1,000 ballots with reasonable performance (< 10 seconds)
- **Rationale**: Most elections are 20-100 ballots; large conventions may reach 300+ ballots

**NFR-2: Database Efficiency**
- **Requirement**: Use batch operations for Result record creation/update
- **Rationale**: Minimize database round trips

### 4.2 Reliability

**NFR-3: Accuracy**
- **Requirement**: Tally results must be 100% accurate and deterministic
- **Verification**: Results match legacy TallyJ system for same input data
- **Testing**: Comprehensive test cases with known expected outcomes

**NFR-4: Idempotency**
- **Requirement**: Calculating results multiple times produces identical results
- **Rationale**: Users may trigger calculation multiple times during data entry

**NFR-5: Transaction Safety**
- **Requirement**: Tally calculation runs in a database transaction
- **Rationale**: Ensure consistency if calculation fails partway

### 4.3 Maintainability

**NFR-6: Code Organization**
- **Requirement**: Separate concerns (service layer, algorithm classes, API controllers)
- **Rationale**: Enable unit testing, future enhancements

**NFR-7: Logging**
- **Requirement**: Log tally calculation start, completion, and errors
- **Rationale**: Enable debugging and audit trail

**NFR-8: Documentation**
- **Requirement**: Algorithm classes include comments explaining business logic
- **Rationale**: Future developers must understand Bahá'í electoral principles

### 4.4 Testability

**NFR-9: Unit Test Coverage**
- **Requirement**: 15+ unit tests covering core algorithms
- **Test Scenarios**:
  - Normal election tally
  - Single-name election tally
  - Tie detection (all scenarios)
  - Section assignment
  - Edge cases (zero ballots, all tied, etc.)

**NFR-10: Integration Tests**
- **Requirement**: API endpoint tests for calculate, get results, get summary
- **Coverage**: Authentication, validation, error handling

---

## 5. User Stories

**US-1: Calculate Election Results**
> As an Election Administrator, I want to calculate election results on demand so that I can see vote counts and determine winners.
>
> **Acceptance Criteria**:
> - Click "Calculate Results" button (future UI)
> - API calculates vote counts for all candidates
> - Results displayed with ranks and sections
> - Statistics show total ballots, votes, spoiled ballots

**US-2: Identify Ties Requiring Resolution**
> As a Head Teller, I want the system to detect ties and tell me which ones require resolution so that I can conduct tie-break elections if needed.
>
> **Acceptance Criteria**:
> - System detects all ties (candidates with same vote count)
> - System marks ties spanning section boundaries as "tie-break required"
> - Ties within a section show "no tie-break needed"
> - Tie information included in results display

**US-3: Recalculate After Data Corrections**
> As an Election Administrator, I want to recalculate results after fixing data entry errors so that the results are always accurate.
>
> **Acceptance Criteria**:
> - Edit ballot or vote data
> - Recalculate results
> - New results reflect corrected data
> - Previous results are replaced (not duplicated)

**US-4: View Election Statistics**
> As an Election Observer, I want to see summary statistics (total ballots, spoiled ballots, total votes) so that I can verify election integrity.
>
> **Acceptance Criteria**:
> - View ResultSummary data
> - Statistics include: total ballots, valid ballots, spoiled ballots, total votes, invalid votes
> - Statistics are accurate and match ballot/vote counts

**US-5: Verify Result Accuracy**
> As a System Verifier, I want tally results to match the legacy TallyJ system exactly so that I can trust the new system.
>
> **Acceptance Criteria**:
> - Create test election with known results
> - Calculate results in TallyJ4
> - Compare with legacy TallyJ results
> - Vote counts, ranks, sections, and tie detection match exactly

---

## 6. Out of Scope (Future Phases)

The following are explicitly **NOT** part of Phase 3:

1. **Tie-Break Election Resolution**: Conducting secondary elections to resolve ties (future phase)
2. **Real-time Progress Updates**: SignalR notifications during tally calculation (Phase 5)
3. **Report Generation**: PDF/Excel export of results (Phase 4)
4. **UI Components**: Frontend forms and displays (Phase 4)
5. **Advanced Analytics**: Historical trends, vote distribution charts (future phase)
6. **Online Voting Tally**: Special handling for online votes (Phase 4)
7. **Manual Result Adjustment**: Override calculated results (future phase)

---

## 7. Assumptions

1. **Database is seeded**: Phase 1 completed with test data
2. **API layer exists**: Phase 2 completed with all CRUD endpoints
3. **Authentication works**: Users can authenticate and receive JWT tokens
4. **Ballot data is correct**: Tellers have entered ballot/vote data accurately
5. **Election configuration is valid**: `NumberToElect`, `NumberExtra` are set correctly
6. **Entity relationships are correct**: Foreign keys link Ballots→Votes→People→Elections properly

---

## 8. Constraints

1. **No UI Development**: Phase 3 focuses on backend/API only
2. **No Breaking Changes**: Must not modify existing API contracts from Phase 2
3. **.NET 9.0 Only**: No additional frameworks or languages
4. **SQL Server Only**: No support for other databases yet
5. **No External Dependencies**: Algorithm must be self-contained (no third-party tally libraries)

---

## 9. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Functional Completeness** | 100% of FR-1 to FR-18 implemented | Manual review of requirements |
| **Test Coverage** | 15+ unit tests passing | xUnit test results |
| **Performance** | < 1 second for 100 ballots | Performance test |
| **Accuracy** | 100% match with known results | Comparison test with legacy system |
| **Build Success** | 0 errors, 0 warnings | `dotnet build` output |
| **API Endpoints** | 3 new/enhanced endpoints working | Swagger UI manual testing |

---

## 10. Acceptance Criteria

Phase 3 is complete when:

1. ✅ Normal election tally algorithm implemented and tested
2. ✅ Single-name election tally algorithm implemented and tested
3. ✅ Tie detection logic implemented and tested (all scenarios)
4. ✅ Result sectioning (Elected/Extra/Other) working correctly
5. ✅ ResultSummary statistics calculated accurately
6. ✅ API endpoints for calculate, get results, get summary working
7. ✅ 15+ unit tests passing
8. ✅ Integration tests passing (or known limitations documented)
9. ✅ Build succeeds with 0 errors
10. ✅ Tally results match expected outcomes for all test scenarios

---

## 11. Dependencies

**Upstream Dependencies** (must be complete):
- ✅ Phase 1: Database schema and seeded data
- ✅ Phase 2: API endpoints for Elections, People, Ballots, Votes, Results

**Downstream Dependencies** (will use Phase 3 outputs):
- Phase 4: Frontend displays results calculated by Phase 3
- Phase 5: SignalR broadcasts tally progress
- Future: Reporting uses tally results

---

## 12. Risks and Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Tally results don't match legacy system** | High | Medium | Extensive testing with known results, manual verification |
| **Complex tie scenarios not handled** | High | Low | Comprehensive test cases for all tie types |
| **Performance issues with large elections** | Medium | Low | Performance testing, optimization if needed |
| **Algorithm logic too complex** | Medium | Low | Clear code comments, separate concerns, unit tests |
| **Seeded data doesn't support testing** | Medium | Low | Add additional test data if needed |

---

## 13. Glossary

- **LSA**: Local Spiritual Assembly (9-member governing body)
- **Tally**: The process of counting votes and determining election results
- **Tie**: Two or more candidates with identical vote counts
- **Tie-Break**: Secondary election to resolve a tie
- **Section**: Result category (Elected, Extra, Other)
- **Spoiled Ballot**: Invalid ballot that should not be counted
- **Normal Election**: Standard election where each ballot has up to N votes
- **Single-Name Election**: Election for a single position where ballots can have multiple votes for the same candidate

---

## 14. References

- **Technical Specification**: `.zenflow/tasks/continue-phase-1-7001/phase3-spec.md`
- **Reverse Engineering Docs**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`
- **Business Logic**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/business-logic/tally-algorithms.md`
- **Database Schema**: `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/database/entities.md`
- **Phase 2 Report**: `report.md`

---

**Document Status**: Ready for Technical Specification  
**Next Step**: Create `spec.md` based on this PRD
