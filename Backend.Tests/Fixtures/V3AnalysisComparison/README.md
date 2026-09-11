# v3 analysis comparison fixtures (issue #168)

This folder is the drop-in format for comparing v4 Analyze output to a v3 election package.

**There are no known-good production v3 packages in this repository.** The `synthetic-pipeline/` fixtures only prove that import → Analyze → diff runs. Do not treat their numbers as live-election results.

## Layout

```text
V3AnalysisComparison/
  synthetic-pipeline/          # shipped; pipeline proof only
    simple/package.xml
    ties/package.xml
    extras/package.xml
  known-good-v3/               # empty until Glen adds real packages
    README.md
    <election-name>/
      package.xml              # required: TallyJ v3 XML export
      manifest.json            # optional
      expected.json            # optional override
```

Each fixture directory must contain `package.xml` (TallyJ v2/v3 XML, schema `backend/Schemas/TallyJv2-Export.xsd`).

## How expected values are chosen

1. If `expected.json` is present, that file is the expected snapshot.
2. Otherwise the harness snapshots **imported** `resultSummary` (types `C` and `F`), `resultTie`, and `result` rows — the v3 Analyze output already in the package.
3. If neither exists, the test fails with a message that the package must be exported **after Analyze** in v3.

The harness never invents “known-good v3” numbers.

## What is compared

After a fresh v4 Analyze (same analyzer as the Results page, without the product review/reconciliation gates):

- **ResultSummary** `C` and `F`: voter/envelope counts, ballots received/spoiled, spoiled votes, total votes, needing review, `UseOnReports`
- **ResultTies**: group, size, seats in the tie, required, resolved
- **Counts**: per person (last + first name) vote total, rank, section, tie flags, extra rank

GUIDs are remapped on import, so people are matched by name.

Manual summaries (`ResultType=M`) are kept by Analyze and applied as overrides; they are not themselves the expected/actual pair.

## Local only

Tests use the Backend.Tests InMemory/SQLite factory. Do not point this harness at Azure SQL.
