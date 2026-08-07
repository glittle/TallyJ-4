using Backend.DTOs.Public;

namespace Backend.Services;

/// <summary>
/// Service interface for anonymous public operations (guest teller discovery and system info).
/// </summary>
public interface IPublicService
{
    /// <summary>
    /// Retrieves data for the public home page display.
    /// </summary>
    /// <returns>The public home page data.</returns>
    Task<PublicHomeDto> GetPublicHomeDataAsync();

    /// <summary>
    /// Retrieves elections currently open for guest teller join.
    /// </summary>
    /// <returns>A list of available election data.</returns>
    Task<List<AvailableElectionDto>> GetAvailableElectionsAsync();
}
