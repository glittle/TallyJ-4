using TallyJ4.DTOs.Ballots;
using TallyJ4.Models;

namespace TallyJ4.Services;

/// <summary>
/// Service interface for managing ballot operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle ballots within elections with pagination support.
/// </summary>
public interface IBallotService
{
    /// <summary>
    /// Retrieves ballots for a specific election with pagination support.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of ballots per page.</param>
    /// <returns>A paginated response containing ballot data.</returns>
    Task<PaginatedResponse<BallotDto>> GetBallotsByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves a specific ballot by its unique identifier.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot.</param>
    /// <returns>The ballot data, or null if not found.</returns>
    Task<BallotDto?> GetBallotByGuidAsync(Guid ballotGuid);

    /// <summary>
    /// Creates a new ballot with the provided data.
    /// </summary>
    /// <param name="createDto">The ballot creation data.</param>
    /// <returns>The created ballot data.</returns>
    Task<BallotDto> CreateBallotAsync(CreateBallotDto createDto);

    /// <summary>
    /// Updates an existing ballot with the provided data.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot to update.</param>
    /// <param name="updateDto">The updated ballot data.</param>
    /// <returns>The updated ballot data, or null if the ballot was not found.</returns>
    Task<BallotDto?> UpdateBallotAsync(Guid ballotGuid, UpdateBallotDto updateDto);

    /// <summary>
    /// Deletes a ballot by its unique identifier.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot to delete.</param>
    /// <returns>True if the ballot was successfully deleted, false otherwise.</returns>
    Task<bool> DeleteBallotAsync(Guid ballotGuid);
}
