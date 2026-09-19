namespace Backend.Helpers;

/// <summary>
/// Formats vote counts with an optional tie-break suffix.
/// Unset (<c>null</c>) is not a runoff result; explicit 0 is.
/// </summary>
public static class TieBreakDisplayHelper
{
    public static string FormatVoteCountDisplay(int voteCount, bool tieBreakRequired, int? tieBreakCount)
    {
        var votes = voteCount.ToString("N0");
        if (!tieBreakRequired || !tieBreakCount.HasValue)
        {
            return votes;
        }

        return votes + " / " + tieBreakCount.Value;
    }
}
