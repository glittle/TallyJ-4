# Tie & Extras Test Improvements — June 17, 2026

## Summary

- **Added 6 new tests** addressing the highest-severity gaps from the coverage review: tie-break reordering, `SaveTieCountsAsync` end-to-end workflow, `RankInExtra` sequencing, and ties confined to the extra (`"X"`) section.
- **Fixed `TallyService.SaveTieCountsAsync`** to automatically re-run the analyzer when all tied candidates in a group have tie-break counts — this was required for meaningful service-level tests and completes the broken production workflow.
- **All 6 new tests pass**, along with the full `ElectionAnalyzerNormalTests` (18) and `TallyServiceTests` (19) suites (37 total).
- Tests use realistic fixtures: 3-way equal-vote ties with `numberToElect: 1, numberExtra: 1`, and descending vote distributions with spoiled second ballot slots when `numberToElect: 2`.

---

## Production Code Change

### `TallyService.SaveTieCountsAsync` — wire save → re-analyze

Previously logged a message but never re-ran the analyzer. Now calls the appropriate calculator when every tied member in an updated group has a `TieBreakCount`:

```csharp
if (reAnalysisNeeded)
{
    _logger.LogInformation("All tie-break counts entered for a group, re-analyzing election {ElectionGuid}", electionGuid);
    if (election.ElectionType == "Oth")
    {
        await CalculateSingleNameElectionAsync(electionGuid);
    }
    else
    {
        await CalculateNormalElectionAsync(electionGuid);
    }
}
```

**File:** `backend/Services/TallyService.cs`

---

## 1. Tie-Break Reordering Test (Analyzer)

### `ElectionAnalyzerNormalTests.TieBreakCounts_ReorderCandidatesAndSections`

**Purpose:** Prove that manually entered tie-break counts change `Rank`, `Section`, and `RankInExtra` after re-analysis — not just resolution flags.

**Scenario:**

- `numberToElect: 1`, `numberExtra: 1`
- Three candidates each receive 1 vote (3-way tie)
- Initial name-sort order: `a0` → E, `a1` → X (`RankInExtra: 1`), `a2` → O
- Tie-break counts: `a2=5`, `a0=2`, `a1=1`
- After re-analysis: `a2` → E, `a0` → X, `a1` → O; tie group marked resolved

**Key assertions:**

| Candidate | Before rank/section | After rank/section |
|-----------|--------------------|--------------------|
| a0 | 1 / E | 2 / X (`RankInExtra: 1`) |
| a1 | 2 / X | 3 / O |
| a2 | 3 / O | 1 / E |

**File:** `Backend.Tests/UnitTests/ElectionAnalyzerNormalTests.cs`

---

## 2. `SaveTieCountsAsync` Tests (Service)

### `TallyServiceTests.SaveTieCountsAsync_TriggersReanalysisAndReordersCandidates`

**Purpose:** End-to-end test of the live workflow: tally → save tie-break counts via service → automatic re-analysis → verify updated rankings.

**Flow:**

1. `CalculateNormalElectionAsync` on 3-way tie fixture
2. `SaveTieCountsAsync` with counts `{ person2: 5, person0: 2, person1: 1 }`
3. Assert `ReAnalysisTriggered == true`
4. Assert final order matches analyzer reorder test (person2 E, person0 X, person1 O)
5. Assert counts persisted in DB

### `TallyServiceTests.SaveTieCountsAsync_UnresolvedCountsStillReordersButStaysUnresolved`

**Purpose:** Tie-break counts can reorder candidates even when the tie group remains unresolved (equal secondary counts across sections).

**Flow:**

1. Save counts `{ 2, 1, 1 }` for the same 3-way tie
2. Assert re-analysis triggered and person0 moves to E
3. Assert `ResultTie.IsResolved == false` (person1 and person2 still tied at count 1 across X/O)

### New test helpers in `TallyServiceTests`

- `CreateThreeWayEqualVoteAsync` — one vote per ballot for three people
- `CreateDescendingVoteDistributionAsync` — configurable vote counts per person; optional spoiled second slot for multi-seat ballots

**File:** `Backend.Tests/UnitTests/TallyServiceTests.cs`

---

## 3. `RankInExtra` Sequencing Tests

### `ElectionAnalyzerNormalTests.Extras_AssignRankInExtraSequentially`

**Purpose:** Verify `RankInExtra` is assigned 1, 2, 3 for the extra block when `numberExtra: 3`.

**Scenario:**

- `numberToElect: 2`, `numberExtra: 3`
- Five candidates with vote counts 5, 4, 3, 2, 1
- Each ballot has 2 slots (second slot spoiled) so ballots pass validation with `numberToElect: 2`

**Assertions:**

| Rank | Section | RankInExtra |
|------|---------|-------------|
| 1–2 | E | null |
| 3 | X | 1 |
| 4 | X | 2 |
| 5 | X | 3 |

