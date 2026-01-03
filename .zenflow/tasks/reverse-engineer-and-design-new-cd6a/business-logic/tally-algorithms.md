# TallyJ Election Tally Algorithms Documentation

## Overview

TallyJ implements sophisticated ballot tallying logic to count votes, detect ties, rank candidates, and generate election results. The tally system handles various election types (LSA elections, conventions, by-elections, tie-breaks) with specific Bahá'í electoral rules.

---

## Core Tally Flow

### 1. Trigger Tally Analysis
**Entry Point**: `/After/Analyze` → `AfterController.StartAnalysis()`

**Process**:
1. User clicks "Analyze Ballots" button
2. Server creates appropriate analyzer based on election type
3. Analysis runs (can take 30 seconds to 5 minutes for large elections)
4. Results saved to database
5. UI redirected to reports page

### 2. Analyzer Selection

**Code**: `ElectionAnalyzerFactory` (inferred from pattern)

| Election Mode | Analyzer Class | Description |
|---------------|----------------|-------------|
| Normal | `ElectionAnalyzerNormal` | Standard 9-member LSA election |
| SingleName | `ElectionAnalyzerSingleName` | Single position (e.g., treasurer) |

**Note**: Both inherit from `ElectionAnalyzerCore` which contains shared logic.

---

## Normal Election Tally Algorithm

**File**: `CoreModels/ElectionAnalyzerNormal.cs`

### Step 1: Prepare for Analysis
**Method**: `PrepareForAnalysis()` (inherited from `ElectionAnalyzerCore`)

**Actions**:
1. Load all ballots for election
2. Load all votes for election
3. Load all people (candidates) for election
4. Initialize result summary counters
5. Clear previous results (if re-running analysis)

### Step 2: Calculate Ballot Statistics
**Code**: `ElectionAnalyzerNormal.cs:33-44`

```csharp
ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(BallotAnalyzer.BallotNeedsReview);

ResultSummaryCalc.TotalVotes = Ballots.Count * TargetElection.NumberToElect;

var invalidBallotGuids = 
    Ballots.Where(b => b.StatusCode != BallotStatusEnum.Ok).Select(b => b.BallotGuid).ToList();

ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count();
ResultSummaryCalc.SpoiledVotes =
    VoteInfos.Count(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && vi.VoteStatusCode != VoteStatusCode.Ok);

ResultSummaryCalc.BallotsReceived = Ballots.Count - ResultSummaryCalc.SpoiledBallots;
```

**Calculated Metrics**:
- **BallotsNeedingReview**: Ballots with changed candidate names (need manual review)
- **TotalVotes**: Total possible votes (num ballots × positions to fill)
- **SpoiledBallots**: Invalid ballots (marked by tellers)
- **SpoiledVotes**: Invalid votes on valid ballots
- **BallotsReceived**: Valid ballots (total - spoiled)

### Step 3: Count Votes
**Code**: `ElectionAnalyzerNormal.cs:46-99`

```csharp
var electionGuid = TargetElection.ElectionGuid;

// Collect only valid ballots
_hub.StatusUpdate("Processing ballots", true);
var numDone = 0;
var numVotesTotal = 0;

foreach (var ballot in Ballots.Where(bi => bi.StatusCode == BallotStatusEnum.Ok))
{
    numDone++;
    if (numDone % 10 == 0) {
        _hub.StatusUpdate("Processed {0} ballot{1} ({2} votes)".FilledWith(numDone, numDone.Plural(), numVotesTotal), true);
    }

    var ballotGuid = ballot.BallotGuid;
    
    // Collect only valid votes
    foreach (var voteInfoRaw in VoteInfos.Where(vi => vi.BallotGuid == ballotGuid && vi.VoteStatusCode == VoteStatusCode.Ok))
    {
        var voteInfo = voteInfoRaw;
        numVotesTotal++;
        
        // Get existing result record for this person, if available
        var result = Results.FirstOrDefault(r => r.ElectionGuid == electionGuid && r.PersonGuid == voteInfo.PersonGuid);
        
        if (result == null)
        {
            result = new Result
            {
                ElectionGuid = electionGuid,
                PersonGuid = voteInfo.PersonGuid.AsGuid()
            };
            InitializeSomeProperties(result);
            
            Savers.ResultSaver(DbAction.Add, result);
            Results.Add(result);
        }

        var voteCount = result.VoteCount.AsInt() + 1;
        result.VoteCount = voteCount;
    }
}
```

