using TallyJ4.DTOs.Votes;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface IVoteService
{
    Task<List<VoteDto>> GetVotesByBallotAsync(Guid ballotGuid);
    Task<List<VoteDto>> GetVotesByElectionAsync(Guid electionGuid);
    Task<VoteDto?> GetVoteByIdAsync(int id);
    Task<VoteDto> CreateVoteAsync(CreateVoteDto createDto);
    Task<VoteDto?> UpdateVoteAsync(int id, CreateVoteDto updateDto);
    Task<bool> DeleteVoteAsync(int id);
}
