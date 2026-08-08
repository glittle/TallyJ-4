using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// SignalR hub for per-voter personal updates (v3 VoterPersonalHub parity).
/// Group is derived from the JWT <c>voterId</c> claim — clients cannot join another voter's group.
/// Server push only via <see cref="Backend.Services.ISignalRNotificationService"/> /
/// <c>IHubContext&lt;VoterPersonalHub&gt;</c>.
/// </summary>
[Authorize(Policy = "OnlineVoter")]
public class VoterPersonalHub : Hub
{
    private readonly ILogger<VoterPersonalHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoterPersonalHub"/> class.
    /// </summary>
    public VoterPersonalHub(ILogger<VoterPersonalHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Joins the caller's personal voter group (server-derived from JWT voterId).
    /// </summary>
    public async Task Join()
    {
        var voterId = GetVoterId(Context.User);
        if (string.IsNullOrWhiteSpace(voterId))
        {
            throw new HubException("Authenticated online voter id is required.");
        }

        var groupName = GetGroupName(voterId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} joined personal voter group",
            Context.ConnectionId);
    }

    /// <summary>
    /// Leaves the caller's personal voter group.
    /// </summary>
    public async Task Leave()
    {
        var voterId = GetVoterId(Context.User);
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return;
        }

        var groupName = GetGroupName(voterId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Client {ConnectionId} left personal voter group",
            Context.ConnectionId);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client {ConnectionId} disconnected from VoterPersonalHub",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Personal group for one online voter identity (email, phone, or kiosk code).
    /// </summary>
    public static string GetGroupName(string voterId) => $"Voter{voterId}";

private static string? GetVoterId(ClaimsPrincipal? user)
{
    var voterId = user?.FindFirst("voterId")?.Value;
    return string.IsNullOrWhiteSpace(voterId) ? null : voterId.Trim();
}
