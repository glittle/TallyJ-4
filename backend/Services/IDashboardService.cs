using Backend.DTOs.Dashboard;

namespace Backend.Services;

/// <summary>
/// Service interface for dashboard operations including summary data, election listings, and computer management.
/// Provides functionality for displaying election status and managing election locations.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves summary information for the dashboard display.
    /// </summary>
    /// <returns>The dashboard summary data.</returns>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();

    /// <summary>
    /// Retrieves a list of recent elections accessible to the current user.
    /// </summary>
    /// <param name="limit">The maximum number of elections to return.</param>
    /// <returns>A list of recent election card data.</returns>
    Task<List<ElectionCardDto>> GetRecentElectionsAsync(int limit = 10);

    /// <summary>
    /// At most 100 elections the user belongs to, newest date first.
    /// </summary>
    /// <returns>Election cards, capped at 100.</returns>
    Task<List<ElectionCardDto>> GetAllAccessibleElectionsAsync();

    /// <summary>
    /// Retrieves static information about a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>Static election information data.</returns>
    Task<object> GetElectionStaticInfoAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves live statistics for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>Live election statistics data.</returns>
    Task<object> GetElectionLiveStatsAsync(Guid electionGuid);

    /// <summary>
    /// Sets the location for a specific computer in the election system.
    /// </summary>
    /// <param name="computerCode">The code identifying the computer.</param>
    /// <param name="locationGuid">The unique identifier of the location.</param>
    /// <returns>True if the location was successfully set, false otherwise.</returns>
    Task<bool> SetComputerLocationAsync(string computerCode, Guid locationGuid);

    /// <summary>
    /// Inserts a Teller name when that exact name is missing. Does not open a guest session.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tellerName">The name to store.</param>
    /// <returns>True when the election exists.</returns>
    Task<bool> AssignGuestTellerAsync(Guid electionGuid, string tellerName);

    /// <summary>
    /// Deletes the Teller row with this exact name.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tellerName">The stored name to delete.</param>
    /// <returns>True if a row was deleted, false if the name is missing.</returns>
    Task<bool> RemoveGuestTellerAsync(Guid electionGuid, string tellerName);
}



