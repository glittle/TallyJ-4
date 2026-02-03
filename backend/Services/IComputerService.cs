using TallyJ4.DTOs.Computers;

namespace TallyJ4.Services;

public interface IComputerService
{
    Task<List<ComputerDto>> GetComputersByLocationAsync(Guid locationGuid);
    
    Task<List<ComputerDto>> GetComputersByElectionAsync(Guid electionGuid);
    
    Task<ComputerDto?> GetComputerByGuidAsync(Guid computerGuid);
    
    Task<ComputerDto?> GetComputerByCodeAsync(Guid electionGuid, string computerCode);
    
    Task<ComputerDto> RegisterComputerAsync(RegisterComputerDto dto);
    
    Task<ComputerDto?> UpdateComputerAsync(Guid computerGuid, UpdateComputerDto dto);
    
    Task<bool> DeleteComputerAsync(Guid computerGuid);
    
    Task<ComputerDto?> UpdateActivityAsync(Guid computerGuid);
    
    Task<string> GenerateComputerCodeAsync(Guid electionGuid);
}
