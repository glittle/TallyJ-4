# Unit Test Coverage Review: Ties, Tie-Breaks, Ranking & Extras

**Date:** June 17, 2026  
**Scope:** `Backend.Tests/UnitTests/` — tie detection, tie-break vote handling, cross-section ties, extras (`"X"` section), and ranking behavior.

---

## Summary

- **`ElectionAnalyzerNormalTests` is the strongest layer** — eight dedicated tie/tie-break tests with detailed assertions on `TieBreakGroup`, `TieBreakRequired`, `NumToElect`, `ForceShowInOther`, and `IsResolved` flags across E/X/O section boundaries.
- **Cross-section tie detection is well covered** at both analyzer and service levels, including E/X, X/O, E/X/O spanning, and within-section ties that do *not* require tie-break.
- **Tie-break re-analysis is partially tested** — three analyzer tests set `TieBreakCount` on DB entities and re-run analysis, but they assert resolution flags only; **no test verifies that rank, section, or `RankInExtra` actually change** after tie-break counts are applied.
- **`TallyServiceTests` adds integration coverage** for section counts and boundary ties, but **`SaveTieCountsAsync` / `GetTiesAsync` have zero tests**, and a prepared helper `CreateVotesWithTieWithinExtraSectionAsync` is never wired to a `[Fact]`.
- **Extras (`RankInExtra`, multi-extra ordering, `NumberExtra = 0`) are largely untested** in analyzer/service tests; the only `RankInExtra` assertion is in `ReportServiceTests` (display formatting, not tally logic).
- **Single-name analyzer tie tests exist but omit extras and tie-break workflows entirely.**

### Key Gaps Identified

| Gap | Severity |
|-----|----------|
| No test proves tie-break counts **reorder** candidates (rank/section movement) | High |
| No `TallyService.SaveTieCountsAsync` or end-to-end save → re-analyze tests | High |
| No test for tie **within extra section only** (`"X"` only group) | Medium |
| No `RankInExtra` sequencing tests (1, 2, 3 for multiple extras) | Medium |
| No explicit `NumberExtra = 0` / no-extras election test | Low |
| No `UseOnReports` gate test tied to unresolved ties | Medium |
| No tie-break persistence test at `TallyService` level (recalculation drops counts) | Medium |
| No name-sort tie-breaker test (equal votes + equal tie-break counts) | Low |
| Single-name: no extras or tie-break re-analysis coverage | Medium |

### Recommendations for New or Improved Tests

1. Add **`ElectionAnalyzerNormalTests` test** where tie-break counts clearly change rank *and* section (e.g. counts `3,1,0` move a candidate from `"X"` to `"E"`); assert `PersonGuid` order, `Rank`, `Section`, and `RankInExtra` before and after.
2. Wire **`CreateVotesWithTieWithinExtraSectionAsync`** to a new `TallyServiceTests` fact asserting tied extras require tie-break and correct `NumToElect`.
3. Add **`TallyServiceTests.SaveTieCountsAsync_*`** tests: save counts via service → trigger re-analysis (once implemented) → assert updated ranks.
4. Add **`RankInExtra` analyzer test** with `numberExtra: 3`, verify `RankInExtra` is 1/2/3 and null for non-extras.
5. Add **`UseOnReports` test**: unresolved tie → `false`; all ties resolved → `true` (with envelopes reconciled).
6. Extend **`Recalculation_ProducesIdenticalResults`** to include a scenario where tie-break counts are preserved across re-runs.

---

## Test Inventory by File

### `ElectionAnalyzerNormalTests.cs` — Primary tie logic coverage

| Test | What it covers |
|------|----------------|
| `Election_3_people_with_Tie_Not_Required` | 3-way tie all within `"E"`; `TieBreakRequired = false`; `NumToElect = 0` |
| `Election_3_people_with_3_way_Tie` | 3-way tie at elected cutoff spanning `"E"` / `"O"`; `ForceShowInOther`; `TieBreakRequired = true` |
| `ElectionWithTwoSetsOfTies` | Two independent tie groups (E/X boundary + X/O boundary); `numberExtra: 2` |
| `ElectionTieSpanningTopExtraOther` | Single tie group across E, X, and O (4 tied of 5 candidates) |
| `ElectionTieWithTieBreakTiedInTopSection` | Sets counts `1,1,0` → re-analyze → `IsResolved = true` (sections unchanged) |
| `ElectionTieWithTieBreakTiedInExtraSection` | Sets counts `2,1,1` → re-analyze → still unresolved (E/X same secondary count) |
| `ElectionTieWithTieBreakTiedInExtraSection2` | Partial tie group (E+X only); counts `2,1,1` → still unresolved |
| `NSA_Election_1` | Election type variant (not tie-specific) |

