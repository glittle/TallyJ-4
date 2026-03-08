using Backend.DTOs.Votes;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Service interface for managing vote operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle votes within ballots and elections.
/// </summary>
public interface IVoteService
{
    /// <summary>
    /// Retrieves all votes associated with a specific ballot.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot.</param>
    /// <returns>A list of vote data transfer objects.</returns>
    Task<List<VoteDto>> GetVotesByBallotAsync(Guid ballotGuid);

    /// <summary>
    /// Retrieves all votes associated with a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of vote data transfer objects.</returns>
    Task<List<VoteDto>> GetVotesByElectionAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves a specific vote by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the vote.</param>
    /// <returns>The vote data, or null if not found.</returns>
    Task<VoteDto?> GetVoteByIdAsync(int id);

    /// <summary>
    /// Creates a new vote with the provided data.
    /// The vote status is determined server-side based on the person's eligibility.
    /// </summary>
    /// <param name="createDto">The vote creation data.</param>
    /// <returns>The created vote data along with the ballot's current status.</returns>
    Task<VoteWithBallotStatusDto> CreateVoteAsync(CreateVoteDto createDto);

    /// <summary>
    /// Updates an existing vote with the provided data.
    /// The vote status is determined server-side based on the person's eligibility.
    /// </summary>
    /// <param name="id">The unique identifier of the vote to update.</param>
    /// <param name="updateDto">The updated vote data.</param>
    /// <returns>The updated vote data along with the ballot's current status, or null if the vote was not found.</returns>
    Task<VoteWithBallotStatusDto?> UpdateVoteAsync(int id, CreateVoteDto updateDto);

    /// <summary>
    /// Deletes a vote by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the vote to delete.</param>
    /// <returns>True if the vote was successfully deleted, false otherwise.</returns>
    Task<bool> DeleteVoteAsync(int id);
}



