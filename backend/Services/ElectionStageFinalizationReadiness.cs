using Backend.Context;
using Backend.Enumerations;
using static Backend.Enumerations.ElectionStageMessageKeys;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Evaluates whether an election meets the prerequisites for advancing to the Finalized stage.
/// </summary>
public sealed class FinalizationReadinessResult
{
    public bool IsReady { get; init; }

    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static FinalizationReadinessResult Ready() => new() { IsReady = true };

    public static FinalizationReadinessResult NotReady(IEnumerable<string> blockers) =>
        new() { IsReady = false, Blockers = blockers.ToList() };
}

public static class ElectionStageFinalizationReadiness
{
    public static async Task<FinalizationReadinessResult> EvaluateAsync(
        MainDbContext context,
        Guid electionGuid)
    {
        var blockers = new List<string>();

        var hasResults = await context.Results.AnyAsync(r => r.ElectionGuid == electionGuid);
        var finalSummary = await context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == electionGuid && rs.ResultType == "F");

        if (!hasResults || finalSummary == null)
        {
            blockers.Add(AnalysisNotCompleted);
        }
        else if (finalSummary.UseOnReports != true)
        {
            blockers.Add(AnalysisNotReady);
        }

        var blockingBallotCount = await ElectionBallotBlocking.CountBlockingBallotsAsync(
            context,
            electionGuid);

        if (blockingBallotCount > 0)
        {
            blockers.Add(WithParam(BallotsOutstanding, "count", blockingBallotCount));
        }
        else if (finalSummary is { UseOnReports: true, BallotsNeedingReview: > 0 })
        {
            blockers.Add(WithParam(
                BallotsNeedReview,
                "count",
                finalSummary.BallotsNeedingReview.Value));
        }

        var hasUnresolvedTies = await context.ResultTies
            .AnyAsync(rt => rt.ElectionGuid == electionGuid
                            && rt.TieBreakRequired == true
                            && rt.IsResolved != true)
            || await context.Results
                .AnyAsync(r => r.ElectionGuid == electionGuid
                               && r.TieBreakRequired == true
                               && r.IsTied == true);

        if (hasUnresolvedTies)
        {
            blockers.Add(UnresolvedTies);
        }

        return blockers.Count == 0
            ? FinalizationReadinessResult.Ready()
            : FinalizationReadinessResult.NotReady(blockers);
    }
}