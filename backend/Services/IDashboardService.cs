using TallyJ4.DTOs.Dashboard;

namespace TallyJ4.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<List<ElectionCardDto>> GetRecentElectionsAsync(int limit = 10);
    Task<List<ElectionCardDto>> GetAllAccessibleElectionsAsync();
    Task<object> GetElectionStaticInfoAsync(Guid electionGuid);
    Task<object> GetElectionLiveStatsAsync(Guid electionGuid);
    Task<bool> SetComputerLocationAsync(string computerCode, Guid locationGuid);
    Task<bool> AssignGuestTellerAsync(Guid electionGuid, string tellerName);
    Task<bool> RemoveGuestTellerAsync(Guid electionGuid, string tellerName);
}