### `TallyServiceTests.CalculateNormalElectionAsync_ExtrasAssignRankInExtraSequentially`

**Purpose:** Same `RankInExtra` logic validated through `TallyService` integration path.

**File:** Both test files above.

---

## Supporting Test Infrastructure Changes

| Change | File | Why |
|--------|------|-----|
| `MakeVote(ballot, person, positionOnBallot = 1)` | `ElectionAnalyzerNormalTests.cs` | Support multi-slot ballots |
| `MakeVoteForIneligible(..., positionOnBallot = 1)` | `ElectionAnalyzerNormalTests.cs` | Spoiled filler slot without adding vote counts |
| `using Backend.DTOs.Results` | `TallyServiceTests.cs` | `SaveTieCountsRequestDto` / `TieCountDto` |

---

## Additional Test Added

### `TallyServiceTests.CalculateNormalElectionAsync_TieWithinExtraSectionOnly_RequiresTieBreak`

**Purpose:** Cover a tie that exists **only within the `"X"` (extra) section** — not spanning E/X/O boundaries.

**Helper change:** Replaced the unused `CreateVotesWithTieWithinExtraSectionAsync` implementation with a focused, non-overlapping ballot fixture:

| Candidate | Ballots used | Vote count | Expected section |
|-----------|--------------|------------|------------------|
| person0 | 0–3 | 4 | E (rank 1) |
| person1 | 4–6 | 3 | X (rank 2, `RankInExtra: 1`) |
| person2 | 7–9 | 3 | X (rank 3, `RankInExtra: 2`) |

**Configuration:** `numberToElect: 1`, `numberExtra: 3`, 10 single-vote ballots.

**Assertions:**

- person0 elected, not tied
- persons 1 and 2 tied, both `Section == "X"`, both `TieBreakRequired == true`, same `TieBreakGroup`
- `RankInExtra` assigned as 1 and 2 within the extra block
- Single `ResultTie` record: `NumInTie = 2`, `NumToElect = 1` (decremented from 2 because all tied members are in X), `TieBreakRequired = true`, `IsResolved = false`

**`NumToElect` behavior:** For an X-only group, the analyzer adds the count of X-section members (`2`), then decrements because `NumInTie == NumToElect` — yielding `1` contested extra slot.

**File:** `Backend.Tests/UnitTests/TallyServiceTests.cs`

---

## Test Results

```
Filter: TieBreakCounts_Reorder | Extras_AssignRankInExtra | SaveTieCountsAsync | ExtrasAssignRankInExtra | TieWithinExtraSectionOnly
Passed: 6/6

Filter: ElectionAnalyzerNormalTests | TallyServiceTests
Passed: 37/37
```

---

## Next Steps

### Recommended follow-up tests

1. ~~**Tie within extra section only**~~ — **Done** (`CalculateNormalElectionAsync_TieWithinExtraSectionOnly_RequiresTieBreak`).
2. **`GetTiesAsync` smoke test** — verify tie retrieval API returns correct candidates and counts for a tie group.
3. **`UseOnReports` gate** — assert `ResultSummaryFinal.UseOnReports` is `false` with unresolved ties and `true` after resolving tie-break + re-analysis.
4. **Single-name parity** — add tie-break reorder test in `ElectionAnalyzerSingleNameTests` using `SaveTieCountsAsync` with `ElectionType: "Oth"`.
5. **`NumberExtra = 0`** — explicit test that no `"X"` sections are assigned.
6. **Analyzer-level X-only tie test** — mirror the service test in `ElectionAnalyzerNormalTests` for consistency across layers.
7. **LSA-scale X-only tie** (`numberToElect: 9`, `numberExtra: 2`) — extend helper or add variant for convention-sized elections.

### Recommended code changes (not done in this pass)

| Item | Rationale |
|------|-----------|
| Trigger re-analysis on **any** tie-count save (not only when all group members have counts) | Would allow incremental reorder as tellers enter counts; current behavior requires all counts before re-run |
| Distinguish default `TieBreakCount = 0` from user-entered zero | Analyzer defaults required ties to 0, so `reAnalysisNeeded` fires even after a single save if others already have the default |
| Frontend: send explicit `0` counts and refresh tally results after save | Align UI with backend workflow now that re-analysis is wired |
| v3 golden-file regression | Import known v3 elections with extras/tie-breaks and compare output |

### Files modified in this work

| File | Change |
|------|--------|
| `backend/Services/TallyService.cs` | Save → re-analyze wiring |
| `Backend.Tests/UnitTests/ElectionAnalyzerNormalTests.cs` | +2 tests, helper signature updates |
| `Backend.Tests/UnitTests/TallyServiceTests.cs` | +4 tests, +2 helpers, rewrote `CreateVotesWithTieWithinExtraSectionAsync` |