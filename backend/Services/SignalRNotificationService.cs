using Microsoft.AspNetCore.SignalR;
using TallyJ4.DTOs.SignalR;
using TallyJ4.DTOs.Results;
using TallyJ4.Hubs;

namespace TallyJ4.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<MainHub> _mainHubContext;
    private readonly IHubContext<AnalyzeHub> _analyzeHubContext;
    private readonly IHubContext<BallotImportHub> _ballotImportHubContext;
    private readonly IHubContext<FrontDeskHub> _frontDeskHubContext;
    private readonly IHubContext<PublicHub> _publicHubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

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

    public async Task SendElectionUpdateAsync(ElectionUpdateDto update)
    {
        try
        {
            var groupName = $"Election_{update.ElectionGuid}";
            await _mainHubContext.Clients.Group(groupName).SendAsync("ElectionUpdated", update);
            _logger.LogInformation("Sent ElectionUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ElectionUpdated notification for election {ElectionGuid}", update.ElectionGuid);
        }
    }

    public async Task SendTallyProgressAsync(TallyProgressDto progress)
    {
        try
        {
            var groupName = $"TallyProgress_{progress.ElectionGuid}";
            var eventName = progress.IsComplete ? "TallyComplete" : "TallyProgress";
            await _analyzeHubContext.Clients.Group(groupName).SendAsync(eventName, progress);
            _logger.LogInformation("Sent {EventName} notification to group {GroupName}", eventName, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending TallyProgress notification for election {ElectionGuid}", progress.ElectionGuid);
        }
    }

    public async Task SendImportProgressAsync(ImportProgressDto progress)
    {
        try
        {
            var groupName = $"Import_{progress.ElectionGuid}";
            var eventName = progress.IsComplete ? "ImportComplete" : "ImportProgress";
            await _ballotImportHubContext.Clients.Group(groupName).SendAsync(eventName, progress);
            _logger.LogInformation("Sent {EventName} notification to group {GroupName}", eventName, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ImportProgress notification for election {ElectionGuid}", progress.ElectionGuid);
        }
    }

    public async Task SendPersonUpdateAsync(PersonUpdateDto update)
    {
        try
        {
            var groupName = $"FrontDesk_{update.ElectionGuid}";
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

    public async Task SendMonitorUpdateAsync(MonitorInfoDto monitorInfo)
    {
        try
        {
            var groupName = $"Election_{monitorInfo.ElectionGuid}";
            await _mainHubContext.Clients.Group(groupName).SendAsync("MonitorUpdated", monitorInfo);
            _logger.LogInformation("Sent MonitorUpdated notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MonitorUpdated notification for election {ElectionGuid}", monitorInfo.ElectionGuid);
        }
    }
}