**Algorithm**:
1. Iterate through all **valid ballots** (StatusCode = Ok)
2. For each ballot, iterate through all **valid votes** (VoteStatusCode = Ok)
3. For each vote:
   - Find or create `Result` record for that candidate
   - Increment `VoteCount` by 1
4. Progress updates via SignalR every 10 ballots

**Vote Validity** (from `VoteAnalyzer.cs`):
```csharp
public static string DetermineStatus(VoteInfo voteInfo)
{
    // Vote is OK if:
    // - Person can receive votes (CanReceiveVotes = true)
    // - Person info hasn't changed significantly since ballot entry
    return !voteInfo.PersonCanReceiveVotes
        ? VoteStatusCode.Spoiled
        : voteInfo.PersonCombinedInfo.HasContent() &&
          !voteInfo.PersonCombinedInfo.StartsWith(voteInfo.PersonCombinedInfoInVote ?? "NULL")
            ? VoteStatusCode.Changed
            : VoteStatusCode.Ok;
}
```

**Vote Status Codes**:
- **Ok**: Valid vote, counts toward tally
- **Spoiled**: Person ineligible (e.g., under 18, not a member)
- **Changed**: Person info changed after ballot entry (needs review)
- **OnlineRaw**: Online ballot not yet processed

### Step 4: Finalize Results and Detect Ties
**Method**: `FinalizeResultsAndTies()` (from `ElectionAnalyzerCore`)

**Actions**:
1. **Sort results by vote count** (descending)
2. **Assign ranks** (1 = highest votes)
3. **Detect ties**:
   - Candidates with same vote count = tied
   - Group tied candidates by `TieBreakGroup`
4. **Categorize results**:
   - **Elected**: Top N candidates (where N = `NumberToElect`)
   - **Extra**: Next M candidates (where M = `NumberExtra`) - shown on reports
   - **Other**: Remaining candidates
5. **Detect "close to" relationships**:
   - `CloseToPrev`: Vote count within X of previous candidate
   - `CloseToNext`: Vote count within X of next candidate
6. **Determine tie-break requirements**:
   - If tie crosses elected/extra boundary → tie-break required
   - If all tied candidates fit in elected → no tie-break needed
   - If all tied candidates in extra/other → no tie-break needed

**Tie Detection Logic** (simplified):
```csharp
// Group candidates by vote count
var groupedByVotes = Results
    .GroupBy(r => r.VoteCount)
    .OrderByDescending(g => g.Key);

var rank = 1;
var tieGroupNumber = 1;

foreach (var group in groupedByVotes)
{
    var candidatesInGroup = group.ToList();
    var isTied = candidatesInGroup.Count > 1;

    foreach (var result in candidatesInGroup)
    {
        result.Rank = rank;
        result.IsTied = isTied;
        
        if (isTied)
        {
            result.TieBreakGroup = tieGroupNumber;
        }

        // Determine section (Elected, Extra, Other)
        if (rank <= NumberToElect)
        {
            result.Section = "Elected";
        }
        else if (rank <= NumberToElect + NumberExtra)
        {
            result.Section = "Extra";
        }
        else
        {
            result.Section = "Other";
        }
    }

    rank += candidatesInGroup.Count;
    if (isTied) tieGroupNumber++;
}

// Check if tie-break is required
foreach (var group in groupedByVotes.Where(g => g.Count() > 1))
{
    var sections = group.Select(r => r.Section).Distinct();
    
    // Tie-break required if tied candidates span multiple sections
    if (sections.Count() > 1)
    {
        foreach (var result in group)
        {
            result.TieBreakRequired = true;
        }
    }
}
```

**Example Tie Scenarios**:

**Scenario 1: Tie for 9th position** (9 to elect)
```
Rank  Name         Votes  Section   Tied?  Tie-Break?
1     Alice        45     Elected   No     No
2     Bob          42     Elected   No     No
3     Carol        40     Elected   No     No
4     David        38     Elected   No     No
5     Eve          36     Elected   No     No
6     Frank        34     Elected   No     No
7     Grace        32     Elected   No     No
8     Henry        30     Elected   No     No
9     Irene        28     Elected   Yes    YES (tied with Jack)
9     Jack         28     Extra     Yes    YES (crosses boundary)
11    Kelly        26     Extra     No     No
```
→ **Tie-break required** between Irene and Jack

