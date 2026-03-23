using Backend.DTOs.Reports;

namespace Backend.Services;

public interface IReportService
{
    Task<List<ReportListItemDto>> GetAvailableReportsAsync(Guid electionGuid);
    Task<MainReportDto> GetMainReportAsync(Guid electionGuid);
    Task<VotesByNumDto> GetVotesByNumAsync(Guid electionGuid);
    Task<VotesByNameDto> GetVotesByNameAsync(Guid electionGuid);
    Task<BallotsReportDto> GetBallotsReportAsync(Guid electionGuid, string? filter = null);
    Task<SpoiledVotesReportDto> GetSpoiledVotesAsync(Guid electionGuid);
    Task<BallotAlignmentReportDto> GetBallotAlignmentAsync(Guid electionGuid);
    Task<BallotsSameReportDto> GetBallotsSameAsync(Guid electionGuid);
    Task<BallotsSummaryReportDto> GetBallotsSummaryAsync(Guid electionGuid);
    Task<AllCanReceiveReportDto> GetAllCanReceiveAsync(Guid electionGuid);
    Task<VotersReportDto> GetVotersAsync(Guid electionGuid);
    Task<FlagsReportDto> GetFlagsReportAsync(Guid electionGuid);
    Task<VotersOnlineReportDto> GetVotersOnlineAsync(Guid electionGuid);
    Task<VotersByAreaReportDto> GetVotersByAreaAsync(Guid electionGuid);
    Task<VotersByLocationReportDto> GetVotersByLocationAsync(Guid electionGuid);
    Task<VotersByLocationAreaReportDto> GetVotersByLocationAreaAsync(Guid electionGuid);
    Task<ChangedPeopleReportDto> GetChangedPeopleAsync(Guid electionGuid);
    Task<AllNonEligibleReportDto> GetAllNonEligibleAsync(Guid electionGuid);
    Task<VoterEmailsReportDto> GetVoterEmailsAsync(Guid electionGuid);
}
