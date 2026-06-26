using Backend.Context;
using Backend.Enumerations;
using Backend.Services.Analyzers;
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
    private static readonly BallotStatus[] OutstandingBallotStatuses =
    [
        BallotStatus.Review,
        BallotStatus.Verify,
        BallotStatus.Raw
    ];

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
            blockers.Add("Election analysis has not been completed");
        }
        else if (finalSummary.UseOnReports != true)
        {
            blockers.Add("Election analysis is not complete or ready for finalization");
        }

        if (finalSummary is { UseOnReports: true })
        {
            if (finalSummary.BallotsNeedingReview is > 0)
            {
                blockers.Add($"{finalSummary.BallotsNeedingReview} ballot(s) still need review");
            }
            else
            {
                var ballotStatuses = await context.Ballots
                    .AsNoTracking()
                    .Where(b => b.Location.ElectionGuid == electionGuid)
                    .Select(b => b.StatusCode)
                    .ToListAsync();

                var liveOutstanding = ballotStatuses.Count(status =>
                    OutstandingBallotStatuses.Contains(status)
                    || BallotAnalyzer.BallotNeedsReview(status));

                if (liveOutstanding > 0)
                {
                    blockers.Add($"{liveOutstanding} ballot(s) have outstanding issues");
                }
            }
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
            blockers.Add("Unresolved ties must be broken before finalizing");
        }

        return blockers.Count == 0
            ? FinalizationReadinessResult.Ready()
            : FinalizationReadinessResult.NotReady(blockers);
    }
}