**Strengths:**

- Exhaustive flag assertions: `IsTied`, `TieBreakGroup`, `TieBreakRequired`, `ForceShowInOther`, `CloseToPrev`/`CloseToNext`, `ResultTie.NumToElect` / `NumInTie`.
- Uses `numberExtra: 2` in most cross-section scenarios.
- Tie-break tests simulate the *intended* workflow (persist counts → re-run analyzer).

**Weaknesses:**

- Tie-break tests check `IsTieResolved` / `IsResolved` but **never assert rank or section changed** — even when counts differ (e.g. `2,1,1` should reorder by tie-break sort).
- **`RankInExtra` is never asserted** despite multiple `"X"` section candidates.
- No tie within `"O"` only (should not require tie-break).
- No test for `NumInTie == NumToElect` decrement edge case in isolation.
- No `UseOnReports` assertion.

### `ElectionAnalyzerSingleNameTests.cs` — Tie detection only

| Test | What it covers |
|------|----------------|
| `Election_3_people_with_Tie` | 2-way tie at top; `ForceShowInOther` on `"O"` member |
| `Invalid_People_Do_Not_Affect_Results` | 3-way tie in `"O"` only; `TieBreakRequired = false` |
| `SingleNameElection_3_people_with_Tie` | Basic 2-way tie (minimal assertions) |
| `SingleNameElection_3_people_with_3_way_Tie` | 3-way tie all spanning E/O |

**Strengths:** Confirms single-name path uses same tie detection; includes within-Other tie not requiring break.

**Weaknesses:**

- **No `numberExtra` scenarios** (defaults to 0).
- **No tie-break count / re-analysis tests.**
- Tie assertions are shallower than normal-election counterparts.

### `TallyServiceTests.cs` — Service integration

| Test | What it covers |
|------|----------------|
| `CalculateNormalElectionAsync_DetectsTies` | Smoke: ties exist in DTO |
| `CalculateNormalElectionAsync_CategorizesSections` | 9 elected + 3 extra + others; **only section-count test** |
| `CalculateNormalElectionAsync_CreateResultTieRecords_ForTiesSpanningSections` | `ResultTie` rows created; `NumToElect > 0` |
| `CalculateNormalElectionAsync_WithAllCandidatesTied_MarksAllAsTied` | All 10 candidates tied at same vote count |
| `CalculateNormalElectionAsync_TieSpanningElectedExtraBoundary_RequiresTieBreak` | Ranks 9–10 tied at E/X boundary |
| `CalculateNormalElectionAsync_TieSpanningExtraOtherBoundary_RequiresTieBreak` | Ties at X/O boundary |
| `CalculateNormalElectionAsync_TieWithinSection_DoesNotRequireTieBreak` | Ranks 7–8 tied within `"E"` |
| `CalculateNormalElectionAsync_Recalculation_ProducesIdenticalResults` | Idempotent re-tally **without** tie-break counts |

**Strengths:** Realistic LSA-scale fixtures (9+2 extras); boundary tests use programmatic vote builders.

**Weaknesses:**

- **`CreateVotesWithTieWithinExtraSectionAsync` exists (lines 856–928) but has no `[Fact]`** — dead test infrastructure.
- Boundary tests assert flags, not specific `RankInExtra` or post-tie-break ordering.
- **No `SaveTieCountsAsync`, `GetTiesAsync`, or tie-break persistence tests.**
- `Recalculation_ProducesIdenticalResults` does not verify tie-break count survival (regression risk once save workflow is fixed).

### `ReportServiceTests.cs` — Display layer (not analyzer)

| Test | What it covers |
|------|----------------|
| `GetMainReport_WithTies_SetsHasTies` | `HasTies` flag; tie-break count in display string |
| `GetMainReport_ExtraResults_ShowsNextRank` | `"Next 1"` label for `Section = "X"` with `rankInExtra: 1` |
| `GetVotesByNum_ShowBreak_SetOnSectionChange` | Section break between `"T"` and `"O"` rows |
| `GetBallotsReport_TiedFilter_ReturnsBallotsWithTiedCandidates` | Tied ballot filter |

