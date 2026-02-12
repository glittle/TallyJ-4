using TallyJ4.DTOs.SuperAdmin;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface ISuperAdminService
{
    Task<SuperAdminSummaryDto> GetSummaryAsync();
    Task<PaginatedResponse<SuperAdminElectionDto>> GetElectionsAsync(SuperAdminElectionFilterDto filter);
    Task<SuperAdminElectionDetailDto?> GetElectionDetailAsync(Guid electionGuid);
}
