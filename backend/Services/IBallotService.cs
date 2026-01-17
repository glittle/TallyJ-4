using TallyJ4.DTOs.Ballots;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface IBallotService
{
    Task<PaginatedResponse<BallotDto>> GetBallotsByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50);
    Task<BallotDto?> GetBallotByGuidAsync(Guid ballotGuid);
    Task<BallotDto> CreateBallotAsync(CreateBallotDto createDto);
    Task<BallotDto?> UpdateBallotAsync(Guid ballotGuid, UpdateBallotDto updateDto);
    Task<bool> DeleteBallotAsync(Guid ballotGuid);
}
