using Backend.Context;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Ballot statuses that must be resolved before election analysis or finalization.
/// </summary>
public static class ElectionBallotBlocking
{
    public static readonly BallotStatus[] BlockingStatuses =
    [
        BallotStatus.Review,
        BallotStatus.Verify,
        BallotStatus.Raw
    ];

    public static async Task<int> CountBlockingBallotsAsync(
        MainDbContext context,
        Guid electionGuid)
    {
        return await context.Ballots
            .AsNoTracking()
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .CountAsync(b => BlockingStatuses.Contains(b.StatusCode));
    }

    public static async Task EnsureAnalysisCanProceedAsync(
        MainDbContext context,
        Guid electionGuid)
    {
        var blockingCount = await CountBlockingBallotsAsync(context, electionGuid);
        if (blockingCount > 0)
        {
            throw new InvalidOperationException(
                ElectionStageMessageKeys.WithParam(
                    ElectionStageMessageKeys.BallotsOutstanding,
                    "count",
                    blockingCount));
        }
    }
}