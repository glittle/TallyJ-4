using Backend.DTOs.FrontDesk;
using Backend.DTOs.Results;
using Backend.DTOs.SignalR;

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
    /// Sends ballot import progress to BallotImportHub clients (event <c>importProgress</c>).
    /// </summary>
    /// <param name="progress">The import progress information to broadcast.</param>
    Task SendImportProgressAsync(ImportProgressDto progress);

    /// <summary>
    /// Sends a ballot import row/fatal error to BallotImportHub clients (event <c>importError</c>).
    /// </summary>
    /// <param name="electionGuid">Election whose import session to notify.</param>
    /// <param name="errorMessage">Error description.</param>
    /// <param name="rowNumber">Source row number (0 when not row-specific).</param>
    Task SendImportErrorAsync(Guid electionGuid, string errorMessage, int rowNumber);

    /// <summary>
    /// Sends ballot import completion to BallotImportHub clients (event <c>importComplete</c>).
    /// </summary>
    /// <param name="electionGuid">Election whose import session to notify.</param>
    /// <param name="summary">Import summary payload for the SPA.</param>
    Task SendImportCompleteAsync(Guid electionGuid, object summary);

    /// <summary>
    /// Sends people import progress to PeopleImportHub clients (event <c>importProgress</c>).
    /// Payload shape: <c>{ processed, total, status }</c> (matches SPA PeopleImportProgressEvent).
    /// </summary>
    Task SendPeopleImportProgressAsync(Guid electionGuid, int processed, int total, string status);

    /// <summary>
    /// Sends a people import error to PeopleImportHub clients (event <c>importError</c>).
    /// </summary>
    Task SendPeopleImportErrorAsync(Guid electionGuid, string errorMessage, int rowNumber = 0);

    /// <summary>
    /// Sends people import completion to PeopleImportHub clients (event <c>importComplete</c>).
    /// </summary>
    Task SendPeopleImportCompleteAsync(Guid electionGuid, object summary);

    /// <summary>
    /// Sends person/voter update notifications to relevant clients.
    /// </summary>
    /// <param name="update">The person update data to broadcast.</param>
    Task SendPersonUpdateAsync(PersonUpdateDto update);

    /// <summary>
    /// Notifies the Public hub group that the guest-joinable elections list has changed.
    /// </summary>
    /// <param name="electionGuid">Optional election whose guest-access state changed.</param>
    /// <param name="guestAccessOpen">When set with <paramref name="electionGuid"/>, indicates whether guest tellers may join.</param>
    Task SendPublicElectionListUpdateAsync(Guid? electionGuid = null, bool? guestAccessOpen = null);

    /// <summary>
    /// Notifies connected guest tellers that they must leave the election immediately.
    /// </summary>
    /// <param name="electionGuid">The election where guest tellers should be closed out.</param>
    Task CloseOutGuestTellersAsync(Guid electionGuid);

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

    /// <summary>
    /// Sends a live vote count update for a person to all ballot entry clients.
    /// </summary>
    /// <param name="update">The updated vote count data for the person.</param>
    Task SendPersonVoteCountUpdateAsync(PersonVoteCountUpdateDto update);

    /// <summary>
    /// Sends person flags updated notification to front desk clients.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="voter">The voter with updated flags.</param>
    Task SendPersonFlagsUpdatedAsync(Guid electionGuid, FrontDeskVoterDto voter);

    /// <summary>
    /// Notifies front desk / monitor clients that online open/close (or related) settings changed.
    /// </summary>
    /// <param name="update">Thin online-window payload for the election.</param>
    Task SendOnlineElectionUpdateAsync(OnlineElectionUpdateDto update);

    /// <summary>
    /// Asks front desk (and other FrontDeskHub clients) to re-fetch state after bulk ballot import.
    /// </summary>
    /// <param name="electionGuid">The election whose open sessions should refresh.</param>
    Task RequestFrontDeskReloadAsync(Guid electionGuid);
}



