# Auto

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

---

## Workflow Steps

### [x] Step: Investigation and Planning
<!-- chat-id: 4e87dc07-4c92-451b-be1c-941fa41aa722 -->

Investigated v3 tests (AnalyzerNormalTests.cs, AnalyzerSingleNameTests.cs, BallotAnalysisTests.cs) and v4 implementation (ElectionAnalyzerBase, ElectionAnalyzerNormal, ElectionAnalyzerSingleName, TallyService). Cataloged all gaps and differences. Produced the plan below.

### [x] Step: Create BallotAnalyzer service

Create `backend/Services/Analyzers/BallotAnalyzer.cs` — a standalone service that determines ballot status from its list of votes.

**Reference**: v3 `Site/CoreModels/BallotAnalyzer.cs` method `DetermineStatusFromVotesList`.

**Behavior** (port from v3):
- Inputs: `int votesNeededOnBallot`, `bool isSingleNameElection`, current ballot status string, list of vote-info items.
- If current status is `Review` → keep it, return false (no change).
- Check if any vote has a changed person name (person's `CombinedInfo` doesn't start with the vote's `PersonCombinedInfo`) → `Verify`.
- If single-name election → `Ok`.
- If 0 votes → `Empty`.
- If fewer than needed → `TooFew`.
- If more than needed → `TooMany`.
- If duplicate `PersonGuid` values → `Dup`.
- Otherwise → `Ok`.
- Output: new status code and spoiled count (votes where status is Spoiled).
- Returns bool: true if status changed from the input.

**Implementation notes:**
- The class should work with a lightweight DTO (not require full EF entities) so it's easily testable. Define a simple `BallotVoteInfo` record/class with the fields the analyzer needs: `Guid? PersonGuid`, `bool PersonCanReceiveVotes`, `string? PersonCombinedInfo`, `string? VoteCombinedInfo`, `string? IneligibleReasonCode`, `int? SingleNameElectionCount`.
- Add a public method `DetermineStatusFromVotes(BallotStatus? currentStatus, List<BallotVoteInfo> votes, out BallotStatus newStatus, out int spoiledCount) → bool`.
- Add a convenience method `DetermineVoteStatus(BallotVoteInfo vote) → VoteStatus` that mirrors the v3 `VoteAnalyzer.DetermineStatus` logic:
  - If person is null (no PersonGuid) and has IneligibleReasonCode → `Spoiled`
  - If `PersonCanReceiveVotes` is false → `Spoiled`
  - If `PersonCombinedInfo` doesn't start with `VoteCombinedInfo` → `Changed`
  - Otherwise → `Ok`

**Tests file**: `Backend.Tests/UnitTests/BallotAnalyzerTests.cs`

Port ALL of these tests from v3 `BallotAnalysisTests.cs`:
1. `CorrectNumberOfVotes` — 3 valid votes, needs 3 → Ok, spoiledCount=0
2. `TooManyNumberOfVotes` — 4 valid votes, needs 3 → TooMany
3. `TooManyNumberOfVotesWithBlank` — 3 valid votes, needs 3 → Ok (blank removed in v3)
4. `TooManyNumberOfVotesWithIneligible` — 3 valid + 1 ineligible, needs 3 → TooMany
5. `TooManyNumberOfVotesWithSpoiled` — 4 ineligible votes, needs 3 → TooMany
6. `TooFewNumberOfVotes` — 2 valid votes, needs 3 → TooFew
7. `SingleIneligible` — 1 ineligible vote in single-name election → Ok
8. `EmptyNumberOfVotes` — 0 votes → Empty
9. `TooFewNumberOfVotesWithBlank` — 2 valid votes, needs 3 → TooFew
10. `KeepReviewStatus` — current status is Review → stays Review regardless of vote count
11. `HasDuplicates` — 5 votes with 2 sharing same PersonGuid → Dup
12. `HasDuplicatesAndTooMany` — 6 votes (1 dup) but more than needed → TooMany takes precedence
13. `AllSpoiled` — 3 spoiled votes → Ok (unchanged), spoiledCount=3
14. `HasDuplicates2_KeepStatusCode` — Review stays; Ok overrides to TooFew

**Verification**: `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~BallotAnalyzerTests"`

### [x] Step: Fix ElectionAnalyzerBase tie analysis logic

Rewrite `ElectionAnalyzerBase.FinalizeResultsAndTies()` to match v3 `ElectionAnalyzerCore` behavior.

**Reference files**:
- v3: `Site/CoreModels/ElectionAnalyzerCore.cs` methods `DetermineOrderAndSections`, `AnalyzeForTies`, `AnalyzeTieGroup`
- v4: `backend/Services/Analyzers/ElectionAnalyzerBase.cs` method `FinalizeResultsAndTies`

**Section code mapping** (v4 uses different codes than v3):
- v3 `"T"` (Top) → v4 `"E"` (Elected)
- v3 `"E"` (Extra) → v4 `"X"` (Extra)
- v3 `"O"` (Other) → v4 `"O"` (Other)

**Changes required** (all in `ElectionAnalyzerBase.cs`):

1. **DetermineOrderAndSections** — Sort results by VoteCount descending, then TieBreakCount descending, then by person name (for deterministic tie ordering). Assign `Rank` as ordinal (1, 2, 3...), assign Section based on rank vs NumberToElect/NumberExtra. Set `RankInExtra` for results in Extra section.

2. **AnalyzeForTies (pass 1)** — Walk results in rank order. Compare each with the one above it:
   - If same VoteCount → both `IsTied = true`, assign same `TieBreakGroup`.
   - Set `CloseToPrev`/`CloseToNext` when vote difference ≤ 3 **including zero** (ties are "close"). v4 currently excludes zero — fix this.
   - Stop processing ties after the first tie group that ends in the Other section (optimization from v3).

3. **AnalyzeForTies (pass 2 — per group)** — For each tie group, create a `ResultTie` record (v4 currently only creates ResultTie for cross-section ties; v3 creates for ALL tie groups). Then call `AnalyzeTieGroup`.

4. **AnalyzeTieGroup** — Port the v3 `AnalyzeTieGroup` method:
   - Set `NumInTie` = count of results in group.
   - Determine which sections the group spans (Elected, Extra, Other).
   - `TieBreakRequired` = true unless group is entirely within Elected or entirely within Other.
   - `NumToElect`:
     - If group spans Elected+Extra or Elected+Other: NumToElect = count of results in Elected section within this group.
     - If group is only in Extra (and extends to Other): NumToElect = count of results in Extra section.
     - If `NumInTie == NumToElect` → decrement NumToElect by 1 (all tied candidates fit, need to vote for N-1).
   - `ForceShowInOther` = true for results in Other section when the tie group also includes Elected or Extra results.
   - `IsTieResolved` / `IsResolved`: Check if any two results that are in different sections (or both in Extra) have the same TieBreakCount. If so, not resolved. Otherwise resolved.
   - If `TieBreakRequired`: ensure each result has `TieBreakCount` (default 0 if null). If not required: clear TieBreakCount to null.

5. **Remove zero-VoteCount results** — v3 removes results with VoteCount=0 before tie analysis. Add this.

**Verification**: existing tests should still pass. `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~TallyServiceTests"`

### [x] Step: Fix ElectionAnalyzerBase ballot statistics and summary logic

Update `ElectionAnalyzerBase` to calculate ballot statistics matching v3 behavior.

**Reference**: v3 `ElectionAnalyzerCore.FillResultSummaryCalc`, `CombineCalcAndManualSummaries`, `RefreshBallotStatuses`, `FinalizeSummaries`

**Changes required:**

1. **Integrate BallotAnalyzer into PrepareForAnalysisAsync** — After loading ballots and votes, run the BallotAnalyzer to re-determine each ballot's status (call `DetermineStatusFromVotes` for each ballot). Update ballot StatusCode if changed. This matches v3's `RefreshBallotStatuses()` which refreshes vote statuses then re-analyzes all ballot statuses during analysis.

2. **Integrate VoteStatus refresh** — Before running BallotAnalyzer, refresh each vote's status using the `DetermineVoteStatus` method (already exists in `ElectionAnalyzerBase`). Update the `Vote.VoteStatus` property on each vote if changed.

3. **FillResultSummaryCalc** — Add voting-method-based ballot counts to ResultSummaryCalc:
   - `NumVoters` = count of people with a VotingMethod set
   - `NumEligibleToVote` = count of people where CanVote is true
   - `InPersonBallots` = count of people with VotingMethod "P"
   - `MailedInBallots` = count with "M"
   - `DroppedOffBallots` = count with "D"
   - `CalledInBallots` = count with "C"
   - `OnlineBallots` = count with "O" or "K"
   - `ImportedBallots` = count with "I"
   - `Custom1Ballots` = count with "1"
   - `Custom2Ballots` = count with "2"
   - `Custom3Ballots` = count with "3"

4. **CalculateBallotStatistics update** — The current method sets `BallotsNeedingReview` using `BallotNeedsReview()`. Change this to use the v3 definition: ballots with status `Review`, `Verify`, or any online-raw equivalent.

5. **CombineCalcAndManualSummaries** — Add support for a "Manual" ResultSummary record. If one exists, merge its values into the final summary (manual values override calculated for voter counts, ballot method counts). Create a "Final" ResultSummary that combines Calculated + Manual.

6. **FinalizeSummaries update** — Set `UseOnReports` based on: no ballots needing review AND all ties resolved AND NumBallotsWithManual == SumOfEnvelopesCollected. Add computed properties `NumBallotsWithManual` (= BallotsReceived + SpoiledBallots) and `SumOfEnvelopesCollected` (sum of all voting-method counts) — either as methods in the analyzer or as extension methods on ResultSummary.

**Verification**: `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~TallyServiceTests"`

### [x] Step: Fix ElectionAnalyzerSingleName statistics

Update `ElectionAnalyzerSingleName` to calculate statistics correctly per v3 logic.

**Reference**: v3 `Site/CoreModels/ElectionAnalyzerSingleName.cs`

**Key differences from normal election:**
- `BallotsReceived` = `NumVoters` = `TotalVotes` = sum of all `SingleNameElectionCount` values across all votes (not ballot count)
- `SpoiledVotes` = sum of `SingleNameElectionCount` for votes on valid ballots that have a non-Ok vote status
- `BallotsNeedingReview` = count of votes that need review (name changed), NOT ballot-level review
- Ballot counts from People (InPersonBallots, etc.) still apply

Override the relevant methods in `ElectionAnalyzerSingleName` to implement these differences.

**Verification**: `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~TallyServiceTests"`

### [x] Step: Write ElectionAnalyzerNormal unit tests

Create `Backend.Tests/UnitTests/ElectionAnalyzerNormalTests.cs` — test the analyzer directly (not through TallyService) for precise, deterministic verification of all v3 scenarios.

**Test infrastructure**: Create a small test base class or helper that:
- Creates an InMemory `MainDbContext`
- Creates an `Election` entity
- Creates a `Location` entity
- Provides helper methods to create `Person`, `Ballot`, `Vote` entities with minimal boilerplate
- Creates a `Mock<ILogger<ElectionAnalyzerNormal>>` (or `NullLogger`)
- Calls `analyzer.AnalyzeAsync()` and reads back results from the context

**Port ALL of these v3 tests** (from `AnalyzerNormalTests.cs`):

1. **Ballot_TwoPeople** — NumberToElect=2, 1 ballot, 2 valid votes. Expect: 2 results, both rank 1 and 2, both section "E" (elected), vote count 1 each. Summary: 0 needingReview, 1 numBallotsWithManual, 0 spoiled ballots, 1 InPerson (person[0] has VotingMethod="P"), 6 eligible to vote, 1 voter.

2. **Ballot_TwoPeople_NameChanged** — Same setup but vote[0].PersonCombinedInfo differs from person.CombinedInfo (not a prefix). Expect: 0 valid results (all on a ballot that needs verification), 1 needingReview, ballot status changes to Verify.

3. **Ballot_TwoPeople_NameExtended** — vote[0].PersonCombinedInfo is shorter prefix of person.CombinedInfo. Expect: 2 results (vote is still valid because person info starts with vote info), 0 needingReview.

4. **Ballot_TwoNames_AllSpoiled** — 2 votes with IneligibleReasonCode set (unidentifiable). Expect: 0 results, ballot Ok, both votes Spoiled. Summary: 0 needingReview.

5. **Ballot_OlderYouth** — 1 vote for ineligible youth (V01 reason), 1 vote for person[6] who has V01 reason. Expect: 0 results, both votes Spoiled.

6. **Ballot_TwoPeople_AllSpoiled** — 2 votes for persons with IneligibleReasonGuid set (CanReceiveVotes=false). Expect: 0 results, both votes Spoiled.

7. **Election_3_people_with_Tie_Not_Required** — NumberToElect=3, 3 ballots, each person gets 3 votes. All 3 results tied with same vote count. Tie NOT required because all 3 fit in Elected section. ResultTie: NumToElect=0, NumInTie=3, TieBreakRequired=false.

8. **Election_3_people_with_3_way_Tie** — NumberToElect=1, 3 candidates each with 1 vote. Tie IS required (only 1 slot). ResultTie: NumToElect=1, NumInTie=3, TieBreakRequired=true.

9. **ElectionWithTwoSetsOfTies** — NumberToElect=2, NumberExtra=2. 5 candidates: person0=3votes, person1=2, person2=2, person3=1, person4=1, person5=spoiled. Two tie groups. ResultTie[0]: persons 1&2, NumToElect=1, NumInTie=2. ResultTie[1]: persons 3&4, NumToElect=1, NumInTie=2. Check sections, CloseToPrev, CloseToNext, ForceShowInOther for person4 (in Other section of a tie group).

10. **ElectionTieSpanningTopExtraOther** — NumberToElect=2, NumberExtra=2. 5 candidates: person0=2, persons 1-4=1 (4-way tie). One ResultTie spanning all sections. NumToElect=1, NumInTie=4. ForceShowInOther=true for person4.

11. **ElectionTieWithTieBreakTiedInTopSection** — NumberToElect=2, NumberExtra=2. 3 candidates all with 1 vote (plus 1 spoiled vote). TieBreakRequired=true, NumToElect=2. Apply TieBreakCounts [1,1,0], re-analyze. After: IsResolved=true (persons 0&1 both have count 1, person2 has 0 — distinct across sections).

12. **ElectionTieWithTieBreakTiedInExtraSection** — Similar but TieBreakCounts [2,1,1] — person1 and person2 still tied → IsResolved=false.

13. **ElectionTieWithTieBreakTiedInExtraSection2** — 4 candidates. Person0=2 (not tied). Persons 1,2,3 tied at 1 vote. TieBreakCounts [2,1,1] → not resolved (persons 2&3 still tied in Extra).

14. **NSA_Election_1** — ElectionType="NSA", NumberToElect=2. Verify same normal analysis applies to NSA elections.

15. **Unit_In_TwoStage_Election** — Two linked elections, verify analysis only counts votes for the current election.

**Verification**: `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~ElectionAnalyzerNormalTests"`

### [x] Step: Write ElectionAnalyzerSingleName unit tests

Create `Backend.Tests/UnitTests/ElectionAnalyzerSingleNameTests.cs`.

**Port ALL of these v3 tests** (from `AnalyzerSingleNameTests.cs`):

1. **Election_3_people** — 3 ballots with SingleNameElectionCount values [33,5,2]. Expect: 2 results (person0=38, person1=2). Rank 1 in "E", rank 2 in "O".

2. **Election_3_people_With_Manual_Results** — Same votes + a Manual ResultSummary with SpoiledManualBallots=1. Final summary should include manual overrides. NumBallotsWithManual = BallotsReceived + SpoiledBallots = 40+1=41.

3. **Election_3_people_with_Tie** — Counts [10,10,2]. Two persons tied at 10. ResultTie with TieBreakRequired=true. ForceShowInOther=true for the tied person in Other.

4. **SingleNameElection_1_person** — All 3 vote records for same person (33+5+2=40). 1 result, rank 1, all tie-related fields false/null.

5. **Invalid_Ballots_Affect_Results** — Mix of valid and invalid ballots, person with IneligibleReason, vote with changed name. Verify spoiled ballot count, needingReview count, result count.

6. **Invalid_People_Do_Not_Affect_Results** — Persons with IneligibleReason get spoiled votes. Verify tie detection among non-spoiled results.

7. **SingleNameElection_3_people** — 3 people on 1 ballot. Verify 3 results with correct counts.

8. **SingleNameElection_3_people_with_Tie** — Two people tied at 10 votes. Verify tie group.

9. **SingleNameElection_3_people_with_3_way_Tie** — Three people all at 10 votes. Verify 3-way tie group.

**Verification**: `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~ElectionAnalyzerSingleNameTests"`

### [x] Step: Update existing TallyServiceTests for compatibility

After the analyzer fixes, review and update the existing `Backend.Tests/UnitTests/TallyServiceTests.cs` to ensure all existing tests pass with the corrected analyzer behavior.

**Key changes expected:**
- Section code assertions should remain "E"/"X"/"O" (unchanged).
- Tie detection tests may need updated assertions for `NumToElect` on ResultTie (now correctly calculated per-group instead of total).
- CloseToPrev/CloseToNext assertions may change for tied candidates (now includes diff=0).
- Some vote counting may change if ballot status is now re-evaluated during analysis.

**Process:**
1. Run all existing tests: `dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~TallyServiceTests"`
2. Fix any failing assertions to match the corrected (v3-compatible) behavior.
3. Do NOT weaken assertions — if a test was checking something correctly before, keep it. Only change assertions that reflect the improved v3-compatible behavior.

**Verification**: `dotnet test Backend.Tests/Backend.Tests.csproj` (all tests pass)

### [x] Step: Final validation

Run full test suite and verify everything passes:
1. `dotnet build backend/Backend.csproj`
2. `dotnet test Backend.Tests/Backend.Tests.csproj`
3. Review test count — should have significantly more analyzer/ballot tests than before.
