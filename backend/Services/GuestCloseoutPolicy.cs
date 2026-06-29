namespace Backend.Services;

/// <summary>
/// Decides when to start or cancel the guest teller close-out timer based on main teller count transitions.
/// </summary>
public enum GuestCloseoutAction
{
    None,
    StartTimer,
    CancelTimer,
}

public static class GuestCloseoutPolicy
{
    /// <summary>
    /// Returns the close-out action for a main-teller count change.
    /// Start only when the last main teller disconnects; cancel only when a main teller reconnects
    /// after all mains had left. Guest-only changes never move the deadline.
    /// </summary>
    public static GuestCloseoutAction Decide(int previousMainCount, int newMainCount)
    {
        if (previousMainCount > 0 && newMainCount == 0)
        {
            return GuestCloseoutAction.StartTimer;
        }

        if (previousMainCount == 0 && newMainCount > 0)
        {
            return GuestCloseoutAction.CancelTimer;
        }

        return GuestCloseoutAction.None;
    }

    /// <summary>
    /// Whether the guest teller join-page election list may have changed (first main connected or last main left).
    /// </summary>
    public static bool ShouldRefreshGuestJoinList(int previousMainCount, int newMainCount) =>
        Decide(previousMainCount, newMainCount) is GuestCloseoutAction.StartTimer
            or GuestCloseoutAction.CancelTimer;
}