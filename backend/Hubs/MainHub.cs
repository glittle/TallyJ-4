using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// Main SignalR hub for election-related real-time communication.
/// Handles client connections, group management, and broadcasting election status updates.
/// </summary>
[Authorize]
public class MainHub : Hub
{
    private readonly ILogger<MainHub> _logger;

    /// <summary>
    /// Initializes a new instance of the MainHub.
    /// </summary>
    /// <param name="logger">Logger for recording hub operations and client connections.</param>
    public MainHub(ILogger<MainHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds the current client to the SignalR group for the specified election.
    /// Clients in this group will receive real-time updates about the election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to join.</param>
    public async Task JoinElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Removes the current client from the SignalR group for the specified election.
    /// The client will no longer receive real-time updates about the election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to leave.</param>
    public async Task LeaveElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    // Server-to-client methods (called by business logic)
    /// <summary>
    /// Broadcasts election status changes to all clients in the election groups.
    /// Sends different information to known users and guest users for security purposes.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election whose status changed.</param>
    /// <param name="infoForKnown">Status information to send to authenticated/known users.</param>
    /// <param name="infoForGuest">Status information to send to guest users (potentially filtered for security).</param>
    public async Task StatusChanged(Guid electionGuid, object infoForKnown, object infoForGuest)
    {
        var knownGroup = GetGroupName(electionGuid) + "Known";
        var guestGroup = GetGroupName(electionGuid) + "Guest";

        await Clients.Group(knownGroup).SendAsync("statusChanged", infoForKnown);
        await Clients.Group(guestGroup).SendAsync("statusChanged", infoForGuest);

        _logger.LogInformation("Status changed broadcast for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Broadcasts that an election has been closed to all clients in the election group.
    /// This notifies all connected clients that voting is no longer possible.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election that was closed.</param>
    public async Task ElectionClosed(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("electionClosed");

        _logger.LogInformation("Election closed broadcast for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Notifies guest tellers that they should be closed out from the election.
    /// This is typically called when an election is being finalized or when guest access needs to be restricted.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where guest tellers should be closed out.</param>
    public async Task CloseOutGuestTellers(Guid electionGuid)
    {
        var guestGroup = GetGroupName(electionGuid) + "Guest";
        await Clients.Group(guestGroup).SendAsync("electionClosed");

        _logger.LogInformation("Guest tellers closed out for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Logs the disconnection event for monitoring purposes.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetGroupName(Guid electionGuid) => $"Main{electionGuid}";
}



