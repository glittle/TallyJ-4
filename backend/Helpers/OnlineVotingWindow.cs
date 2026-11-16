using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// The only online-voting window check. Online voting must be on, the open
/// time must be set and already reached, and the close time must be unset or
/// still ahead. The exact close instant is closed. An unset open time is not
/// open. Sign-in, login-code request and verify, kiosk codes, ballot submit,
/// and the available-elections list all use this.
/// </summary>
public static class OnlineVotingWindow
{
    public static bool IsCurrentlyOpen(
        bool useOnlineVoting,
        DateTimeOffset? onlineWhenOpen,
        DateTimeOffset? onlineWhenClose,
        DateTimeOffset? now = null)
    {
        if (!useOnlineVoting || onlineWhenOpen == null)
        {
            return false;
        }

        var at = now ?? DateTimeOffset.UtcNow;
        if (onlineWhenOpen > at)
        {
            return false;
        }

        if (onlineWhenClose != null && onlineWhenClose <= at)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Election ids from <paramref name="elections"/> that
    /// <see cref="IsCurrentlyOpen"/> accepts at <paramref name="now"/>.
    /// The predicate is applied after the rows are loaded so every caller
    /// uses that method and not a second copy of the clock.
    /// </summary>
    public static async Task<List<Guid>> SelectOpenElectionIdsAsync(
        IQueryable<Election> elections,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var rows = await elections
            .Select(election => new
            {
                election.ElectionGuid,
                election.UseOnlineVoting,
                election.OnlineWhenOpen,
                election.OnlineWhenClose
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => IsCurrentlyOpen(
                row.UseOnlineVoting,
                row.OnlineWhenOpen,
                row.OnlineWhenClose,
                now))
            .Select(row => row.ElectionGuid)
            .ToList();
    }
}
