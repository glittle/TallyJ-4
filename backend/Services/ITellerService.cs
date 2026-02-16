using Backend.DTOs.Tellers;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Service for managing tellers in an election.
/// </summary>
public interface ITellerService
{
    /// <summary>
    /// Retrieves a paginated list of tellers for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated response containing teller DTOs.</returns>
    Task<PaginatedResponse<TellerDto>> GetTellersByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves a teller by their unique row identifier.
    /// </summary>
    /// <param name="rowId">The unique row identifier of the teller.</param>
    /// <returns>The teller DTO if found, otherwise null.</returns>
    Task<TellerDto?> GetTellerByIdAsync(int rowId);

    /// <summary>
    /// Creates a new teller.
    /// </summary>
    /// <param name="createDto">The teller creation data.</param>
    /// <returns>The created teller DTO.</returns>
    Task<TellerDto> CreateTellerAsync(CreateTellerDto createDto);

    /// <summary>
    /// Updates an existing teller.
    /// </summary>
    /// <param name="rowId">The unique row identifier of the teller to update.</param>
    /// <param name="updateDto">The teller update data.</param>
    /// <returns>The updated teller DTO if found, otherwise null.</returns>
    Task<TellerDto?> UpdateTellerAsync(int rowId, UpdateTellerDto updateDto);

    /// <summary>
    /// Deletes a teller.
    /// </summary>
    /// <param name="rowId">The unique row identifier of the teller to delete.</param>
    /// <returns>True if the teller was deleted, false if not found.</returns>
    Task<bool> DeleteTellerAsync(int rowId);

    /// <summary>
    /// Checks if a teller name is unique within an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="name">The teller name to check.</param>
    /// <param name="excludeRowId">Optional row ID to exclude from the uniqueness check (for updates).</param>
    /// <returns>True if the name is unique, false otherwise.</returns>
    Task<bool> IsTellerNameUniqueAsync(Guid electionGuid, string name, int? excludeRowId = null);
}