**Scenario 2: Tie within elected** (9 to elect)
```
Rank  Name         Votes  Section   Tied?  Tie-Break?
1     Alice        45     Elected   Yes    No (all elected)
1     Bob          45     Elected   Yes    No
3     Carol        42     Elected   No     No
... (7 more elected)
```
→ **No tie-break required** (both Alice and Bob elected)

**Scenario 3: Tie within extra** (9 to elect, 3 extra)
```
Rank  Name         Votes  Section   Tied?  Tie-Break?
... (9 elected)
10    Irene        28     Extra     Yes    No (all in extra)
10    Jack         28     Extra     Yes    No
12    Kelly        26     Other     No     No
```
→ **No tie-break required** (both in "Extra" section for reporting)

### Step 5: Finalize Summaries
**Method**: `FinalizeSummaries()` (from `ElectionAnalyzerCore`)

**Actions**:
1. Create or update `ResultSummary` record
2. Save statistics:
   - `NumBallotsWithManual`: Manually entered ballots
   - `NumBallotsWithOnline`: Online ballots
   - `NumBallots`: Total ballots
   - `NumVoters`: Total registered voters
   - `NumVotersInPerson`: In-person voters
   - `NumVotersOnline`: Online voters
   - `SpoiledBallots`: Invalid ballots
   - `InvalidVotes`: Invalid votes
   - `TotalVotes`: Total votes cast
   - `UsedManualEntry`: Manual entry was used?
   - `UsedOnlineVoting`: Online voting was used?
3. Set `UseOnReports` flag (determines if results shown on reports)

### Step 6: Save to Database
**Code**: `ElectionAnalyzerNormal.cs:111-116`

```csharp
_hub.StatusUpdate("Saving");

Db.SaveChanges();

new ResultSummaryCacher(Db).DropThisCache();
new ResultTieCacher(Db).DropThisCache();
```

**Actions**:
1. Persist all `Result` records
2. Persist `ResultSummary`
3. Clear cached result data (force refresh)

---

## Tie-Break Election Tally

**Purpose**: Resolve ties from previous election

**Special Handling**:
- Only candidates in the tie group are on the ballot
- Uses same tally logic as normal election
- Results update original election's tie records

**Workflow**:
1. Head teller creates tie-break election
2. Only tied candidates are eligible to be voted for
3. Ballots entered (smaller number than original election)
4. Tally runs (same algorithm)
5. Tie resolved:
   - Update `ResultTie.IsResolved = true`
   - Update `Result.TieBreakCount` with new vote counts
   - Determine final rankings

---

## Single-Name Election Tally

**File**: `CoreModels/ElectionAnalyzerSingleName.cs` (not shown, but referenced)

**Use Case**: Elect single position (e.g., treasurer, secretary)

**Difference from Normal**:
- Each ballot can have **multiple votes for same person**
- `Vote.SingleNameElectionCount` field used
- Tally counts total votes per person across all ballots
- No "9 positions" limit

**Example**:
```
Ballot 1: Alice (5), Bob (3), Carol (1)
Ballot 2: Alice (4), Bob (4), Carol (1)
Ballot 3: Alice (6), Bob (2), Carol (1)

Results:
Alice: 15 votes (5+4+6)
Bob: 9 votes (3+4+2)
Carol: 3 votes (1+1+1)
```

---

## Ballot Validation

### Ballot Status Codes
**File**: `Code/Enumerations/BallotStatusEnum.cs`

| Status | Description |
|--------|-------------|
| Ok | Valid ballot, counts in tally |
| Review | Needs head teller review |
| Spoiled | Invalid (too many/few votes, damaged, etc.) |

### Ballot Needs Review Logic
**Code**: `BallotAnalyzer.BallotNeedsReview()`

**Conditions**:
- Any vote has `VoteStatusCode.Changed` (candidate name changed)
- Ballot marked for review by teller
- Too few votes on ballot (< NumberToElect)
- Too many votes on ballot (> NumberToElect)

**Example**:
```
LSA election (9 to elect):
- Ballot with 8 votes → Needs review (too few)
- Ballot with 10 votes → Needs review (too many)
- Ballot with 9 votes, 1 candidate name changed → Needs review
- Ballot with 9 votes, all OK → Does not need review
```

---

## Vote Duplicate Detection

### Duplicate Name on Same Ballot
**Handled during ballot entry** (not in tally)

