# Known-good v3 packages — what to drop in

Issue #168 still needs **2–3 real v3 election packages** that were analyzed in v3:

| Folder name (suggested) | What it should cover |
| --- | --- |
| `simple` | Straightforward LSA (or similar), no interesting ties, extras unused or empty |
| `ties` | At least one real tie (ideally a required tie-break) |
| `extras` | `NumberExtra` > 0 with people in the extra section |

## What Glen should export

From **TallyJ v3**, after **Analyze** has been run (so the export includes `resultSummary`, `result`, and `resultTie`):

1. Export the election package as TallyJ v3 XML (`TallyJ2`, namespace `urn:tallyj.bahai:v2`).
2. Create a folder here named for the election (no spaces if possible), for example `known-good-v3/lsa-2024-ties/`.
3. Save the export as `package.xml`.
4. Add an optional `manifest.json`:

```json
{
  "name": "lsa-2024-ties",
  "scenario": "ties",
  "source": "known-good-v3",
  "notes": "Exported from v3 after Analyze on <date>. Redact voter contact if needed."
}
```

5. Do **not** add `expected.json` unless you need to override a field the XML does not carry. The default expected snapshot is the analysis rows already in the package.

## What not to supply

- Synthetic or hand-typed totals that were not produced by v3 Analyze
- Packages from before Analyze (no `result` / `resultSummary` rows)
- Live Azure SQL dumps or connection strings — local files only
- Copies of `synthetic-pipeline/` packages labeled as known-good

## Privacy

Prefer test or already-public demo elections. If a real roll must be used, strip email/phone from the XML before committing.

## How to run

```bash
dotnet test Backend.Tests/Backend.Tests.csproj --filter FullyQualifiedName~V3AnalysisComparison
```

When a `package.xml` appears in a child folder, `KnownGoodV3_WhenPackagesArePresent_ImportAnalyzeAndDiff` imports it, re-runs Analyze, and diffs `ResultSummary`, `ResultTies`, and counts.

See `docs/V3_ANALYSIS_COMPARISON.md`.
