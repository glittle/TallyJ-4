using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for front desk operations in election management.
/// Handles client join/leave for the election front-desk group.
/// Server-to-client broadcasts go through
/// <see cref="Backend.Services.ISignalRNotificationService"/> / <c>IHubContext&lt;FrontDeskHub&gt;</c>,
/// not hub instance methods.
/// </summary>
[Authorize]
public class FrontDeskHub : Hub
{
    private readonly ILogger<FrontDeskHub> _logger;

    /// <summary>
    /// Initializes a new instance of the FrontDeskHub.
    /// </summary>
    /// <param name="logger">Logger for recording hub operations and front desk activities.</param>
    public FrontDeskHub(ILogger<FrontDeskHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds the current client to the SignalR group for front desk operations of the specified election.
    /// Clients in this group will receive real-time updates about voter registration and election changes.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to join for front desk operations.</param>
    public async Task JoinElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined front desk for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Removes the current client from the SignalR group for front desk operations of the specified election.
    /// The client will no longer receive real-time updates about voter registration and election changes.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election front desk session to leave.</param>
    public async Task LeaveElection(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left front desk for election {ElectionGuid}",
            Context.ConnectionId, electionGuid);
    }

    /// <summary>
    /// Called when a client disconnects from the FrontDeskHub.
    /// Logs the disconnection event for monitoring purposes.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from FrontDeskHub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// FrontDeskHub group for an election. Server pushes person, check-in, ballot, and
    /// related events via <see cref="Backend.Services.ISignalRNotificationService"/>.
    /// </summary>
    public static string GetGroupName(Guid electionGuid) => $"FrontDesk{electionGuid}";
}