**Note:** These seed `Result` rows directly — they validate reporting, not tally/ranking correctness.

### Other files

- **`IneligibleReasonEnumTests.cs`** — includes a tie-break election metadata case; not functional tie logic.
- **`ReportExportServiceTests.cs`** — no tie/extras-specific coverage found.

---

## Coverage Assessment by Topic

### 1. Tie Detection

| Scenario | Covered? | Where |
|----------|----------|-------|
| Equal vote count → `IsTied` | Yes | Multiple tests in Normal + SingleName + TallyService |
| Tie group assignment (`TieBreakGroup`) | Yes | `ElectionAnalyzerNormalTests` (detailed) |
| Within-section tie, no break required (`"E"` only) | Yes | `Election_3_people_with_Tie_Not_Required`, `TieWithinSection_DoesNotRequireTieBreak` |
| Within-section tie, no break required (`"O"` only) | Yes | `Invalid_People_Do_Not_Affect_Results` (single-name) |
| Cross-section tie, break required | Yes | `Election_3_people_with_3_way_Tie`, boundary tests |
| Multiple independent tie groups | Yes | `ElectionWithTwoSetsOfTies` |
| `ForceShowInOther` for O-section members | Yes | Multiple normal-election tests |
| Close-vote flags (`CloseToPrev`/`CloseToNext`) | Partial | Asserted in some tests; threshold (≤3) not isolated |
| Early termination after first O-section tie group | No | Not explicitly tested |

**Verdict:** Tie **detection** is strong for normal elections; good at service level; adequate for single-name.

### 2. Tie-Break Vote Handling

| Scenario | Covered? | Where |
|----------|----------|-------|
| `TieBreakCount` restored on re-analysis | Implicit | Tests write to DB then `RunAnalysis` — persistence path exercised |
| Resolved vs unresolved (`IsTieResolved`) | Yes | Three `ElectionTieWithTieBreak*` tests |
| Unresolved when E and X share same tie-break count | Yes | `ElectionTieWithTieBreakTiedInExtraSection` |
| Tie-break counts affect **final rank/order** | **No** | Tests never compare rank/section before vs after |
| Tie-break counts move candidate between sections | **No** | — |
| `SaveTieCountsAsync` service method | **No** | — |
| `UseOnReports` blocked by unresolved ties | **No** | — |
| Default `TieBreakCount = 0` when required | **No** | — |

**Verdict:** Resolution **flags** are tested; the **ordering effect** of tie-break counts — the primary user-visible outcome — is not.

### 3. Cross-Section Ties (Including Extras)

| Scenario | Covered? | Where |
|----------|----------|-------|
| E / X boundary | Yes | `ElectionWithTwoSetsOfTies`, `TieSpanningElectedExtraBoundary_*`, tie-break tests |
| X / O boundary | Yes | `ElectionWithTwoSetsOfTies`, `TieSpanningExtraOtherBoundary_*` |
| E / X / O single group | Yes | `ElectionTieSpanningTopExtraOther` |
| X / X only (within extras) | **No** | Helper exists, no test |
| `NumToElect` calculation per group | Yes | Asserted in analyzer tests |
| `NumInTie == NumToElect` decrement | Indirect | Not isolated |

**Verdict:** Cross-section ties involving extras are the **best-covered area**. Within-extra-only ties are a clear hole.

### 4. Extras Configuration & `"X"` Section Ranking

| Scenario | Covered? | Where |
|----------|----------|-------|
| Correct count of E / X / O sections | Yes | `CategorizesSections` (9+3+rest) |
| `RankInExtra` assignment (1, 2, 3…) | **No** | Never asserted in analyzer/service tests |
| Multiple extras with distinct ranks | **No** | — |
| `NumberExtra = 0` (no `"X"` section) | **No** | Default in most tests; never explicit |
| Extra display formatting (`"Next N"`) | Yes | `ReportServiceTests` only |
| Tie involving extras + tie-break reordering within X | **No** | — |

**Verdict:** Extras **section assignment** is smoke-tested; **intra-extra ranking** (`RankInExtra`) is not validated in the tally pipeline.

### 5. Manually Entered Tie-Break Results → Final Ordering

The analyzer tests approximate manual entry by:

1. Running initial tally
2. Directly setting `Result.TieBreakCount` on entities
3. Calling `RunAnalysis()` again

