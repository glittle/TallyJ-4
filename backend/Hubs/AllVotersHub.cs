using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for connected online voters (v3 AllVotersHub parity).
/// Global group receives thin online-settings change signals so clients re-fetch
/// <c>GET availableElections</c> (and related status APIs). Server push only via
/// <see cref="Backend.Services.ISignalRNotificationService"/> / <c>IHubContext&lt;AllVotersHub&gt;</c>.
/// <see cref="JoinElection"/> records an anonymous ballot-page session for the monitor.
/// </summary>
[Authorize(Policy = "OnlineVoter")]
public class AllVotersHub : Hub
{
    private readonly ILogger<AllVotersHub> _logger;
    private readonly IOnlineVoterPresenceService _presence;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllVotersHub"/> class.
    /// </summary>
    public AllVotersHub(ILogger<AllVotersHub> logger, IOnlineVoterPresenceService presence)
    {
        _logger = logger;
        _presence = presence;
    }

    /// <summary>
    /// Adds the authenticated online voter to the global all-voters group.
    /// </summary>
    public async Task Join()
    {
        var groupName = GetGroupName();
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} joined AllVoters group",
            Context.ConnectionId);
    }

    /// <summary>
    /// Removes the client from the global all-voters group.
    /// </summary>
    public async Task Leave()
    {
        var groupName = GetGroupName();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} left AllVoters group",
            Context.ConnectionId);
    }

    /// <summary>
    /// Counts this connection as a ballot-page session for <paramref name="electionGuid"/>.
    /// Connection id only — the monitor never receives voter identity.
    /// </summary>
    public Task JoinElection(Guid electionGuid)
    {
        if (electionGuid == Guid.Empty)
        {
            throw new HubException("Election is required.");
        }

        _presence.AddSession(electionGuid, Context.ConnectionId);
        _logger.LogInformation(
            "Client {ConnectionId} joined online voter presence for election {ElectionGuid}",
            Context.ConnectionId,
            electionGuid);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops counting this connection as a ballot-page session.
    /// </summary>
    public Task LeaveElection()
    {
        _presence.RemoveSession(Context.ConnectionId);
        _logger.LogInformation(
            "Client {ConnectionId} left online voter presence",
            Context.ConnectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _presence.RemoveSession(Context.ConnectionId);
        _logger.LogInformation(
            "Client {ConnectionId} disconnected from AllVotersHub",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Global group for all connected online voters (v3 <c>AllVoters</c>).
    /// Chosen over per-election groups so a single join covers list refresh when any
    /// election's online window changes; clients re-fetch and filter server-side eligibility.
    /// </summary>
    public static string GetGroupName() => "AllVoters";
}
