using TallyJ4.DTOs.Tellers;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface ITellerService
{
    Task<PaginatedResponse<TellerDto>> GetTellersByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50);
    Task<TellerDto?> GetTellerByIdAsync(int rowId);
    Task<TellerDto> CreateTellerAsync(CreateTellerDto createDto);
    Task<TellerDto?> UpdateTellerAsync(int rowId, UpdateTellerDto updateDto);
    Task<bool> DeleteTellerAsync(int rowId);
    Task<bool> IsTellerNameUniqueAsync(Guid electionGuid, string name, int? excludeRowId = null);
}
