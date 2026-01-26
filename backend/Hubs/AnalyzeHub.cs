using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

[Authorize]
public class AnalyzeHub : Hub
{
    private readonly ILogger<AnalyzeHub> _logger;

    public AnalyzeHub(ILogger<AnalyzeHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinTallySession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined tally session for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    public async Task LeaveTallySession(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left tally session for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    // Server-to-client methods for tally progress updates
    public async Task StatusUpdate(Guid electionGuid, string message, bool showProgress)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("statusUpdate", message, showProgress);

        _logger.LogInformation("Tally status update for election {ElectionGuid}: {Message}", electionGuid, message);
    }

    public async Task TallyProgress(Guid electionGuid, int processedBallots, int totalBallots, int processedVotes, int totalVotes)
    {
        var groupName = GetGroupName(electionGuid);
        var progress = new
        {
            processedBallots,
            totalBallots,
            processedVotes,
            totalVotes,
            percentage = totalBallots > 0 ? (processedBallots * 100.0 / totalBallots) : 0
        };

        await Clients.Group(groupName).SendAsync("tallyProgress", progress);

        _logger.LogInformation("Tally progress for election {ElectionGuid}: {Processed}/{Total} ballots",
            electionGuid, processedBallots, totalBallots);
    }

    public async Task TallyComplete(Guid electionGuid, object results)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("tallyComplete", results);

        _logger.LogInformation("Tally completed for election {ElectionGuid}", electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from AnalyzeHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"Analyze{electionGuid}";
}
