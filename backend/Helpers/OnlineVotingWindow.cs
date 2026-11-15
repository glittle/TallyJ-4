namespace Backend.Helpers;

/// <summary>
/// Submit calls this. <c>UseOnlineVoting</c> must be on. A null open or
/// close does not close the window, and the close instant is closed.
/// Available-elections copies that clock and also hides flagged elections
/// and elections with no approved owner or co-admin when one is rejected.
/// Login-code and kiosk sign-in
/// require open to be set. External
/// sign-in requires both ends and still treats the close instant as open.
/// Those three do not call this.
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
