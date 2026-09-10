namespace Backend.Helpers;

/// <summary>
/// Whether the online voting window is currently open for voters.
/// Matches voter submit and available-elections: <c>UseOnlineVoting</c>
/// plus open/close vs now. A null open or close does not close the window.
/// </summary>
public static class OnlineVotingWindow
{
    public static bool IsCurrentlyOpen(
        bool useOnlineVoting,
        DateTimeOffset? onlineWhenOpen,
        DateTimeOffset? onlineWhenClose,
        DateTimeOffset? now = null)
    {
        if (!useOnlineVoting)
        {
            return false;
        }

        var at = now ?? DateTimeOffset.UtcNow;
        if (onlineWhenOpen != null && onlineWhenOpen > at)
        {
            return false;
        }

        if (onlineWhenClose != null && onlineWhenClose <= at)
        {
            return false;
        }

        return true;
    }
}
