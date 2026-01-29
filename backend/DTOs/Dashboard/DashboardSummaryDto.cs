namespace TallyJ4.DTOs.Dashboard;

using TallyJ4.DTOs.Elections;

/// <summary>
/// Data transfer object representing a dashboard summary with election counts and recent elections.
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    /// The number of currently active elections.
    /// </summary>
    public int ActiveElectionCount { get; set; }

    /// <summary>
    /// The number of completed elections.
    /// </summary>
    public int CompletedElectionCount { get; set; }

    /// <summary>
    /// A list of recent elections for display on the dashboard.
    /// </summary>
    public List<ElectionCardDto> RecentElections { get; set; } = new();
}
