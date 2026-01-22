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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from AnalyzeHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"TallyProgress_{electionGuid}";
}
