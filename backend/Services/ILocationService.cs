using Backend.DTOs.Locations;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Service interface for managing location operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle voting locations within elections.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Retrieves all locations for a specific election with pagination support.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of locations per page.</param>
    /// <returns>A paginated response containing location data.</returns>
    Task<PaginatedResponse<LocationDto>> GetLocationsByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves a specific location by its unique identifier.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location.</param>
    /// <returns>The location data, or null if not found.</returns>
    Task<LocationDto?> GetLocationByGuidAsync(Guid locationGuid);

    /// <summary>
    /// Creates a new location with the provided data.
    /// </summary>
    /// <param name="createDto">The location creation data.</param>
    /// <returns>The created location data.</returns>
    Task<LocationDto> CreateLocationAsync(CreateLocationDto createDto);

    /// <summary>
    /// Updates an existing location with the provided data.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location to update.</param>
    /// <param name="updateDto">The updated location data.</param>
    /// <returns>The updated location data, or null if the location was not found.</returns>
    Task<LocationDto?> UpdateLocationAsync(Guid locationGuid, UpdateLocationDto updateDto);

    /// <summary>
    /// Deletes a location by its unique identifier.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location to delete.</param>
    /// <returns>True if the location was successfully deleted, false otherwise.</returns>
    Task<bool> DeleteLocationAsync(Guid locationGuid);
}



