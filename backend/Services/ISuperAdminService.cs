using Backend.DTOs.SuperAdmin;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Service interface for super admin functionality providing system-wide election management and monitoring.
/// </summary>
public interface ISuperAdminService
{
    /// <summary>
    /// Gets a summary of system-wide election statistics for the super admin dashboard.
    /// </summary>
    /// <returns>A task containing the super admin summary data.</returns>
    Task<SuperAdminSummaryDto> GetSummaryAsync();

    /// <summary>
    /// Gets a paginated list of elections based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for querying elections.</param>
    /// <returns>A task containing paginated election data.</returns>
    Task<PaginatedResponse<SuperAdminElectionDto>> GetElectionsAsync(SuperAdminElectionFilterDto filter);

    /// <summary>
    /// Gets detailed information about a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task containing detailed election information, or null if not found.</returns>
    Task<SuperAdminElectionDetailDto?> GetElectionDetailAsync(Guid electionGuid);
}



