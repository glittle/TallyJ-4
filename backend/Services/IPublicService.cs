using TallyJ4.DTOs.Public;

namespace TallyJ4.Services;

public interface IPublicService
{
    Task<PublicHomeDto> GetPublicHomeDataAsync();
    Task<List<AvailableElectionDto>> GetAvailableElectionsAsync();
    Task<ElectionStatusDto?> GetElectionStatusAsync(Guid electionGuid);
}
