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
