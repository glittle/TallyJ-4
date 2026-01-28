using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

/// <summary>
/// Public SignalR hub for anonymous users to receive election information and updates.
/// Provides real-time notifications about available elections and their status changes.
/// </summary>
public class PublicHub : Hub
{
    private readonly ILogger<PublicHub> _logger;

    /// <summary>
    /// Initializes a new instance of the PublicHub.
    /// </summary>
    /// <param name="logger">Logger for recording hub operations and public client activities.</param>
    public PublicHub(ILogger<PublicHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds the current client to the public SignalR group.
    /// Public clients in this group will receive updates about available elections and their status.
    /// </summary>
    public async Task JoinPublicGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Public");
        _logger.LogInformation("Client {ConnectionId} joined public group", Context.ConnectionId);
    }

    /// <summary>
    /// Removes the current client from the public SignalR group.
    /// The client will no longer receive updates about available elections and their status.
    /// </summary>
    public async Task LeavePublicGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Public");
        _logger.LogInformation("Client {ConnectionId} left public group", Context.ConnectionId);
    }

    // Server-to-client methods for public election updates
    /// <summary>
    /// Broadcasts an updated HTML list of available elections to all public clients.
    /// Used to notify public viewers when elections are added, removed, or their status changes.
    /// </summary>
    /// <param name="html">The HTML content representing the updated list of elections.</param>
    public async Task ElectionsListUpdated(string html)
    {
        await Clients.Group("Public").SendAsync("ElectionsListUpdated", html);
        _logger.LogInformation("Public elections list updated broadcast");
    }

    /// <summary>
    /// Broadcasts a status change for a specific election to all public clients.
    /// Used to notify public viewers when an election's status changes (e.g., opened, closed, results available).
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election whose status changed.</param>
    /// <param name="electionInfo">Information about the election's new status and details.</param>
    public async Task ElectionStatusChanged(Guid electionGuid, object electionInfo)
    {
        await Clients.Group("Public").SendAsync("ElectionStatusChanged", electionGuid, electionInfo);
        _logger.LogInformation("Election status changed broadcast for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the PublicHub.
    /// Logs the disconnection event for monitoring purposes.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from PublicHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
