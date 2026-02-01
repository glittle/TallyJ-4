using TallyJ4.DTOs.OnlineVoting;

namespace TallyJ4.Services;

public interface IOnlineVotingService
{
    Task<(bool Success, string? Error)> RequestVerificationCodeAsync(RequestCodeDto dto);
    
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> VerifyCodeAsync(VerifyCodeDto dto);
    
    Task<OnlineElectionInfoDto?> GetElectionInfoAsync(Guid electionGuid);
    
    Task<List<OnlineCandidateDto>> GetCandidatesAsync(Guid electionGuid);
    
    Task<(bool Success, string? Error)> SubmitBallotAsync(SubmitOnlineBallotDto dto);
    
    Task<OnlineVoteStatusDto> GetVoteStatusAsync(Guid electionGuid, string voterId);
}
