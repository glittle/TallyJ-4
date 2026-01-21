using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

[Authorize]
public class FrontDeskHub : Hub
{
    private readonly ILogger<FrontDeskHub> _logger;

    public FrontDeskHub(ILogger<FrontDeskHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined front desk for election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public async Task LeaveElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left front desk for election {ElectionGuid}", 
            Context.ConnectionId, electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from FrontDeskHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"FrontDesk_{electionGuid}";
}
