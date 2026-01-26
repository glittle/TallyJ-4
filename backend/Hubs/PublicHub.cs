using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

public class PublicHub : Hub
{
    private readonly ILogger<PublicHub> _logger;

    public PublicHub(ILogger<PublicHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinPublicGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Public");
        _logger.LogInformation("Client {ConnectionId} joined public group", Context.ConnectionId);
    }

    public async Task LeavePublicGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Public");
        _logger.LogInformation("Client {ConnectionId} left public group", Context.ConnectionId);
    }

    // Server-to-client methods for public election updates
    public async Task ElectionsListUpdated(string html)
    {
        await Clients.Group("Public").SendAsync("ElectionsListUpdated", html);
        _logger.LogInformation("Public elections list updated broadcast");
    }

    public async Task ElectionStatusChanged(Guid electionGuid, object electionInfo)
    {
        await Clients.Group("Public").SendAsync("ElectionStatusChanged", electionGuid, electionInfo);
        _logger.LogInformation("Election status changed broadcast for election {ElectionGuid}", electionGuid);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from PublicHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
