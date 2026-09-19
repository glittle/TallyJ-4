# Election Analysis Engine

## Status: active
## Evidence: confirmed (maintainer + issue #168)

The core analysis engine is the highest-risk component in TallyJ v4. It must produce correct results against known-good v3 data and handle ties, mixed voting methods, and edge cases.

### Why this is treated as Critical
- Incorrect analysis directly corrupts election results.
- Historical source of subtle bugs in earlier versions.
- Must be validated with dedicated test elections before other work is considered complete.

### Design posture
Risk-first: prove analysis correctness before polishing secondary features or UI.

### Related
- Ballot validation (must catch problems before analysis)
- Election state management (analysis only runs in appropriate states)

## v3 comparison is a harness, not an engine rewrite

**Status:** active  
**Evidence:** confirmed (issue #168 remaining checklist; no v3 packages in this repo)  
**Source:** issue #168  
**Revisit when:** 2–3 known-good v3 packages (simple + ties + extras) are added under `Backend.Tests/Fixtures/V3AnalysisComparison/known-good-v3/`

Remaining #168 work is import → Analyze → diff of `ResultSummary`, `ResultTies`, and person counts. The analyzer, tie-break, and extras handling already exist (#198 is the product follow-up). Report HTML/PDF vs v3 is #185.

**Chosen:** a reusable test harness that imports a TallyJ v3 XML package, snapshots expected analysis from `expected.json` or from analysis rows already in the package, re-runs the v4 analyzer, and diffs C/F summaries, ties, and name-keyed counts. Synthetic `simple` / `ties` / `extras` packages ship only to prove that pipeline. They are not production elections.

**Rejected alternative:** invent “known-good v3” totals and assert they match. Rejected — there are no golden files or real packages in the tree; fabricated numbers would claim parity that was never checked.

**Rejected alternative:** rewrite or “port” the analyzer as the #168 deliverable. Rejected — the issue is comparison against known-good v3 results.

**Rejected alternative:** point automated tests at Azure SQL. Rejected — Backend.Tests stay on local InMemory/SQLite; live comparison in the app can use local Docker SQL + SeedOnStartup.

**Rejected alternative:** require a separate `expected.json` for every real package. Rejected — a v3 export after Analyze already carries `resultSummary` / `result` / `resultTie`; import remaps GUIDs, so the harness snapshots those rows (by name) before re-analysis.

## Tie-break counts: save, 0, and unset

**Status:** active  
**Evidence:** confirmed (v3 `SaveTieCounts` + `ElectionAnalyzerCore`; issue #198 remaining items)  
**Source:** issue #198; TallyJ-3.0 `Analyze.cshtml.js` `saveTieCounts` and `ElectionAnalyzerCore.AnalyzeTieGroup`  
**Revisit when:** known-good v3 packages with extras/tie-break land for #168

v3 always re-ran analysis after saving tie-break counts (the Analyze button is “Save Counts & Re-run Analysis”). It sent every input, including 0 (blank was coerced to 0). All-0 was accepted and stayed unresolved because every member still had the same count. After a required-tie analysis, v3 filled missing `TieBreakCount` with 0, so default and explicit 0 were the same.

**Chosen:**
- `SaveTieCountsAsync` re-analyzes whenever any count is persisted, including a single member of a group and all-0. Partial counts already change rank (`ThenByDescending` on `TieBreakCount ?? 0`).
- All-0 is valid input. Ranking treats every 0 as equal, so the tie stays unresolved and `UseOnReports` stays false.
- Persist null for “not entered” and 0 for an explicit runoff result. Required ties no longer coerce null to 0. Sort and still-tied checks still treat null as 0.
- The tie-management page sends explicit 0 when a field is cleared (otherwise a previous count would stay on the server) and refreshes stored results after save.
- Reports show `/ {count}` only when a count was entered (including 0). Unset omits the suffix. Main `HasTies` is `TieBreakRequired` on the elected/extra rows, not a slash in the display string.

**Rejected alternative:** re-analyze only when every group member `HasValue`. Rejected — that is not v3 behavior, and it would skip re-rank after a partial save once required ties stop filling 0.

**Rejected alternative:** reject all-0 as invalid. Rejected — v3 accepted those values; they simply do not resolve the tie.

**Rejected alternative:** keep filling required ties with 0 (`??= 0`). Rejected — that is what made default and explicit 0 indistinguishable, which #198 asked to separate.

**Rejected alternative:** concatenate `TieBreakCount` whenever the tie is required. Rejected — that printed `" / "` (C#) or `" / null"` (Vue) for unset, and made `HasTies` false until a count was typed.

## Analyze manual voter counts persist M without re-running Analyze

**Status:** active  
**Evidence:** confirmed (v3 `AfterController.SaveManual` / `ResultsModel.SaveManualResults`; issue #186)  
**Source:** TallyJ-3.0 Analyze count table (Calculated / Override / Final); v4 count-reconciliation gate  
**Revisit when:** Analyze is allowed to run without a reconciled Front Desk vs ballot report

v3’s Analyze page had Eligible Voters and method counts (In Person, Dropped Off, Mailed In, Called In, custom) with an Override column. Save Values wrote `ResultType = M`. Online and Imported stayed calculated-only. Analyzer `CombineCalcAndManualSummaries` already applies `manual ?? calculated` into Final.

v3 re-ran analysis immediately after SaveManual. v4 Save Values only persists M (`GET/POST .../manual-counts`). The next Calculate applies the overrides and rewrites stored F via `CombineCalcAndManualSummaries`. That keeps Analyze behind the count-reconciliation / pending-online gates (#190).

The panel API’s Final column is always `manual ?? calculated`. It does not read stored F. After Save Values, Override and Final must agree even when an old F row is still on disk.

This table is v3 count-override parity (method counts and optional Eligible Voters). It is not a special-person workflow.

**Rejected alternative:** re-analyze on save, matching v3. Rejected — v4 Analyze is blocked while Front Desk and ballots do not reconcile; a silent re-run would skip that gate.

**Rejected alternative:** show stored F as the panel Final. Rejected — Save Values would leave Final stale until Calculate.

**Rejected alternative:** invent override columns for Online or Imported. Rejected — v3 left those calculated-only.

**Rejected alternative:** treat this table as #198 tie/extra counts. Rejected — `SaveTieCounts` is a different Analyze control.
