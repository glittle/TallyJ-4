using System.Text;

namespace Backend.Tests.V3AnalysisComparison;

/// <summary>
/// Field-level diff of <see cref="AnalysisComparisonSnapshot"/> for ResultSummary,
/// ResultTies, and person vote counts (issue #168).
/// </summary>
public static class AnalysisComparisonDiff
{
    public static IReadOnlyList<string> Diff(
        AnalysisComparisonSnapshot expected,
        AnalysisComparisonSnapshot actual)
    {
        var mismatches = new List<string>();
        DiffSummaries(expected.Summaries, actual.Summaries, mismatches);
        DiffTies(expected.Ties, actual.Ties, mismatches);
        DiffCounts(expected.Counts, actual.Counts, mismatches);
        return mismatches;
    }

    public static string FormatReport(IReadOnlyList<string> mismatches)
    {
        if (mismatches.Count == 0)
        {
            return "No differences.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{mismatches.Count} analysis difference(s):");
        foreach (var line in mismatches)
        {
            sb.AppendLine("  - " + line);
        }

        return sb.ToString();
    }

    private static void DiffSummaries(
        IReadOnlyList<ResultSummarySnapshot> expected,
        IReadOnlyList<ResultSummarySnapshot> actual,
        List<string> mismatches)
    {
        var expectedByType = expected.ToDictionary(s => s.ResultType, StringComparer.Ordinal);
        var actualByType = actual.ToDictionary(s => s.ResultType, StringComparer.Ordinal);

        foreach (var type in expectedByType.Keys.Union(actualByType.Keys).OrderBy(t => t, StringComparer.Ordinal))
        {
            if (!expectedByType.TryGetValue(type, out var exp))
            {
                mismatches.Add($"ResultSummary[{type}]: present in v4, missing from expected");
                continue;
            }

            if (!actualByType.TryGetValue(type, out var act))
            {
                mismatches.Add($"ResultSummary[{type}]: present in expected, missing from v4");
                continue;
            }

            Compare($"ResultSummary[{type}].NumVoters", exp.NumVoters, act.NumVoters, mismatches);
            Compare($"ResultSummary[{type}].NumEligibleToVote", exp.NumEligibleToVote, act.NumEligibleToVote, mismatches);
            Compare($"ResultSummary[{type}].InPersonBallots", exp.InPersonBallots, act.InPersonBallots, mismatches);
            Compare($"ResultSummary[{type}].MailedInBallots", exp.MailedInBallots, act.MailedInBallots, mismatches);
            Compare($"ResultSummary[{type}].DroppedOffBallots", exp.DroppedOffBallots, act.DroppedOffBallots, mismatches);
            Compare($"ResultSummary[{type}].CalledInBallots", exp.CalledInBallots, act.CalledInBallots, mismatches);
            Compare($"ResultSummary[{type}].OnlineBallots", exp.OnlineBallots, act.OnlineBallots, mismatches);
            Compare($"ResultSummary[{type}].ImportedBallots", exp.ImportedBallots, act.ImportedBallots, mismatches);
            Compare($"ResultSummary[{type}].Custom1Ballots", exp.Custom1Ballots, act.Custom1Ballots, mismatches);
            Compare($"ResultSummary[{type}].Custom2Ballots", exp.Custom2Ballots, act.Custom2Ballots, mismatches);
            Compare($"ResultSummary[{type}].Custom3Ballots", exp.Custom3Ballots, act.Custom3Ballots, mismatches);
            Compare($"ResultSummary[{type}].SpoiledBallots", exp.SpoiledBallots, act.SpoiledBallots, mismatches);
            Compare($"ResultSummary[{type}].SpoiledVotes", exp.SpoiledVotes, act.SpoiledVotes, mismatches);
            Compare($"ResultSummary[{type}].SpoiledManualBallots", exp.SpoiledManualBallots, act.SpoiledManualBallots, mismatches);
            Compare($"ResultSummary[{type}].TotalVotes", exp.TotalVotes, act.TotalVotes, mismatches);
            Compare($"ResultSummary[{type}].BallotsReceived", exp.BallotsReceived, act.BallotsReceived, mismatches);
            Compare($"ResultSummary[{type}].BallotsNeedingReview", exp.BallotsNeedingReview, act.BallotsNeedingReview, mismatches);
            Compare($"ResultSummary[{type}].UseOnReports", exp.UseOnReports, act.UseOnReports, mismatches);
        }
    }

