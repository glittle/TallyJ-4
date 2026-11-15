using Backend.DTOs.Ballots;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Teller paper ballots for one election. See <see cref="BallotService"/>.
/// </summary>
public interface IBallotService
{
    Task<PaginatedResponse<BallotDto>> GetBallotsByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50);

    Task<BallotDto?> GetBallotByGuidAsync(Guid ballotGuid);

    Task<BallotDto> CreateBallotAsync(CreateBallotDto createDto);

    Task<BallotDto?> UpdateBallotAsync(Guid ballotGuid, UpdateBallotDto updateDto);

    Task<bool> DeleteBallotAsync(Guid ballotGuid);
}



