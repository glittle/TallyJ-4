namespace Backend.Helpers;

/// <summary>
/// Formats vote counts with an optional tie-break suffix.
/// Unset (<c>null</c>) is not a runoff result; explicit 0 is.
/// A stored count is shown after the tie is resolved and no longer required.
/// </summary>
public static class TieBreakDisplayHelper
{
    public static string FormatVoteCountDisplay(int voteCount, int? tieBreakCount)
    {
        var votes = voteCount.ToString("N0");
        if (!tieBreakCount.HasValue)
        {
            return votes;
        }

        return votes + " / " + tieBreakCount.Value;
    }
}
