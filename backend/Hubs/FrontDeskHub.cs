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

    // Server-to-client methods for voter registration updates
    public async Task UpdatePeople(Guid electionGuid, object message)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("updatePeople", message);

        _logger.LogInformation("People update broadcast for election {ElectionGuid}", electionGuid);
    }

    public async Task ReloadPage(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("reloadPage");

        _logger.LogInformation("Page reload broadcast for election {ElectionGuid}", electionGuid);
    }

    public async Task UpdateOnlineElection(Guid electionGuid, object message)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("updateOnlineElection", message);

        _logger.LogInformation("Online election update broadcast for election {ElectionGuid}", electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from FrontDeskHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"FrontDesk{electionGuid}";
}