**Code**: Ballot entry UI detects duplicates in real-time

**Actions**:
1. Teller enters "Alice" twice on same ballot
2. UI marks second "Alice" as duplicate
3. Teller must remove one or correct

### Changed Candidate Information
**Detected in tally**

**Code**: `VoteAnalyzer.DetermineStatus()`

**Scenario**:
1. Ballot entered with "John Smith"
2. Later, teller edits Person record: "John Smith" → "John Smith Jr."
3. Tally detects mismatch
4. Vote marked as "Changed"
5. Head teller reviews and decides:
   - Keep vote (same person, just name correction)
   - Invalidate vote (different person)

---

## Result Sections

### Elected Section
- **Rank**: 1 to `NumberToElect`
- **Display**: "Elected" on reports
- **Purpose**: Winning candidates

### Extra Section
- **Rank**: `NumberToElect + 1` to `NumberToElect + NumberExtra`
- **Display**: "Also Receiving Votes" on reports
- **Purpose**: Near-winners (for community awareness)

### Other Section
- **Rank**: `NumberToElect + NumberExtra + 1` onwards
- **Display**: "Others" on reports (optional)
- **Purpose**: All other candidates who received votes

### Force Show in Other
**Field**: `Result.ForceShowInOther`

**Use Case**: Head teller wants to show specific candidates even if they received few votes

---

## Progress Updates via SignalR

**Code**: `ElectionAnalyzerNormal.cs:50-58, 100`

```csharp
_hub.StatusUpdate("Processing ballots", true);
var numDone = 0;
var numVotesTotal = 0;

foreach (var ballot in Ballots.Where(bi => bi.StatusCode == BallotStatusEnum.Ok))
{
    numDone++;
    if (numDone % 10 == 0) {
        _hub.StatusUpdate("Processed {0} ballot{1} ({2} votes)".FilledWith(numDone, numDone.Plural(), numVotesTotal), true);
    }
    // ... process ballot ...
}

_hub.StatusUpdate("Processed {0} unspoiled ballot{1} ({2} votes)".FilledWith(numDone, numDone.Plural(), numVotesTotal));
```

**Purpose**:
- Keep users informed during long tally operations
- Show progress bar in UI
- Prevent timeouts for large elections

**Message Examples**:
- "Processing ballots"
- "Processed 10 ballots (90 votes)"
- "Processed 50 ballots (450 votes)"
- "Processed 200 unspoiled ballots (1800 votes)"
- "Saving"

---

## Performance Optimization

### Caching Strategy
**Code**: Throughout analyzer classes

```csharp
// Load data once
var ballots = new BallotCacher().AllForThisElection;
var voteInfos = new VoteInfoCacher().AllForThisElection;
var people = new PersonCacher().AllForThisElection;

// Use in-memory collections for tally
```

**Benefits**:
- Single database query per entity type
- In-memory processing (fast)
- No repeated database hits

### Batch Processing
**Code**: Updates every 10 ballots, not every ballot

```csharp
if (numDone % 10 == 0) {
    _hub.StatusUpdate(...);
}
```

**Benefits**:
- Reduces SignalR message frequency
- Prevents UI flooding
- Better performance for large elections

### Result Saving
**Code**: Save all results at end, not incrementally

```csharp
// Add results to in-memory list during tally
Results.Add(result);

// Save all at once at end
Db.SaveChanges();
```

**Benefits**:
- Single database transaction
- Better performance
- Atomic operation (all or nothing)

---

## Edge Cases

### 1. No Ballots Received
**Handling**:
- Tally runs but produces no results
- `ResultSummary` shows 0 ballots, 0 votes
- Reports show "No ballots received"

### 2. All Ballots Spoiled
**Handling**:
- `SpoiledBallots` = total ballots
- `BallotsReceived` = 0
- No `Result` records created
- Reports show "All ballots were spoiled"

### 3. No Valid Votes on Any Ballot
**Handling**:
- Ballots exist, but all votes invalid
- `SpoiledVotes` = all votes
- No `Result` records created
- Reports show "No valid votes received"

### 4. Tie Across All Candidates
**Example**: All 20 candidates receive exactly 10 votes each

**Handling**:
- All candidates marked as `IsTied = true`
- Single `TieBreakGroup = 1`
- `TieBreakRequired` depends on number to elect
- If electing 9 from 20 tied → tie-break required
- If electing 20 from 20 tied → all elected, no tie-break

