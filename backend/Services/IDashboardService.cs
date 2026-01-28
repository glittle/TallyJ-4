using TallyJ4.DTOs.Dashboard;

namespace TallyJ4.Services;

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
    /// Retrieves all elections accessible to the current user.
    /// </summary>
    /// <returns>A list of all accessible election card data.</returns>
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
    /// Assigns a guest teller to an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tellerName">The name of the guest teller.</param>
    /// <returns>True if the guest teller was successfully assigned, false otherwise.</returns>
    Task<bool> AssignGuestTellerAsync(Guid electionGuid, string tellerName);

    /// <summary>
    /// Removes a guest teller from an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tellerName">The name of the guest teller to remove.</param>
    /// <returns>True if the guest teller was successfully removed, false otherwise.</returns>
    Task<bool> RemoveGuestTellerAsync(Guid electionGuid, string tellerName);
}
