using TallyJ4.DTOs.Public;

namespace TallyJ4.Services;

/// <summary>
/// Service interface for public-facing operations that don't require authentication.
/// Provides functionality to access public election information and status.
/// </summary>
public interface IPublicService
{
    /// <summary>
    /// Retrieves data for the public home page display.
    /// </summary>
    /// <returns>The public home page data.</returns>
    Task<PublicHomeDto> GetPublicHomeDataAsync();

    /// <summary>
    /// Retrieves a list of elections that are available for public viewing.
    /// </summary>
    /// <returns>A list of available election data.</returns>
    Task<List<AvailableElectionDto>> GetAvailableElectionsAsync();

    /// <summary>
    /// Retrieves the current status of a specific election for public viewing.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The election status data, or null if the election is not found or not public.</returns>
    Task<ElectionStatusDto?> GetElectionStatusAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves election results formatted for public display.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The public display data with results, or null if the election is not found or not public.</returns>
    Task<PublicDisplayDto?> GetPublicDisplayDataAsync(Guid electionGuid);
}
