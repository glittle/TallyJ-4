using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

[Authorize]
public class MainHub : Hub
{
    private readonly ILogger<MainHub> _logger;

    public MainHub(ILogger<MainHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public async Task LeaveElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"Election_{electionGuid}";
}
