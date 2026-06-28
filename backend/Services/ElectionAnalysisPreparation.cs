using Backend.Context;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Preliminary analysis preparation: refresh vote eligibility and ballot statuses
/// before determining whether a full tally can proceed (v3 parity).
/// </summary>
public static class ElectionAnalysisPreparation
{
    public static async Task PrepareAsync(
        MainDbContext context,
        Guid electionGuid,
        ILogger? logger = null)
    {
        await VoteStatusRefresher.RefreshVotesForElectionAsync(context, electionGuid, logger);
        await BallotStatusRefresher.RefreshForElectionAsync(context, electionGuid, logger);

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }
}