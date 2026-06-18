# Analyzer Ranking, Ordering & Tie Resolution Review

**Date:** June 17, 2026  
**Scope:** Review only — how the v4 analysis engine determines final candidate order, detects and resolves ties, and handles extra positions.  
**Primary code:** `backend/Services/Analyzers/`, `backend/Services/TallyService.cs`

---

## Executive Summary

The analysis engine assigns candidate order in a single pass after vote counting: sort by vote count, then tie-break count, then name; assign rank and section (Elected / Extra / Other); then detect ties and flag those requiring manual tie-break votes. **Tie-break vote counts are already modeled and consumed by the analyzer** — but only when a full re-analysis runs after counts are saved. The live save workflow persists counts without re-running the analyzer, so rankings and sections do not update until someone manually runs Calculate Tally again.

Extra positions are supported via `Election.NumberExtra` and `Result.Section == "X"`, with dedicated tie logic for cross-section ties involving extras. There is no separate "position type" entity beyond the three section codes.

---

## 1. How Final Order/Ranking Is Determined

### Pipeline overview

All analyzers inherit from `ElectionAnalyzerBase`. After vote counting (`CountVotesAsync`), final ordering happens in `FinalizeResultsAndTies()`:

1. Remove candidates with zero votes (`Results.RemoveAll(r => (r.VoteCount ?? 0) == 0)`)
2. Restore any previously saved tie-break counts from the database
3. `DetermineOrderAndSections()` — assign `Rank`, `Section`, and `RankInExtra`
4. `AnalyzeForTies()` — detect ties, assign tie groups, set flags

### Sorting algorithm (`DetermineOrderAndSections`)

Candidates are sorted with a three-level key, then assigned consecutive ranks starting at 1:

| Priority | Field | Direction |
|----------|-------|-----------|
| 1 | `VoteCount` | Descending |
| 2 | `TieBreakCount` | Descending (null treated as 0) |
| 3 | Name sort key | Ascending (`PersonNameHelper.GetSortKey(person)` — `FullNameFl` or `RowId`) |

```253:290:backend/Services/Analyzers/ElectionAnalyzerBase.cs
    internal void DetermineOrderAndSections()
    {
        var numberToElect = TargetElection.NumberToElect!.Value;
        var numberExtra = TargetElection.NumberExtra ?? 0;
        // ...
        foreach (var result in Results
            .OrderByDescending(r => r.VoteCount)
            .ThenByDescending(r => r.TieBreakCount ?? 0)
            .ThenBy(r => { /* name sort key */ }))
        {
            ordinalRank++;
            result.Rank = ordinalRank;
            // section assignment ...
        }
    }
```

### Section assignment (rank → section)

Sections are derived purely from ordinal rank and election configuration — not from vote thresholds:

| Condition | `Section` | Meaning |
|-----------|-----------|---------|
| `Rank <= NumberToElect` | `"E"` | Elected |
| `Rank <= NumberToElect + NumberExtra` | `"X"` | Extra |
| Otherwise | `"O"` | Other (not elected, not extra) |

For candidates in section `"X"`, a secondary `RankInExtra` is assigned (1-based within the extra block).

### Vote counting (input to ranking)

- **Normal elections** (`ElectionAnalyzerNormal`): counts one vote per valid vote on `BallotStatus.Ok` ballots where `VoteStatus == Ok`
- **Single-name elections** (`ElectionAnalyzerSingleName`): counts per vote record using `SingleNameElectionCount ?? 1`, ignoring ballot status for counting (vote-level model)
- Both paths feed the same `FinalizeResultsAndTies()` / ranking logic in the base class

### What ranking does *not* use

- Election type (LSA, NSA, Unit Convention, etc.) — no type-specific ranking rules
- Separate position-type entities or per-slot configuration
- Random or manual rank overrides (except via tie-break counts influencing the secondary sort)

---

## 2. Tie Detection and Handling

### Detection (`AnalyzeForTies`)

Results are walked in **rank order** (after initial sort). Two consecutive candidates with **equal `VoteCount`** are marked tied:

- `IsTied = true` on both
- Both receive the same `TieBreakGroup` (auto-incremented integer)
- `CloseToPrev` / `CloseToNext` flags set when vote gap ≤ 3 (`ThresholdForCloseVote`)