    private static void DiffTies(
        IReadOnlyList<ResultTieSnapshot> expected,
        IReadOnlyList<ResultTieSnapshot> actual,
        List<string> mismatches)
    {
        if (expected.Count != actual.Count)
        {
            mismatches.Add($"ResultTies count: expected {expected.Count}, v4 {actual.Count}");
        }

        var expectedByGroup = expected.ToDictionary(t => t.TieBreakGroup);
        var actualByGroup = actual.ToDictionary(t => t.TieBreakGroup);

        foreach (var group in expectedByGroup.Keys.Union(actualByGroup.Keys).OrderBy(g => g))
        {
            if (!expectedByGroup.TryGetValue(group, out var exp))
            {
                mismatches.Add($"ResultTie[{group}]: present in v4, missing from expected");
                continue;
            }

            if (!actualByGroup.TryGetValue(group, out var act))
            {
                mismatches.Add($"ResultTie[{group}]: present in expected, missing from v4");
                continue;
            }

            Compare($"ResultTie[{group}].TieBreakRequired", exp.TieBreakRequired, act.TieBreakRequired, mismatches);
            Compare($"ResultTie[{group}].NumToElect", exp.NumToElect, act.NumToElect, mismatches);
            Compare($"ResultTie[{group}].NumInTie", exp.NumInTie, act.NumInTie, mismatches);
            Compare($"ResultTie[{group}].IsResolved", exp.IsResolved, act.IsResolved, mismatches);
        }
    }

    private static void DiffCounts(
        IReadOnlyList<PersonCountSnapshot> expected,
        IReadOnlyList<PersonCountSnapshot> actual,
        List<string> mismatches)
    {
        if (expected.Count != actual.Count)
        {
            mismatches.Add($"Result counts: expected {expected.Count} people, v4 {actual.Count}");
        }

        var expectedByName = expected.ToDictionary(PersonKey, StringComparer.OrdinalIgnoreCase);
        var actualByName = actual.ToDictionary(PersonKey, StringComparer.OrdinalIgnoreCase);

        foreach (var key in expectedByName.Keys.Union(actualByName.Keys).OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!expectedByName.TryGetValue(key, out var exp))
            {
                mismatches.Add($"Count[{key}]: present in v4, missing from expected");
                continue;
            }

            if (!actualByName.TryGetValue(key, out var act))
            {
                mismatches.Add($"Count[{key}]: present in expected, missing from v4");
                continue;
            }

            Compare($"Count[{key}].VoteCount", exp.VoteCount, act.VoteCount, mismatches);
            Compare($"Count[{key}].Rank", exp.Rank, act.Rank, mismatches);
            Compare($"Count[{key}].Section", exp.Section, act.Section, mismatches);
            Compare($"Count[{key}].IsTied", exp.IsTied, act.IsTied, mismatches);
            Compare($"Count[{key}].TieBreakGroup", exp.TieBreakGroup, act.TieBreakGroup, mismatches);
            Compare($"Count[{key}].TieBreakRequired", exp.TieBreakRequired, act.TieBreakRequired, mismatches);
            Compare($"Count[{key}].RankInExtra", exp.RankInExtra, act.RankInExtra, mismatches);
            Compare($"Count[{key}].ForceShowInOther", exp.ForceShowInOther, act.ForceShowInOther, mismatches);
        }
    }

    private static string PersonKey(PersonCountSnapshot person) =>
        $"{person.LastName}, {person.FirstName}";

    private static void Compare<T>(string field, T? expected, T? actual, List<string> mismatches)
    {
        if (!Equals(expected, actual))
        {
            mismatches.Add($"{field}: expected {Format(expected)}, v4 {Format(actual)}");
        }
    }

    private static string Format<T>(T? value) => value is null ? "(null)" : value.ToString() ?? "(null)";
}
