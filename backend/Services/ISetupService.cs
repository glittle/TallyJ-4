using TallyJ4.DTOs.Elections;
using TallyJ4.DTOs.Setup;

namespace TallyJ4.Services;

public interface ISetupService
{
    Task<ElectionDto> CreateElectionStep1Async(ElectionStep1Dto step1Dto);
    Task<ElectionDto?> ConfigureElectionStep2Async(Guid electionGuid, ElectionStep2Dto step2Dto);
    Task<ElectionSetupStatusDto?> GetSetupStatusAsync(Guid electionGuid);
}
