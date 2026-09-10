using Backend.Context;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Finalized is the election lock. There is no separate Locked flag.
/// People, ballot, vote, roll, and import mutations throw
/// <see cref="ElectionStageMessageKeys.FinalizedWriteBlocked"/>.
/// Online voter submit uses the same stage check but returns
/// <see cref="ElectionStageMessageKeys.FinalizedOnlineSubmit"/>.
/// Accept-all uses the same stage check but keeps its own message key.
/// </summary>
public static class ElectionFinalizedWriteGuard
{
    public static bool IsLocked(ElectionStage stage) => stage == ElectionStage.Finalized;

    public static void ThrowIfLocked(ElectionStage stage)
    {
        if (IsLocked(stage))
        {
            throw new InvalidOperationException(ElectionStageMessageKeys.FinalizedWriteBlocked);
        }
    }

    public static async Task<bool> IsLockedAsync(
        MainDbContext context,
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        var stage = await context.Elections
            .AsNoTracking()
            .Where(e => e.ElectionGuid == electionGuid)
            .Select(e => (ElectionStage?)e.ElectionStage)
            .FirstOrDefaultAsync(cancellationToken);

        return stage is ElectionStage s && IsLocked(s);
    }

    public static async Task ThrowIfLockedAsync(
        MainDbContext context,
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        if (await IsLockedAsync(context, electionGuid, cancellationToken))
        {
            throw new InvalidOperationException(ElectionStageMessageKeys.FinalizedWriteBlocked);
        }
    }
}
