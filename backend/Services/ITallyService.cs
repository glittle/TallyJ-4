using TallyJ4.DTOs.Results;

namespace TallyJ4.Services;

public interface ITallyService
{
    Task<TallyResultDto> CalculateNormalElectionAsync(Guid electionGuid);
    Task<TallyResultDto> CalculateSingleNameElectionAsync(Guid electionGuid);
    Task<TallyResultDto> GetTallyResultsAsync(Guid electionGuid);
    Task<TallyStatisticsDto> GetTallyStatisticsAsync(Guid electionGuid);
}
