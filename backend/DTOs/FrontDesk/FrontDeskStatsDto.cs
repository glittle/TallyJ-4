namespace TallyJ4.DTOs.FrontDesk;

/// <summary>
/// Data transfer object containing statistics for the front desk.
/// </summary>
public class FrontDeskStatsDto
{
    /// <summary>
    /// Total number of eligible voters.
    /// </summary>
    public int TotalEligible { get; set; }

    /// <summary>
    /// Number of voters who have checked in.
    /// </summary>
    public int CheckedIn { get; set; }

    /// <summary>
    /// Number of voters who have not yet checked in.
    /// </summary>
    public int NotYetCheckedIn { get; set; }

    /// <summary>
    /// Percentage of eligible voters who have checked in.
    /// </summary>
    public decimal CheckInPercentage => TotalEligible > 0
        ? Math.Round((decimal)CheckedIn / TotalEligible * 100, 1)
        : 0;
}