**Early termination:** once the scan passes the first tied group that reaches section `"O"`, and then encounters candidates with *different* vote counts, the loop **breaks** — no further tie groups are detected below that point.

### When tie-break is required (`AnalyzeTieGroup`)

Each tie group is analyzed against which sections its members occupy:

| Group composition | `TieBreakRequired` | `TieBreakCount` |
|-------------------|-------------------|-----------------|
| All in `"E"` only | `false` | Cleared to `null` |
| All in `"O"` only | `false` | Cleared to `null` |
| Spans `"E"` / `"X"` / `"O"` boundaries (any mix) | `true` | Defaulted to `0` if null |

`ResultTie.NumToElect` is computed per group to indicate how many positions are contested:

- If group spans elected and other sections: `NumToElect +=` count of members in `"E"`
- If group includes extras (and not only top): `NumToElect +=` count of members in `"X"`
- If `NumInTie == NumToElect`, decrement `NumToElect` by 1 (one slot is guaranteed)

`ForceShowInOther = true` is set on `"O"` members when the tie group also includes elected or extra candidates (so they remain visible in reports despite being in Other).

### Resolution check (`IsResolved`)

A tie group is resolved when, for every member, no other member still "competes" at the same tie-break level:

```
stillTied = any other member with same TieBreakCount
            AND (other.Section != my.Section OR my.Section == "X")
```

Implications:

- Two tied candidates **both in `"E"`** with the same tie-break count → **resolved** (same section, not X)
- Two tied candidates in **`"E"` and `"X"`** with the same tie-break count → **not resolved**
- Two tied candidates **both in `"X"`** with the same tie-break count → **not resolved** (X section always participates in cross-section resolution logic)

`ResultSummaryFinal.UseOnReports` is `true` only when all `ResultTie.IsResolved == true` (among other gates).

### Manual tie-break vote support — data model

Manual tie-break votes **are supported in the data model and analyzer logic**:

| Entity / field | Role |
|----------------|------|
| `Result.TieBreakCount` | Per-candidate manual tie-break vote count |
| `Result.TieBreakGroup` | Groups tied candidates |
| `Result.TieBreakRequired` | Whether manual entry is needed |
| `Result.IsTieResolved` | Whether this candidate's tie is resolved |
| `ResultTie` | Group-level metadata (`NumToElect`, `NumInTie`, `IsResolved`) |

**Persistence across re-runs:** before clearing results, `PrepareForAnalysisAsync` snapshots existing `TieBreakCount` values into `_previousTieBreakCounts` keyed by `PersonGuid`. After re-counting votes, counts are restored in `FinalizeResultsAndTies` before `DetermineOrderAndSections` re-sorts.

### Manual tie-break workflow — API and UI

| Layer | Implementation |
|-------|----------------|
| Save API | `POST /api/Results/election/{electionGuid}/ties/save` → `TallyService.SaveTieCountsAsync` |
| Read API (per group) | `GET /api/Results/{electionGuid}/{tieBreakGroup}/ties` → `TallyService.GetTiesAsync` |
| UI | `frontend/src/pages/results/TieManagementPage.vue` — numeric inputs per tied candidate |
| Store | `resultStore.saveTieCounts()` |

### Critical workflow gap

`SaveTieCountsAsync` **saves counts to the database but does not re-run the analyzer**:

```474:480:backend/Services/TallyService.cs
            if (reAnalysisNeeded)
            {
                _logger.LogInformation("All ties resolved, triggering re-analysis for election {ElectionGuid}", electionGuid);
                // Note: In a real implementation, you might want to call CalculateNormalElectionAsync here
                // But for now, we'll just log it
            }
```

The response sets `ReAnalysisTriggered = true`, and the frontend shows an info message — but **rank, section, and `IsResolved` flags do not update** until someone manually runs Calculate Tally. Unit tests work around this by calling `RunAnalysis()` again after setting `TieBreakCount` directly on entities.

### Additional workflow issues

