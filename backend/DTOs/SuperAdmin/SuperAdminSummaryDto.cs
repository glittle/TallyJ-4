namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Data transfer object containing summary statistics for the super admin dashboard.
/// </summary>
public class SuperAdminSummaryDto
{
    /// <summary>
    /// The total number of elections in the system.
    /// </summary>
    public int TotalElections { get; set; }

    /// <summary>
    /// GatheringBallots and ProcessingBallots. The election date is not used.
    /// </summary>
    public int OpenElections { get; set; }

    /// <summary>
    /// SettingUp. Not yet open, including when the election date is already past.
    /// </summary>
    public int UpcomingElections { get; set; }

    /// <summary>
    /// Finalized elections.
    /// </summary>
    public int CompletedElections { get; set; }
}



