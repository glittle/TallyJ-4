using TallyJ4.DTOs.FrontDesk;

namespace TallyJ4.Services;

public interface IFrontDeskService
{
    Task<List<FrontDeskVoterDto>> GetEligibleVotersAsync(Guid electionGuid);
    Task<FrontDeskVoterDto> CheckInVoterAsync(Guid electionGuid, CheckInVoterDto checkInDto);
    Task<RollCallDto> GetRollCallAsync(Guid electionGuid);
    Task<FrontDeskStatsDto> GetStatsAsync(Guid electionGuid);
}