### 5. More Elected Than Candidates
**Example**: Electing 9, but only 7 candidates received votes

**Handling**:
- All 7 candidates marked as "Elected"
- Reports note "Only 7 of 9 positions filled"
- Election incomplete (may require by-election)

---

## Bahá'í Electoral Principles Implementation

### 1. Secret Ballot
- No link between `Ballot` and `Person` (voter)
- `Vote.PersonGuid` links to candidate, not voter
- Votes cannot be traced to voters

### 2. No Nominations
- All eligible persons are candidates
- No formal nomination process
- Voters write any eligible name

### 3. Plurality Voting
- No ranked-choice or preferential voting
- Each vote counts equally (1 point)
- Highest vote counts win

### 4. Tie-Breaking
- Ties resolved by additional election
- Only tied candidates on tie-break ballot
- Process repeats until tie resolved

### 5. Confidentiality
- Vote counts not disclosed until election finalized
- Results only shown after head teller approval
- Partial tallies hidden from voters

---

## Testing Tally Logic

### Unit Test Examples

**Test 1: Simple Tally**
```csharp
[Fact]
public void Tally_SimpleElection_CorrectCounts()
{
    // Arrange
    var ballots = new List<Ballot>
    {
        CreateBallot(votes: new[] { "Alice", "Bob", "Carol" }),
        CreateBallot(votes: new[] { "Alice", "Bob", "David" }),
        CreateBallot(votes: new[] { "Alice", "Carol", "David" })
    };

    // Act
    var analyzer = new ElectionAnalyzerNormal(election, ballots, votes, people);
    analyzer.AnalyzeEverything();

    // Assert
    Assert.Equal(3, GetVoteCount("Alice"));  // 3 votes
    Assert.Equal(2, GetVoteCount("Bob"));    // 2 votes
    Assert.Equal(2, GetVoteCount("Carol"));  // 2 votes
    Assert.Equal(2, GetVoteCount("David"));  // 2 votes
}
```

**Test 2: Tie Detection**
```csharp
[Fact]
public void Tally_TieAcrossBoundary_RequiresTieBreak()
{
    // Arrange: 2 to elect, 3 candidates tied with 5 votes each
    var ballots = CreateBallotsWithTie(candidates: 3, votesEach: 5);

    // Act
    var analyzer = new ElectionAnalyzerNormal(election, ballots, votes, people);
    analyzer.AnalyzeEverything();

    // Assert
    var results = analyzer.Results;
    Assert.All(results, r => Assert.True(r.TieBreakRequired));
}
```

**Test 3: Vote Validity**
```csharp
[Fact]
public void VoteAnalyzer_IneligibleCandidate_SpoiledVote()
{
    // Arrange
    var vote = CreateVote(personGuid, ineligible: true);

    // Act
    var status = VoteAnalyzer.DetermineStatus(vote);

    // Assert
    Assert.Equal(VoteStatusCode.Spoiled, status);
}
```

---

## Migration to .NET Core

### Algorithm Preservation
**Critical**: Tally logic must produce **identical results** to current system

**Testing Strategy**:
1. Export test data from current system (ballots, votes, people)
2. Run tally in current system, save results
3. Run tally in new system with same data
4. Compare results byte-for-byte
5. Any discrepancy = bug

### Performance Improvements
1. **Async/await**: Use `async Task` for database operations
2. **LINQ optimizations**: Use compiled queries
3. **Parallel processing**: Tally multiple elections simultaneously (if needed)
4. **Progress reporting**: Use IProgress<T> instead of SignalR callbacks

### Code Modernization
```csharp
// Old (.NET Framework)
public override void AnalyzeEverything()
{
    var ballots = Ballots.Where(b => b.StatusCode == BallotStatusEnum.Ok);
    foreach (var ballot in ballots)
    {
        // Process...
    }
    Db.SaveChanges();
}

// New (.NET Core)
public override async Task AnalyzeEverythingAsync(IProgress<string> progress, CancellationToken cancellationToken)
{
    var ballots = await Db.Ballots
        .Where(b => b.ElectionGuid == electionGuid && b.StatusCode == BallotStatusEnum.Ok)
        .ToListAsync(cancellationToken);

    foreach (var ballot in ballots)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Process...
        progress.Report($"Processed {numDone} ballots");
    }

    await Db.SaveChangesAsync(cancellationToken);
}
```

---

**End of Tally Algorithms Documentation**