This validates the **analyzer's consume path** but:

- Bypasses `TallyService.SaveTieCountsAsync`
- Does not assert ordering changes
- Does not test the production workflow users actually follow

**Verdict:** Partial / indirect coverage only. **Not sufficient** for confidence in the live tie-management feature.

---

## Coverage Map (Visual)

```
                    Detection   Flags/Groups   Re-order   Service API   RankInExtra
Normal analyzer        ████        ████          ░░          N/A           ░░
Single-name analyzer   ███         ██            ░░          N/A           ░░
TallyService           ███         ███           ░░          ░░            ░░
ReportService          ░░          ██            N/A         N/A           █
SaveTieCountsAsync     ░░          ░░            ░░          ░░            ░░

████ = good   ██ = partial   ░░ = missing
```

---

## Next Steps

Actionable test improvements, in recommended priority order:

### Priority 1 — Prove tie-break changes ordering (analyzer)

**Add:** `ElectionAnalyzerNormalTests.TieBreakCounts_ReorderCandidatesAndSections`

- Setup: `numberToElect: 2`, `numberExtra: 1`, three candidates tied at same vote count spanning E/X/O.
- Set tie-break counts that produce a clear reorder (e.g. `3, 2, 0`).
- Assert **before** re-analysis: specific ranks and sections.
- Assert **after** re-analysis: candidate with highest tie-break count is rank 1 `"E"`; lowest drops to `"O"` or `"X"` as expected.
- Assert `RankInExtra` values for any `"X"` candidates.

### Priority 2 — Service-level tie-break workflow

**Add:** `TallyServiceTests.SaveTieCountsAsync_PersistsCountsAndReanalysisUpdatesRanks`

- Run `CalculateNormalElectionAsync` on a boundary-tie fixture.
- Call `SaveTieCountsAsync` with counts (including explicit zeros).
- Re-run tally (or verify auto re-analysis once implemented).
- Assert ranks/sections/`IsTieResolved` in returned `TallyResultDto`.

**Add:** `TallyServiceTests.GetTiesAsync_ReturnsCorrectGroup`

- Smoke test for tie retrieval API used by tie management UI.

### Priority 3 — Activate dead extra-section helper

**Add:** `TallyServiceTests.TieWithinExtraSection_RequiresTieBreak`

- Use existing `CreateVotesWithTieWithinExtraSectionAsync`.
- Assert tied candidates are all `"X"`, `TieBreakRequired = true`, and `ResultTie.NumToElect` reflects extra slots.

**Add:** `ElectionAnalyzerNormalTests.TieWithinExtraOnly_DoesNotRequireTieBreak` (if business rules match within-E/O behavior) **or** confirm it *does* require break and document with test.

### Priority 4 — Extras ranking

**Add:** `ElectionAnalyzerNormalTests.Extras_AssignRankInExtraSequentially`

- `numberToElect: 2`, `numberExtra: 3`, five candidates with distinct vote counts.
- Assert ranks 3–5 have `Section = "X"` and `RankInExtra` = 1, 2, 3 respectively.
- Assert rank 6+ has `Section = "O"` and `RankInExtra = null`.

**Add:** `ElectionAnalyzerNormalTests.NumberExtraZero_NoExtraSection`

- Explicit `numberExtra: 0`; verify no `"X"` sections assigned.

### Priority 5 — Summary gates and persistence

**Add:** `ElectionAnalyzerNormalTests.UnresolvedTies_BlockUseOnReports`

- After tally with required ties: `UseOnReports = false`.
- After setting resolving tie-break counts and re-analyzing: `UseOnReports = true` (with clean ballot/envelope counts).

**Extend:** `TallyServiceTests.Recalculation_PreservesTieBreakCounts`

- Tally → save tie-break counts to DB → re-tally → assert counts still applied and ranks reflect them.

### Priority 6 — Single-name parity

**Add:** `ElectionAnalyzerSingleNameTests` extras + tie-break tests mirroring the top three normal-election tie-break scenarios (at minimum one reorder test).

### Priority 7 — v3 parity (longer term)

Import a known v3 election with extras and tie-breaks; assert v4 `Results` / `ResultTies` match exported snapshot. Complements unit tests with real-world validation.

---

## Related Documentation

- Analyzer behavior review: `docs/analyzer-ranking-tie-review-2026-06-17.md`
- Broader engine assessment: `docs/analysis-engine-review-2026-06-17.md`