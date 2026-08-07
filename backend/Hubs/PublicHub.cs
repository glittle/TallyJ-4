using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// Public SignalR hub for anonymous clients waiting to join as guest tellers.
/// Broadcasts when the list of guest-joinable elections changes.
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
    /// Clients in this group receive updates when guest-joinable elections open or close.
    /// </summary>
    public async Task JoinPublicGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Public");
        _logger.LogInformation("Client {ConnectionId} joined public group", Context.ConnectionId);
    }

    /// <summary>
    /// Removes the current client from the public SignalR group.
    /// </summary>
    public async Task LeavePublicGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Public");
        _logger.LogInformation("Client {ConnectionId} left public group", Context.ConnectionId);
    }

    /// <summary>
    /// Called when a client disconnects from the PublicHub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from PublicHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
