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

    // Server-to-client methods (called by business logic)
    public async Task StatusChanged(Guid electionGuid, object infoForKnown, object infoForGuest)
    {
        var knownGroup = GetGroupName(electionGuid) + "Known";
        var guestGroup = GetGroupName(electionGuid) + "Guest";

        await Clients.Group(knownGroup).SendAsync("statusChanged", infoForKnown);
        await Clients.Group(guestGroup).SendAsync("statusChanged", infoForGuest);

        _logger.LogInformation("Status changed broadcast for election {ElectionGuid}", electionGuid);
    }

    public async Task ElectionClosed(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("electionClosed");

        _logger.LogInformation("Election closed broadcast for election {ElectionGuid}", electionGuid);
    }

    public async Task CloseOutGuestTellers(Guid electionGuid)
    {
        var guestGroup = GetGroupName(electionGuid) + "Guest";
        await Clients.Group(guestGroup).SendAsync("electionClosed");

        _logger.LogInformation("Guest tellers closed out for election {ElectionGuid}", electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"Main{electionGuid}";
}