- **Frontend save filter:** `TieManagementPage.vue` only sends candidates with `tieBreakCount > 0`. Zero counts (meaningful when the analyzer defaults required ties to `0`) are omitted.
- **Frontend validation:** only requires tie-break counts when `tie.section === 'E'`, but the backend also requires them for ties spanning `"X"` / `"O"` boundaries.
- **API mismatch:** frontend calls `GET /api/results/election/{electionGuid}/ties` (all groups), but the backend only exposes per-group `GET /api/Results/{electionGuid}/{tieBreakGroup}/ties`. Tie management page may fail to load unless a list endpoint is added or the frontend fetches per group from tally results.

---

## 3. Extra Positions and Position Types

### Extra positions — fully supported

Extras are a first-class concept, driven by election configuration:

| Config / field | Location | Purpose |
|----------------|----------|---------|
| `Election.NumberExtra` | Election entity, DTOs, validators, import/export | How many positions beyond `NumberToElect` are "extra" |
| `Result.Section == "X"` | Result entity | Marks a candidate as extra |
| `Result.RankInExtra` | Result entity | 1-based rank within the extra block |
| `"Next N"` display | `ReportService.GetMainReportAsync` | Extra candidates shown as `"Next {RankInExtra}"` |

Unit Convention elections (`ElectionType == "Con"`) use the **same mechanism** — `NumberExtra` on the election record. There is no Convention-specific analyzer branch.

### Section codes (the only "position types")

The engine recognizes exactly three result sections:

| Code | Label (localized) | Description |
|------|-------------------|-------------|
| `"E"` | Elected | Top `NumberToElect` ranks |
| `"X"` | Extra | Next `NumberExtra` ranks after elected |
| `"O"` | Other | Everyone else with votes |

There is **no** separate position-type table, named role slots, or per-position configuration beyond `NumberToElect` + `NumberExtra`.

### How extras appear downstream

- **Reports:** `ReportService` takes top `NumberToElect + NumberExtra` results; extras use `RankInExtra` in display
- **Public display:** `PublicService` shows elected (`"E"`) and additional (`"X"` or overflow `"E"`) candidates separately
- **Presentation view:** `TallyService.GetPresentationDataAsync` splits elected vs extra lists by section

---

## 4. Where Tie-Break Counts Must Be Read and Used

### In the analyzer (core)

| Step | Method | How `TieBreakCount` is used |
|------|--------|----------------------------|
| **Load** | `PrepareForAnalysisAsync` | Read existing `Result.TieBreakCount` from DB → `_previousTieBreakCounts` |
| **Restore** | `FinalizeResultsAndTies` | Copy saved counts onto freshly counted `Result` rows by `PersonGuid` |
| **Re-sort** | `DetermineOrderAndSections` | Secondary sort key — higher tie-break count ranks above lower |
| **Re-section** | `DetermineOrderAndSections` | Rank change from tie-break can move candidates between E / X / O |
| **Re-evaluate ties** | `AnalyzeTieGroup` | Compare counts across sections to set `IsTieResolved` / `ResultTie.IsResolved` |
| **Gate reports** | `FinalizeSummaries` | `UseOnReports` requires all tie groups resolved |

### Outside the analyzer (persistence & display)

| Location | Role |
|----------|------|
| `TallyService.SaveTieCountsAsync` | Write user-entered counts to `Result.TieBreakCount` — **should trigger re-analysis** |
| `TallyService.GetTiesAsync` | Read counts for tie management UI |
| `TallyService.GetTallyResultsAsync` | Expose `TieBreakRequired`, `TieBreakGroup` in results DTO |
| `ReportService` | Append `" / {TieBreakCount}"` to vote display when `TieBreakRequired` |
| Import/export (`TallyJv2ElectionImportService`, `JsonElectionImportExportService`) | Round-trip `TieBreakCount` on results |

**Single integration point for live workflow:** after `SaveTieCountsAsync` persists counts, call `CalculateNormalElectionAsync` (or `CalculateSingleNameElectionAsync` for `"Oth"` elections) so the analyzer reloads, re-sorts, and re-evaluates ties.

---

## 5. Likely Changes Needed

### A. Manual tie-break results affecting final ordering

