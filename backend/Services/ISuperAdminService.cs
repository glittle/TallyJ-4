using Backend.DTOs.SuperAdmin;
using Backend.Models;

namespace Backend.Services;

public interface ISuperAdminService
{
    Task<SuperAdminSummaryDto> GetSummaryAsync();
    Task<PaginatedResponse<SuperAdminElectionDto>> GetElectionsAsync(SuperAdminElectionFilterDto filter);
    Task<SuperAdminElectionDetailDto?> GetElectionDetailAsync(Guid electionGuid);
}



