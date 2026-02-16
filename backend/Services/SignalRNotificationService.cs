using Microsoft.AspNetCore.SignalR;
using Backend.DTOs.SignalR;
using Backend.DTOs.Results;
using Backend.DTOs.FrontDesk;
using Backend.Hubs;

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
    private readonly IHubContext<FrontDeskHub> _frontDeskHubContext;
    private readonly IHubContext<PublicHub> _publicHubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the SignalRNotificationService.
    /// </summary>
    /// <param name="mainHubContext">Hub context for the main SignalR hub.</param>
    /// <param name="analyzeHubContext">Hub context for the analysis SignalR hub.</param>
    /// <param name="ballotImportHubContext">Hub context for the ballot import SignalR hub.</param>
    /// <param name="frontDeskHubContext">Hub context for the front desk SignalR hub.</param>
    /// <param name="publicHubContext">Hub context for the public SignalR hub.</param>
    /// <param name="logger">Logger for recording notification service operations.</param>
    public SignalRNotificationService(
        IHubContext<MainHub> mainHubContext,
        IHubContext<AnalyzeHub> analyzeHubContext,
        IHubContext<BallotImportHub> ballotImportHubContext,
        IHubContext<FrontDeskHub> frontDeskHubContext,
        IHubContext<PublicHub> publicHubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _mainHubContext = mainHubContext;
        _analyzeHubContext = analyzeHubContext;
        _ballotImportHubContext = ballotImportHubContext;
        _frontDeskHubContext = frontDeskHubContext;
        _publicHubContext = publicHubContext;
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
            var groupName = $"Main{update.ElectionGuid}";
            await _mainHubContext.Clients.Group(groupName).SendAsync("ElectionUpdated", update);
            _logger.LogInformation("Sent ElectionUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ElectionUpdated notification for election {ElectionGuid}", update.ElectionGuid);
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
    /// Sends import progress notifications to connected clients.
    /// </summary>
    /// <param name="progress">The import progress data to send.</param>
    public async Task SendImportProgressAsync(ImportProgressDto progress)
    {
        try
        {
            var groupName = $"BallotImport{progress.ElectionGuid}";
            var eventName = progress.IsComplete ? "ImportComplete" : "ImportProgress";
            await _ballotImportHubContext.Clients.Group(groupName).SendAsync(eventName, progress);
            _logger.LogInformation("Sent {EventName} notification to group {GroupName}", eventName, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ImportProgress notification for election {ElectionGuid}", progress.ElectionGuid);
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
            var groupName = $"FrontDesk{update.ElectionGuid}";
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
    /// Sends a notification that the public election list has been updated.
    /// </summary>
    public async Task SendPublicElectionListUpdateAsync()
    {
        try
        {
            await _publicHubContext.Clients.Group("Public").SendAsync("ElectionListUpdated");
            _logger.LogInformation("Sent ElectionListUpdated notification to Public group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PublicElectionListUpdate notification");
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
            var groupName = $"Main{monitorInfo.ElectionGuid}";
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
            var groupName = $"FrontDesk{update.ElectionGuid}";
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
            var groupName = $"FrontDesk{electionGuid}";
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
            var groupName = $"FrontDesk{electionGuid}";
            await _frontDeskHubContext.Clients.Group(groupName).SendAsync("VoterCountUpdated", stats);
            _logger.LogInformation("Sent VoterCountUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending VoterCountUpdated notification for election {ElectionGuid}", electionGuid);
        }
    }
}



