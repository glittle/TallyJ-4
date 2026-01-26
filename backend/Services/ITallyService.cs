using TallyJ4.DTOs.Results;

namespace TallyJ4.Services;

public interface ITallyService
{
    Task<TallyResultDto> CalculateNormalElectionAsync(Guid electionGuid);
    Task<TallyResultDto> CalculateSingleNameElectionAsync(Guid electionGuid);
    Task<TallyResultDto> GetTallyResultsAsync(Guid electionGuid);
    Task<TallyStatisticsDto> GetTallyStatisticsAsync(Guid electionGuid);
    Task<MonitorInfoDto> GetMonitorInfoAsync(Guid electionGuid);
    Task RefreshComputerContactAsync(Guid electionGuid, string computerCode);
    Task<TieDetailsDto> GetTiesAsync(Guid electionGuid, int tieBreakGroup);
    Task<SaveTieCountsResponseDto> SaveTieCountsAsync(Guid electionGuid, SaveTieCountsRequestDto request);
    Task<ElectionReportDto> GetElectionReportAsync(Guid electionGuid);
    Task<ReportDataResponseDto> GetReportDataAsync(Guid electionGuid, string reportCode);
    Task<DetailedStatisticsDto> GetDetailedStatisticsAsync(Guid electionGuid);
    Task<PresentationDto> GetPresentationDataAsync(Guid electionGuid);
}
