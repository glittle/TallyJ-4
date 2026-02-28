using Backend.DTOs.Elections;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Service interface for managing election operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle elections with pagination and status filtering.
/// </summary>
public interface IElectionService
{
    /// <summary>
    /// Retrieves elections with pagination and optional status filtering.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of elections per page.</param>
    /// <param name="status">Optional status filter for elections.</param>
    /// <returns>A paginated response containing election summary data.</returns>
    Task<PaginatedResponse<ElectionSummaryDto>> GetElectionsAsync(int pageNumber = 1, int pageSize = 10, string? status = null);

    /// <summary>
    /// Retrieves a specific election by its unique identifier.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The election data, or null if not found.</returns>
    Task<ElectionDto?> GetElectionByGuidAsync(Guid electionGuid);

    /// <summary>
    /// Creates a new election with the provided data.
    /// </summary>
    /// <param name="createDto">The election creation data.</param>
    /// <returns>The created election data.</returns>
    Task<ElectionDto> CreateElectionAsync(CreateElectionDto createDto);

    /// <summary>
    /// Updates an existing election with the provided data.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to update.</param>
    /// <param name="updateDto">The updated election data.</param>
    /// <returns>The updated election data, or null if the election was not found.</returns>
    Task<ElectionDto?> UpdateElectionAsync(Guid electionGuid, UpdateElectionDto updateDto);

    /// <summary>
    /// Deletes an election by its unique identifier.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to delete.</param>
    /// <returns>True if the election was successfully deleted, false otherwise.</returns>
    Task<bool> DeleteElectionAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves summary information for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The election summary data, or null if not found.</returns>
    Task<ElectionDto?> GetElectionSummaryAsync(Guid electionGuid);

    /// <summary>
    /// Toggles teller access for an election by setting or clearing the ListedForPublicAsOf timestamp.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="isOpen">Whether to open or close teller access.</param>
    /// <returns>The updated election data, or null if the election was not found.</returns>
    Task<ElectionDto?> ToggleTellerAccessAsync(Guid electionGuid, bool isOpen);

    /// <summary>
    /// Updates the listing status of an election (whether it appears in public listings).
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="isListed">Whether the election should be listed publicly.</param>
    /// <returns>True if the listing status was successfully updated, false otherwise.</returns>
    Task<bool> UpdateElectionListingAsync(Guid electionGuid, bool isListed);
}



