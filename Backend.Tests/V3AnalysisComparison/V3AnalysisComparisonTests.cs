using System.Text.Json;
using Backend.Tests.UnitTests;

namespace Backend.Tests.V3AnalysisComparison;

/// <summary>
/// Issue #168: import → Analyze → diff ResultSummary, ResultTies, and counts.
/// Synthetic fixtures prove the harness. Known-good v3 packages are not in
/// this repo; drop them under Fixtures/V3AnalysisComparison/known-good-v3/.
/// </summary>
public class V3AnalysisComparisonTests : ServiceTestBase
{
    public static IEnumerable<object[]> SyntheticFixtures()
    {
        var names = V3AnalysisComparisonHarness
            .DiscoverFixtureDirectories(V3AnalysisComparisonHarness.SyntheticPipelineFolder)
            .Select(dir => Path.GetFileName(dir)!)
            .ToList();

        if (names.Count == 0)
        {
            yield return [""];
            yield break;
        }

        foreach (var name in names)
        {
            yield return [name];
        }
    }

    [Theory]
    [MemberData(nameof(SyntheticFixtures))]
    public async Task SyntheticPipeline_ImportAnalyzeAndDiff(string fixtureName)
    {
        Assert.False(string.IsNullOrEmpty(fixtureName),
            "synthetic-pipeline should ship simple, ties, and extras fixtures.");

        var dir = Path.Combine(
            V3AnalysisComparisonHarness.FixturesRoot,
            V3AnalysisComparisonHarness.SyntheticPipelineFolder,
            fixtureName);

        var run = await V3AnalysisComparisonHarness.RunAsync(Context, dir);

        Assert.False(run.Manifest?.IsKnownGoodV3,
            $"{fixtureName} is a pipeline-proof fixture, not a known-good v3 election.");
        Assert.Empty(run.Mismatches);
    }

    [Fact]
    public void SyntheticPipeline_ShipsSimpleTiesAndExtras()
    {
        var names = V3AnalysisComparisonHarness
            .DiscoverFixtureDirectories(V3AnalysisComparisonHarness.SyntheticPipelineFolder)
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("simple", names);
        Assert.Contains("ties", names);
        Assert.Contains("extras", names);
    }

    [Fact]
    public async Task KnownGoodV3_WhenPackagesArePresent_ImportAnalyzeAndDiff()
    {
        var fixtures = V3AnalysisComparisonHarness.DiscoverFixtureDirectories(
            V3AnalysisComparisonHarness.KnownGoodV3Folder);

        if (fixtures.Count == 0)
        {
            var readme = Path.Combine(
                V3AnalysisComparisonHarness.FixturesRoot,
                V3AnalysisComparisonHarness.KnownGoodV3Folder,
                "README.md");
            Assert.True(File.Exists(readme),
                "known-good-v3/README.md must explain which packages Glen should drop in.");
            return;
        }

        var failures = new List<string>();
        foreach (var dir in fixtures)
        {
            var run = await V3AnalysisComparisonHarness.RunAsync(Context, dir);
            if (run.Mismatches.Count > 0)
            {
                failures.Add($"{run.FixtureName} (expected from {run.ExpectedSource}):{Environment.NewLine}" +
                             AnalysisComparisonDiff.FormatReport(run.Mismatches));
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine + Environment.NewLine, failures));
    }

    [Fact]
    public void Diff_ReportsSummaryTieAndCountMismatches()
    {
        var expected = new AnalysisComparisonSnapshot
        {
            Summaries =
            [
                new ResultSummarySnapshot { ResultType = "F", NumVoters = 3, TotalVotes = 6 }
            ],
            Ties =
            [
                new ResultTieSnapshot { TieBreakGroup = 1, NumInTie = 2, NumToElect = 1, TieBreakRequired = true }
            ],
            Counts =
            [
                new PersonCountSnapshot { LastName = "Able", FirstName = "Ada", VoteCount = 3, Rank = 1, Section = "E" }
            ]
        };
        var actual = new AnalysisComparisonSnapshot
        {
            Summaries =
            [
                new ResultSummarySnapshot { ResultType = "F", NumVoters = 2, TotalVotes = 6 }
            ],
            Ties =
            [
                new ResultTieSnapshot { TieBreakGroup = 1, NumInTie = 3, NumToElect = 1, TieBreakRequired = true }
            ],
            Counts =
            [
                new PersonCountSnapshot { LastName = "Able", FirstName = "Ada", VoteCount = 2, Rank = 1, Section = "E" }
            ]
        };

        var mismatches = AnalysisComparisonDiff.Diff(expected, actual);

        Assert.Contains(mismatches, m => m.Contains("NumVoters") && m.Contains("3") && m.Contains("2"));
        Assert.Contains(mismatches, m => m.Contains("ResultTie[1].NumInTie"));
        Assert.Contains(mismatches, m => m.Contains("Count[Able, Ada].VoteCount"));
    }

    [Fact]
    public async Task ExpectedJson_OverridesImportedRows()
    {
        var fixtureDir = Path.Combine(
            V3AnalysisComparisonHarness.FixturesRoot,
            V3AnalysisComparisonHarness.SyntheticPipelineFolder,
            "simple");
        var expectedPath = Path.Combine(Path.GetTempPath(), $"v3-compare-expected-{Guid.NewGuid():N}.json");
        var workDir = Path.Combine(Path.GetTempPath(), $"v3-compare-fix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        File.Copy(Path.Combine(fixtureDir, V3AnalysisComparisonHarness.PackageFileName),
            Path.Combine(workDir, V3AnalysisComparisonHarness.PackageFileName));
        File.Copy(Path.Combine(fixtureDir, V3AnalysisComparisonHarness.ManifestFileName),
            Path.Combine(workDir, V3AnalysisComparisonHarness.ManifestFileName));

        var wrongExpected = new AnalysisComparisonSnapshot
        {
            Summaries = [new ResultSummarySnapshot { ResultType = "F", NumVoters = 99 }],
            Ties = [],
            Counts = []
        };
        await File.WriteAllTextAsync(expectedPath, JsonSerializer.Serialize(wrongExpected));
        File.Copy(expectedPath, Path.Combine(workDir, V3AnalysisComparisonHarness.ExpectedFileName));

        try
        {
            var run = await V3AnalysisComparisonHarness.RunAsync(Context, workDir);
            Assert.Equal(V3AnalysisComparisonHarness.ExpectedFileName, run.ExpectedSource);
            Assert.NotEmpty(run.Mismatches);
        }
        finally
        {
            File.Delete(expectedPath);
            Directory.Delete(workDir, recursive: true);
        }
    }
}
