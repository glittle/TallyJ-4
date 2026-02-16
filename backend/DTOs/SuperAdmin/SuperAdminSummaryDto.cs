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
    /// The number of elections that are currently open for voting.
    /// </summary>
    public int OpenElections { get; set; }

    /// <summary>
    /// The number of elections that are scheduled for the future.
    /// </summary>
    public int UpcomingElections { get; set; }

    /// <summary>
    /// The number of elections that have been completed and tallied.
    /// </summary>
    public int CompletedElections { get; set; }

    /// <summary>
    /// The number of elections that have been archived.
    /// </summary>
    public int ArchivedElections { get; set; }
}



