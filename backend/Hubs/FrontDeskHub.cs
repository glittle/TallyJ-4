using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TallyJ4.Hubs;

/// <summary>
/// SignalR hub for front desk operations in election management.
/// Handles real-time communication for voter registration, people updates, and election status changes.
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

    // Server-to-client methods for voter registration updates
    /// <summary>
    /// Broadcasts updates about people/voters to all front desk clients.
    /// Used to notify clients when voter information has been added, updated, or changed.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where people were updated.</param>
    /// <param name="message">The update message containing information about the people changes.</param>
    public async Task UpdatePeople(Guid electionGuid, object message)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("updatePeople", message);

        _logger.LogInformation("People update broadcast for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Broadcasts a page reload command to all front desk clients for the specified election.
    /// Used to force clients to refresh their interface when critical data has changed.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where clients should reload their pages.</param>
    public async Task ReloadPage(Guid electionGuid)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("reloadPage");

        _logger.LogInformation("Page reload broadcast for election {ElectionGuid}", electionGuid);
    }

    /// <summary>
    /// Broadcasts updates about online election status to all front desk clients.
    /// Used to notify clients when online voting settings or election parameters have changed.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election where online settings were updated.</param>
    /// <param name="message">The update message containing information about the online election changes.</param>
    public async Task UpdateOnlineElection(Guid electionGuid, object message)
    {
        var groupName = GetGroupName(electionGuid);
        await Clients.Group(groupName).SendAsync("updateOnlineElection", message);

        _logger.LogInformation("Online election update broadcast for election {ElectionGuid}", electionGuid);
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

    private static string GetGroupName(Guid electionGuid) => $"FrontDesk{electionGuid}";
}
