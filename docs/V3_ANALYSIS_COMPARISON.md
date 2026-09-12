# Compare v4 analysis to v3 election packages

Issue [#168](https://github.com/glittle/TallyJ-4/issues/168). This is a **comparison harness**, not a rewrite of the analysis engine.

Related work that stays out of this harness: [#185](https://github.com/glittle/TallyJ-4/issues/185) (teller/analysis **reports** vs v3) and [#198](https://github.com/glittle/TallyJ-4/issues/198) (tie-break / extras product follow-up).

## Current status

| Item | Status |
| --- | --- |
| Search of this repo for v3 packages / golden results | None found (no XML/JSON election dumps, no prior comparison harness) |
| Automated import → Analyze → diff | Shipped in `Backend.Tests/V3AnalysisComparison/` |
| Synthetic fixtures (`simple`, `ties`, `extras`) | Shipped — **pipeline proof only** |
| Known-good v3 packages | **Not in the repo** — see what to supply below |

Synthetic numbers are constructed so the current v4 analyzer matches the analysis rows in those tiny packages. They are not production v3 elections.

## Run locally (no Azure SQL)

```bash
dotnet test Backend.Tests/Backend.Tests.csproj --filter FullyQualifiedName~V3AnalysisComparison
```

The harness uses the same InMemory `MainDbContext` as other Backend.Tests analyzer/import tests. Do not connect it to Azure SQL. A local Docker SQL / `SeedOnStartup` database is fine for **manual** import + Analyze in the app; the automated diffs do not use that database.

## Fixture format

Drop files under `Backend.Tests/Fixtures/V3AnalysisComparison/`:

```text
known-good-v3/<short-name>/
  package.xml       # required — TallyJ v3 XML export
  manifest.json     # optional — name, scenario, source: "known-good-v3"
  expected.json     # optional — override snapshot (same shape as the harness DTO)
```

`package.xml` must validate against `backend/Schemas/TallyJv2-Export.xsd`. Export **after Analyze** in v3 so the file contains `resultSummary`, `result`, and `resultTie`.

Expected values:

1. `expected.json` if present
2. Else the imported v3 analysis rows (`C`/`F` summaries, ties, person counts)
3. Else the test fails and asks for a post-Analyze export

People are matched by last + first name because import remaps GUIDs.

## What Glen needs to supply

Two or three **real** v3 packages, analyzed in v3:

1. Simple election (no interesting ties; extras unused or empty)
2. Ties (at least one real tie / tie-break)
3. Extras (`NumberExtra` > 0 with people in section X)

Put each in `Backend.Tests/Fixtures/V3AnalysisComparison/known-good-v3/<name>/package.xml`. Prefer test or demo elections; strip email/phone if a real roll is used.

Details: `Backend.Tests/Fixtures/V3AnalysisComparison/known-good-v3/README.md`.

## What the automated test does vs still manual

**Automated (this repo)**

- Discover fixtures
- Import via `TallyJv3ElectionImportService`
- Run `ElectionAnalyzerNormal` or `ElectionAnalyzerSingleName` (`ElectionType=Oth` or `manifest.analyzer=singlename`)
- Diff `ResultSummary` (C/F), `ResultTies`, and per-person counts

**Not automated here**

- Product Analyze gates (ballots needing review, count reconciliation) — those live on `TallyService.Calculate*`
- Report HTML/PDF vs v3 (#185)
- Live UI Analyze click-through
- Claiming parity with a production election until a real package is in `known-good-v3/`

**Manual (local Docker SQL + SeedOnStartup, or your usual local SQL)**

- Dashboard → load the same v3 package
- Run Analyze
- Read ResultSummary / ties / counts next to the v3 UI or the export
