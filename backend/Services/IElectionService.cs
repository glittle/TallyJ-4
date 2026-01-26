using TallyJ4.DTOs.Elections;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface IElectionService
{
    Task<PaginatedResponse<ElectionSummaryDto>> GetElectionsAsync(int pageNumber = 1, int pageSize = 10, string? status = null);
    Task<ElectionDto?> GetElectionByGuidAsync(Guid electionGuid);
    Task<ElectionDto> CreateElectionAsync(CreateElectionDto createDto);
    Task<ElectionDto?> UpdateElectionAsync(Guid electionGuid, UpdateElectionDto updateDto);
    Task<bool> DeleteElectionAsync(Guid electionGuid);
    Task<ElectionDto?> GetElectionSummaryAsync(Guid electionGuid);
    Task<bool> UpdateElectionListingAsync(Guid electionGuid, bool isListed);
}
