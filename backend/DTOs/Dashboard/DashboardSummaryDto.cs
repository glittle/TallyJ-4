namespace TallyJ4.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int ActiveElectionCount { get; set; }
    public int CompletedElectionCount { get; set; }
    public List<ElectionCardDto> RecentElections { get; set; } = new();
}
