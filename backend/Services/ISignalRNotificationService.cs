using Backend.DTOs.SignalR;
using Backend.DTOs.Results;
using Backend.DTOs.FrontDesk;

namespace Backend.Services;

/// <summary>
/// Service interface for sending real-time notifications through SignalR hubs.
/// Provides methods to broadcast various types of updates to connected clients.
/// </summary>
public interface ISignalRNotificationService
{
    /// <summary>
    /// Sends an election update notification to relevant clients.
    /// </summary>
    /// <param name="update">The election update data to broadcast.</param>
    Task SendElectionUpdateAsync(ElectionUpdateDto update);

    /// <summary>
    /// Sends tally progress updates to monitoring clients.
    /// </summary>
    /// <param name="progress">The tally progress information to broadcast.</param>
    Task SendTallyProgressAsync(TallyProgressDto progress);

    /// <summary>
    /// Sends ballot import progress updates to monitoring clients.
    /// </summary>
    /// <param name="progress">The import progress information to broadcast.</param>
    Task SendImportProgressAsync(ImportProgressDto progress);

    /// <summary>
    /// Sends person/voter update notifications to relevant clients.
    /// </summary>
    /// <param name="update">The person update data to broadcast.</param>
    Task SendPersonUpdateAsync(PersonUpdateDto update);

    /// <summary>
    /// Sends an update notification for the public election list.
    /// </summary>
    Task SendPublicElectionListUpdateAsync();

    /// <summary>
    /// Sends monitor information updates to monitoring clients.
    /// </summary>
    /// <param name="monitorInfo">The monitor information to broadcast.</param>
    Task SendMonitorUpdateAsync(MonitorInfoDto monitorInfo);

    /// <summary>
    /// Sends person checked-in notification to front desk clients.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="voter">The checked-in voter data.</param>
    Task NotifyPersonCheckedInAsync(Guid electionGuid, FrontDeskVoterDto voter);

    /// <summary>
    /// Sends voter count update notification to front desk clients.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="stats">The updated statistics.</param>
    Task NotifyVoterCountUpdatedAsync(Guid electionGuid, FrontDeskStatsDto stats);
}



