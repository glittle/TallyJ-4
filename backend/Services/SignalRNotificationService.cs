using Backend.DTOs.FrontDesk;
using Backend.DTOs.Results;
using Backend.DTOs.SignalR;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Services;

/// <summary>
/// Service for managing real-time notifications through SignalR hubs.
/// Provides functionality to send updates to connected clients about election changes, tally progress, and other events.
/// </summary>
public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<MainHub> _mainHubContext;
    private readonly IHubContext<AnalyzeHub> _analyzeHubContext;
    private readonly IHubContext<BallotImportHub> _ballotImportHubContext;
    private readonly IHubContext<PeopleImportHub> _peopleImportHubContext;
    private readonly IHubContext<ElectionPackageImportHub> _electionPackageImportHubContext;
    private readonly IHubContext<FrontDeskHub> _frontDeskHubContext;
    private readonly IHubContext<PublicHub> _publicHubContext;
    private readonly IHubContext<AllVotersHub> _allVotersHubContext;
    private readonly IHubContext<VoterPersonalHub> _voterPersonalHubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the SignalRNotificationService.
    /// </summary>
    /// <param name="mainHubContext">Hub context for the main SignalR hub.</param>
    /// <param name="analyzeHubContext">Hub context for the analysis SignalR hub.</param>
    /// <param name="ballotImportHubContext">Hub context for the ballot import SignalR hub.</param>
    /// <param name="peopleImportHubContext">Hub context for the people import SignalR hub.</param>
    /// <param name="electionPackageImportHubContext">Hub context for election package load progress.</param>
    /// <param name="frontDeskHubContext">Hub context for the front desk SignalR hub.</param>
    /// <param name="publicHubContext">Hub context for PublicHub (guest-teller join list).</param>
    /// <param name="allVotersHubContext">Hub context for online voters (global list refresh).</param>
    /// <param name="voterPersonalHubContext">Hub context for per-voter personal updates.</param>
    /// <param name="logger">Logger for recording notification service operations.</param>
    public SignalRNotificationService(
        IHubContext<MainHub> mainHubContext,
        IHubContext<AnalyzeHub> analyzeHubContext,
        IHubContext<BallotImportHub> ballotImportHubContext,
        IHubContext<PeopleImportHub> peopleImportHubContext,
        IHubContext<ElectionPackageImportHub> electionPackageImportHubContext,
        IHubContext<FrontDeskHub> frontDeskHubContext,
        IHubContext<PublicHub> publicHubContext,
        IHubContext<AllVotersHub> allVotersHubContext,
        IHubContext<VoterPersonalHub> voterPersonalHubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _mainHubContext = mainHubContext;
        _analyzeHubContext = analyzeHubContext;
        _ballotImportHubContext = ballotImportHubContext;
        _peopleImportHubContext = peopleImportHubContext;
        _electionPackageImportHubContext = electionPackageImportHubContext;
        _frontDeskHubContext = frontDeskHubContext;
        _publicHubContext = publicHubContext;
        _allVotersHubContext = allVotersHubContext;
        _voterPersonalHubContext = voterPersonalHubContext;
        _logger = logger;
    }

    /// <summary>
    /// Sends an election update notification to connected clients.
    /// </summary>
    /// <param name="update">The election update data to send.</param>
    public async Task SendElectionUpdateAsync(ElectionUpdateDto update)
    {
        try
        {
            // Base group: every joined client is in Main{guid} plus Known or Guest.
            // FE electionStore listens for "statusChanged" (v3 parity); not "ElectionUpdated".
            var groupName = MainHub.GetGroupName(update.ElectionGuid);
            await _mainHubContext.Clients.Group(groupName).SendAsync("statusChanged", update);
            _logger.LogInformation("Sent statusChanged notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending statusChanged notification for election {ElectionGuid}", update.ElectionGuid);
        }
    }

    /// <summary>
    /// Sends tally progress notifications to connected clients.
    /// </summary>
    /// <param name="progress">The tally progress data to send.</param>
    public async Task SendTallyProgressAsync(TallyProgressDto progress)
    {
        try
        {
            var groupName = $"Analyze{progress.ElectionGuid}";
            var eventName = progress.IsComplete ? "tallyComplete" : "tallyProgress";
            await _analyzeHubContext.Clients.Group(groupName).SendAsync(eventName, progress);
            _logger.LogInformation("Sent {EventName} notification to group {GroupName}", eventName, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending TallyProgress notification for election {ElectionGuid}", progress.ElectionGuid);
        }
    }

    /// <summary>
    /// Sends ballot import progress (camelCase event names matching SPA importStore).
    /// </summary>
    /// <param name="progress">The import progress data to send.</param>
    public async Task SendImportProgressAsync(ImportProgressDto progress)
    {
        try
        {
            var groupName = BallotImportHub.GetGroupName(progress.ElectionGuid);
            // SPA listens for camelCase; ASP.NET Core SignalR event names are case-sensitive.
            await _ballotImportHubContext.Clients.Group(groupName).SendAsync("importProgress", progress);
            _logger.LogInformation("Sent importProgress notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending importProgress notification for election {ElectionGuid}", progress.ElectionGuid);
        }
    }

    /// <summary>
    /// Sends a ballot import error (event <c>importError</c>, message + row number args).
    /// </summary>
    public async Task SendImportErrorAsync(Guid electionGuid, string errorMessage, int rowNumber)
    {
        try
        {
            var groupName = BallotImportHub.GetGroupName(electionGuid);
            await _ballotImportHubContext.Clients.Group(groupName)
                .SendAsync("importError", errorMessage, rowNumber);
            _logger.LogInformation("Sent importError notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending importError notification for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends ballot import completion (event <c>importComplete</c>).
    /// </summary>
    public async Task SendImportCompleteAsync(Guid electionGuid, object summary)
    {
        try
        {
            var groupName = BallotImportHub.GetGroupName(electionGuid);
            await _ballotImportHubContext.Clients.Group(groupName).SendAsync("importComplete", summary);
            _logger.LogInformation("Sent importComplete notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending importComplete notification for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends people import progress (event <c>importProgress</c>, thin progress payload).
    /// </summary>
    public async Task SendPeopleImportProgressAsync(Guid electionGuid, int processed, int total, string status)
    {
        try
        {
            var groupName = PeopleImportHub.GetGroupName(electionGuid);
            // SPA PeopleImportPage expects { processed, total, status } (not multi-arg).
            var progress = new { processed, total, status };
            await _peopleImportHubContext.Clients.Group(groupName).SendAsync("importProgress", progress);
            _logger.LogInformation("Sent people importProgress notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending people importProgress for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends a people import error (event <c>importError</c>).
    /// </summary>
    public async Task SendPeopleImportErrorAsync(Guid electionGuid, string errorMessage, int rowNumber = 0)
    {
        try
        {
            var groupName = PeopleImportHub.GetGroupName(electionGuid);
            await _peopleImportHubContext.Clients.Group(groupName)
                .SendAsync("importError", errorMessage, rowNumber);
            _logger.LogInformation("Sent people importError notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending people importError for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends people import completion (event <c>importComplete</c>).
    /// </summary>
    public async Task SendPeopleImportCompleteAsync(Guid electionGuid, object summary)
    {
        try
        {
            var groupName = PeopleImportHub.GetGroupName(electionGuid);
            await _peopleImportHubContext.Clients.Group(groupName).SendAsync("importComplete", summary);
            _logger.LogInformation("Sent people importComplete notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending people importComplete for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends election package load status (event <c>loaderStatus</c>, message + isTemporary args).
    /// User-scoped group <c>ElectionPackageImport{userId}</c> (v3 ImportHub login-scoped parity).
    /// </summary>
    public async Task SendElectionPackageLoaderStatusAsync(Guid userId, string message, bool isTemporary = false)
    {
        try
        {
            var groupName = ElectionPackageImportHub.GetGroupName(userId);
            // SPA listens for camelCase; two-arg payload matches v3 loaderStatus(msg, isTemp).
            await _electionPackageImportHubContext.Clients.Group(groupName)
                .SendAsync("loaderStatus", message, isTemporary);
            _logger.LogDebug(
                "Sent loaderStatus to group {GroupName}: temp={IsTemporary} {Message}",
                groupName,
                isTemporary,
                message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending loaderStatus for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Sends person update notifications to connected clients.
    /// </summary>
    /// <param name="update">The person update data to send.</param>
    public async Task SendPersonUpdateAsync(PersonUpdateDto update)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(update.ElectionGuid);
            // FE peopleStore + FrontDeskPage listen for these exact event names (not v3 updatePeople).
            var eventName = update.Action switch
            {
                "added" => "PersonAdded",
                "updated" => "PersonUpdated",
                "deleted" => "PersonDeleted",
                _ => "PersonUpdated"
            };
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync(eventName, update);
            _logger.LogInformation("Sent {EventName} notification to group {GroupName}", eventName, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PersonUpdate notification for election {ElectionGuid}", update.ElectionGuid);
        }
    }

    /// <summary>
    /// Notifies the Public hub group that the guest-joinable elections list has changed.
    /// </summary>
    public async Task SendPublicElectionListUpdateAsync(Guid? electionGuid = null, bool? guestAccessOpen = null)
    {
        try
        {
            await _publicHubContext.Clients.Group("Public")
                .SendAsync("ElectionListUpdated", electionGuid, guestAccessOpen);
            _logger.LogInformation(
                "Sent ElectionListUpdated notification to Public group (election={ElectionGuid}, open={GuestAccessOpen})",
                electionGuid,
                guestAccessOpen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PublicElectionListUpdate notification");
        }
    }

    /// <summary>
    /// Notifies connected guest tellers that they must leave the election immediately.
    /// </summary>
    public async Task CloseOutGuestTellersAsync(Guid electionGuid)
    {
        try
        {
            var guestGroup = MainHub.GetGroupName(electionGuid) + "Guest";
            await _mainHubContext.Clients.Group(guestGroup).SendAsync("electionClosed");
            _logger.LogInformation("Sent electionClosed notification to guest group {GroupName}", guestGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing out guest tellers for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends monitor update notifications to connected clients.
    /// </summary>
    /// <param name="monitorInfo">The monitor information data to send.</param>
    public async Task SendMonitorUpdateAsync(MonitorInfoDto monitorInfo)
    {
        try
        {
            var groupName = MainHub.GetGroupName(monitorInfo.ElectionGuid);
            await _mainHubContext.Clients.Group(groupName).SendAsync("MonitorUpdated", monitorInfo);
            _logger.LogInformation("Sent MonitorUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MonitorUpdated notification for election {ElectionGuid}", monitorInfo.ElectionGuid);
        }
    }

    /// <summary>
    /// Sends ballot update notifications to connected clients.
    /// </summary>
    /// <param name="update">The ballot update data to send.</param>
    public async Task SendBallotUpdateAsync(BallotUpdateDto update)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(update.ElectionGuid);
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("updateBallots", update);
            _logger.LogInformation("Sent ballot update notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BallotUpdate notification for election {ElectionGuid}", update.ElectionGuid);
        }
    }

    /// <summary>
    /// Sends person checked-in notification to front desk clients.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="voter">The checked-in voter data.</param>
    public async Task NotifyPersonCheckedInAsync(Guid electionGuid, FrontDeskVoterDto voter)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(electionGuid);
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("PersonCheckedIn", voter);
            _logger.LogInformation("Sent PersonCheckedIn notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PersonCheckedIn notification for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends voter count update notification to front desk clients.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="stats">The updated statistics.</param>
    public async Task NotifyVoterCountUpdatedAsync(Guid electionGuid, FrontDeskStatsDto stats)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(electionGuid);
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("VoterCountUpdated", stats);
            _logger.LogInformation("Sent VoterCountUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending VoterCountUpdated notification for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Sends a live vote count update for a person to all ballot entry clients.
    /// </summary>
    /// <param name="update">The updated vote count data for the person.</param>
    public async Task SendPersonVoteCountUpdateAsync(PersonVoteCountUpdateDto update)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(update.ElectionGuid);
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("PersonVoteCountUpdated", update);
            _logger.LogInformation("Sent PersonVoteCountUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PersonVoteCountUpdated notification for election {ElectionGuid}", update.ElectionGuid);
        }
    }

    /// <summary>
    /// Sends person flags updated notification to front desk clients.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="voter">The voter with updated flags.</param>
    public async Task SendPersonFlagsUpdatedAsync(Guid electionGuid, FrontDeskVoterDto voter)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(electionGuid);
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("PersonFlagsUpdated", voter);
            _logger.LogInformation("Sent PersonFlagsUpdated notification to group {GroupName} for person {PersonGuid}", groupName, voter.PersonGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PersonFlagsUpdated notification for election {ElectionGuid}", electionGuid);
        }
    }

    /// <summary>
    /// Notifies front desk / monitor clients and connected online voters that online open/close
    /// settings changed. FrontDesk: <c>updateOnlineElection</c>; AllVoters: <c>updateVoters</c>.
    /// </summary>
    public async Task SendOnlineElectionUpdateAsync(OnlineElectionUpdateDto update)
    {
        try
        {
            var frontDeskGroup = FrontDeskHub.GetGroupName(update.ElectionGuid);
            await _frontDeskHubContext.Clients.Group(frontDeskGroup).SendAsync("updateOnlineElection", update);
            _logger.LogInformation("Sent updateOnlineElection notification to group {GroupName}", frontDeskGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending updateOnlineElection for election {ElectionGuid}", update.ElectionGuid);
        }

        try
        {
            // Thin same payload on global AllVoters — clients re-fetch availableElections.
            var allVotersGroup = AllVotersHub.GetGroupName();
            await _allVotersHubContext.Clients.Group(allVotersGroup).SendAsync("updateVoters", update);
            _logger.LogInformation("Sent updateVoters notification to group {GroupName}", allVotersGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending updateVoters for election {ElectionGuid}", update.ElectionGuid);
        }
    }

    /// <inheritdoc />
    public async Task NotifyVoterPersonalUpdateAsync(
        string? email,
        string? phone,
        string? kioskCode,
        VoterPersonalUpdateDto update)
    {
        var groupKeys = DistinctNonEmpty(email, phone, kioskCode);
        if (groupKeys.Count == 0)
        {
            return;
        }

        foreach (var voterId in groupKeys)
        {
            try
            {
                var groupName = VoterPersonalHub.GetGroupName(voterId);
                await _voterPersonalHubContext.Clients.Group(groupName).SendAsync("updateVoter", update);
                _logger.LogInformation(
                    "Sent updateVoter (registration) to group for election {ElectionGuid}",
                    update.ElectionGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending updateVoter registration for election {ElectionGuid}",
                    update.ElectionGuid);
            }
        }
    }

    /// <inheritdoc />
    public async Task NotifyVoterLoginElsewhereAsync(string voterId)
    {
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return;
        }

        try
        {
            var groupName = VoterPersonalHub.GetGroupName(voterId);
            var update = new VoterPersonalUpdateDto { Login = true };
            await _voterPersonalHubContext.Clients.Group(groupName).SendAsync("updateVoter", update);
            _logger.LogInformation("Sent updateVoter (login) to personal voter group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending updateVoter login notification");
        }
    }

    /// <summary>
    /// Asks FrontDeskHub clients to re-fetch after bulk ballot import (event <c>reloadPage</c>).
    /// </summary>
    public async Task RequestFrontDeskReloadAsync(Guid electionGuid)
    {
        try
        {
            var groupName = FrontDeskHub.GetGroupName(electionGuid);
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("reloadPage");
            _logger.LogInformation("Sent reloadPage notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reloadPage for election {ElectionGuid}", electionGuid);
        }
    }

    private static List<string> DistinctNonEmpty(params string?[] values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }
}



