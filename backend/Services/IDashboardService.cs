using TallyJ4.DTOs.Dashboard;

namespace TallyJ4.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<List<ElectionCardDto>> GetRecentElectionsAsync(int limit = 10);
}
