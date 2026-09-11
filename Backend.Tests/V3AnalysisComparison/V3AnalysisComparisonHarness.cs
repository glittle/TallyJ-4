using System.Text.Json;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Services;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Backend.Tests.V3AnalysisComparison;

/// <summary>
/// Import a TallyJ v3 XML package, run the v4 analyzer, and diff
/// ResultSummary / ResultTies / person counts against expected values.
/// Expected comes from <c>expected.json</c> or from analysis rows already
/// in the imported package (v3 Analyze output). Does not invent numbers.
/// </summary>
public static class V3AnalysisComparisonHarness
{
    public const string SyntheticPipelineFolder = "synthetic-pipeline";
    public const string KnownGoodV3Folder = "known-good-v3";
    public const string PackageFileName = "package.xml";
    public const string ExpectedFileName = "expected.json";
    public const string ManifestFileName = "manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public static string? TryGetFixturesRoot()
    {
        var fromOutput = Path.Combine(AppContext.BaseDirectory, "Fixtures", "V3AnalysisComparison");
        if (Directory.Exists(fromOutput))
        {
            return fromOutput;
        }

        var fromCwd = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "Fixtures",
            "V3AnalysisComparison"));
        return Directory.Exists(fromCwd) ? fromCwd : null;
    }

    public static string FixturesRoot =>
        TryGetFixturesRoot()
        ?? throw new DirectoryNotFoundException(
            "V3 analysis comparison fixtures were not found. Expected " +
            "Backend.Tests/Fixtures/V3AnalysisComparison (copied to the test output).");

    public static IReadOnlyList<string> DiscoverFixtureDirectories(string kind)
    {
        var fixturesRoot = TryGetFixturesRoot();
        if (fixturesRoot is null)
        {
            return [];
        }

        var root = Path.Combine(fixturesRoot, kind);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.GetDirectories(root)
            .Where(dir => File.Exists(Path.Combine(dir, PackageFileName)))
            .OrderBy(dir => Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static async Task<ComparisonRun> RunAsync(MainDbContext context, string fixtureDirectory)
    {
        var packagePath = Path.Combine(fixtureDirectory, PackageFileName);
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException(
                $"Fixture is missing {PackageFileName}.", packagePath);
        }

        var manifest = await LoadManifestAsync(fixtureDirectory);
        var election = await ImportPackageAsync(context, packagePath);
        var importedExpected = CaptureSnapshot(context, election.ElectionGuid);

        var expected = await LoadExpectedAsync(fixtureDirectory);
        string expectedSource;
        if (expected != null)
        {
            expectedSource = ExpectedFileName;
        }
        else if (HasAnalysisRows(importedExpected))
        {
            expected = importedExpected;
            expectedSource = "imported v3 analysis rows";
        }
        else
        {
            throw new InvalidOperationException(
                $"Fixture '{Path.GetFileName(fixtureDirectory)}' has no expected analysis. " +
                $"Add {ExpectedFileName}, or export the v3 package after Analyze so it includes " +
                "resultSummary, result, and resultTie rows.");
        }

        await AnalyzeAsync(context, election.ElectionGuid, manifest?.Analyzer);
        var actual = CaptureSnapshot(context, election.ElectionGuid);
        var mismatches = AnalysisComparisonDiff.Diff(expected, actual);

        return new ComparisonRun(
            Path.GetFileName(fixtureDirectory),
            manifest,
            expectedSource,
            election.ElectionGuid,
            expected,
            actual,
            mismatches);
    }

    public static AnalysisComparisonSnapshot CaptureSnapshot(MainDbContext context, Guid electionGuid)
    {
        var people = context.People
            .AsNoTracking()
            .Where(p => p.ElectionGuid == electionGuid)
            .ToDictionary(p => p.PersonGuid);

        var summaries = context.ResultSummaries
            .AsNoTracking()
            .Where(s => s.ElectionGuid == electionGuid && (s.ResultType == "C" || s.ResultType == "F"))
            .OrderBy(s => s.ResultType)
            .Select(s => new ResultSummarySnapshot
            {
                ResultType = s.ResultType,
                NumVoters = s.NumVoters,
                NumEligibleToVote = s.NumEligibleToVote,
                InPersonBallots = s.InPersonBallots,
                MailedInBallots = s.MailedInBallots,
                DroppedOffBallots = s.DroppedOffBallots,
                CalledInBallots = s.CalledInBallots,
                OnlineBallots = s.OnlineBallots,
                ImportedBallots = s.ImportedBallots,
                Custom1Ballots = s.Custom1Ballots,
                Custom2Ballots = s.Custom2Ballots,
                Custom3Ballots = s.Custom3Ballots,
                SpoiledBallots = s.SpoiledBallots,
                SpoiledVotes = s.SpoiledVotes,
                SpoiledManualBallots = s.SpoiledManualBallots,
                TotalVotes = s.TotalVotes,
                BallotsReceived = s.BallotsReceived,
                BallotsNeedingReview = s.BallotsNeedingReview,
                UseOnReports = s.UseOnReports
            })
            .ToList();

        var ties = context.ResultTies
            .AsNoTracking()
            .Where(t => t.ElectionGuid == electionGuid)
            .OrderBy(t => t.TieBreakGroup)
            .Select(t => new ResultTieSnapshot
            {
                TieBreakGroup = t.TieBreakGroup,
                TieBreakRequired = t.TieBreakRequired,
                NumToElect = t.NumToElect,
                NumInTie = t.NumInTie,
                IsResolved = t.IsResolved
            })
            .ToList();

        var counts = context.Results
            .AsNoTracking()
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .AsEnumerable()
            .Select(r =>
            {
                people.TryGetValue(r.PersonGuid, out var person);
                return new PersonCountSnapshot
                {
                    LastName = person?.LastName ?? "",
                    FirstName = person?.FirstName ?? "",
                    VoteCount = r.VoteCount ?? 0,
                    Rank = r.Rank,
                    Section = r.Section,
                    IsTied = r.IsTied,
                    TieBreakGroup = r.TieBreakGroup,
                    TieBreakRequired = r.TieBreakRequired,
                    RankInExtra = r.RankInExtra,
                    ForceShowInOther = r.ForceShowInOther
                };
            })
            .ToList();

        return new AnalysisComparisonSnapshot
        {
            Summaries = summaries,
            Ties = ties,
            Counts = counts
        };
    }

    public static bool HasAnalysisRows(AnalysisComparisonSnapshot snapshot) =>
        snapshot.Summaries.Count > 0 || snapshot.Ties.Count > 0 || snapshot.Counts.Count > 0;

    private static async Task<FixtureManifest?> LoadManifestAsync(string fixtureDirectory)
    {
        var path = Path.Combine(fixtureDirectory, ManifestFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<FixtureManifest>(stream, JsonOptions);
    }

    private static async Task<AnalysisComparisonSnapshot?> LoadExpectedAsync(string fixtureDirectory)
    {
        var path = Path.Combine(fixtureDirectory, ExpectedFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AnalysisComparisonSnapshot>(stream, JsonOptions)
               ?? throw new InvalidOperationException($"{ExpectedFileName} deserialized to null.");
    }

    private static async Task<ElectionDto> ImportPackageAsync(MainDbContext context, string packagePath)
    {
        var electionService = new Mock<IElectionService>();
        electionService
            .Setup(s => s.GetElectionByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid guid) => new ElectionDto { ElectionGuid = guid, Name = "Imported" });

        var importer = new TallyJv3ElectionImportService(
            context,
            electionService.Object,
            Mock.Of<ISignalRNotificationService>());

        await using var stream = File.OpenRead(packagePath);
        return await importer.ImportTallyJv3ElectionAsync(stream);
    }

    private static async Task AnalyzeAsync(MainDbContext context, Guid electionGuid, string? analyzerHint)
    {
        var election = await context.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
        var useSingleName = string.Equals(analyzerHint, "singlename", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(election.ElectionType, "Oth", StringComparison.Ordinal);

        ElectionAnalyzerBase analyzer = useSingleName
            ? new ElectionAnalyzerSingleName(context, NullLogger.Instance, election)
            : new ElectionAnalyzerNormal(context, NullLogger.Instance, election);

        await analyzer.AnalyzeAsync();
    }
}

public sealed record ComparisonRun(
    string FixtureName,
    FixtureManifest? Manifest,
    string ExpectedSource,
    Guid ElectionGuid,
    AnalysisComparisonSnapshot Expected,
    AnalysisComparisonSnapshot Actual,
    IReadOnlyList<string> Mismatches);