| Change | Effort | Details |
|--------|--------|---------|
| **Wire save → re-analyze** | Small | In `SaveTieCountsAsync`, call the appropriate `Calculate*ElectionAsync` when `reAnalysisNeeded` (or on every save). This is the single most important fix. |
| **Frontend: send all counts** | Small | Include every tied candidate in the save payload, including explicit `0` values. |
| **Frontend: refresh after save** | Small | After successful save + re-analysis, reload tally results (not just tie details) so rank/section updates are visible. |
| **Frontend: validation** | Small | Use `tieBreakRequired` from tally results / tie DTO instead of checking `section === 'E'` only. |
| **List ties endpoint** | Medium | Add `GET /api/Results/election/{electionGuid}/ties` returning all tie groups, or change frontend to iterate `TieBreakGroup` values from `GetTallyResultsAsync().ties`. |
| **Election type routing** | Small | `SaveTieCountsAsync` re-analysis must pick normal vs single-name analyzer based on `ElectionType`. |
| **SignalR / UX** | Small | Notify clients that re-tally completed (existing `SendTallyProgressAsync` pattern). |

### B. Proper handling of extras (including ties involving extras)

The analyzer already contains extra-aware tie logic (tested in `ElectionAnalyzerNormalTests` and `TallyServiceTests`). Gaps are in workflow and UX, not core algorithms:

| Scenario | Current behavior | Likely improvement |
|----------|------------------|-------------------|
| Tie within extras only (`"X"` only) | `TieBreakRequired = true` (because `groupInExtra` triggers it even when `!groupInTop`) | Confirm this matches v3 / Baha'i election rules; add v3 parity test |
| Tie at E/X boundary | `TieBreakRequired = true`, `NumToElect` counts elected slots | Covered by tests; ensure UI explains how many to elect from group |
| Tie at X/O boundary | `TieBreakRequired = true`, `ForceShowInOther` on O members | Covered by tests |
| Tie spanning E/X/O | Single tie group, all require tie-break | Covered by `ElectionTieSpanningTopExtraOther` test |
| Tie-break resolves but rank unchanged | Tests assert `IsTieResolved` but not always rank movement | Add tests asserting rank/section changes when tie-break counts differ (e.g. `2,1,1` should reorder) |
| Extras with `NumberExtra = 0` | No `"X"` section assigned | Expected; document that extras are optional |

No new entity for "position types" appears necessary unless product requirements demand per-slot labels beyond E/X/O.

### C. Optional hardening

- **Validate tie-break totals** — ensure entered counts are consistent with `ResultTie.NumToElect` before save
- **Partial save handling** — `reAnalysisNeeded` currently triggers only when *all* group members have a count; consider re-analyzing on any save so partial counts update ordering incrementally
- **v3 parity regression** — import known v3 elections with extras and tie-breaks, assert v4 output matches (see `docs/analysis-engine-review-2026-06-17.md`)

---

## Key Files Reference

| File | Responsibility |
|------|----------------|
| `backend/Services/Analyzers/ElectionAnalyzerBase.cs` | Ranking, sections, tie detection/resolution, tie-break count restore |
| `backend/Services/Analyzers/ElectionAnalyzerNormal.cs` | Vote counting (ballot-based) |
| `backend/Services/Analyzers/ElectionAnalyzerSingleName.cs` | Vote counting (vote-based) |
| `backend/Services/TallyService.cs` | Tally entry point, tie-break save/load, results DTOs |
| `backend/Entities/Result.cs` | `Rank`, `Section`, `RankInExtra`, tie-break fields |
| `backend/Entities/ResultTie.cs` | Tie group metadata |
| `backend/Controllers/ResultsController.cs` | Tie-break API endpoints |
| `frontend/src/pages/results/TieManagementPage.vue` | Tie-break data entry UI |
| `Backend.Tests/UnitTests/ElectionAnalyzerNormalTests.cs` | Tie + tie-break scenario tests |
| `Backend.Tests/UnitTests/TallyServiceTests.cs` | Integration-level tie boundary tests |

---

## Test Coverage Summary

Tie and ranking behavior is **well covered at the unit level**:

- Tie within elected section — no tie-break required
- 3-way tie at elected cutoff — tie-break required
- Tie spanning E/X, X/O, E/X/O
- Tie-break count persistence across re-analysis
- Tie-break resolution flags (`IsTieResolved`, `ResultTie.IsResolved`)
- `NumToElect` decrement when `NumInTie == NumToElect`

**Not covered end-to-end:** `SaveTieCountsAsync` → automatic re-analysis → updated ranks in API response. **Not covered:** v3 golden-file parity for ranking with extras and tie-breaks.