using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

/// <summary>
/// Anonymous pre-auth hub for voter login-code delivery status (v3 VoterCodeHub).
/// Join requires a server-issued channel token from <c>POST /api/online-voting/requestCode</c>.
/// Server push only via <see cref="ISignalRNotificationService"/> / <c>IHubContext&lt;VoterCodeHub&gt;</c>.
/// Status payloads never include the one-time code.
/// </summary>
[AllowAnonymous]
public class VoterCodeHub : Hub
{
    private readonly IVoterCodeDeliveryChannelService _channels;
    private readonly ILogger<VoterCodeHub> _logger;

    public VoterCodeHub(
        IVoterCodeDeliveryChannelService channels,
        ILogger<VoterCodeHub> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    /// <summary>
    /// Joins the delivery-status group for <paramref name="channelToken"/>.
    /// Rejects unknown, expired, or already-live tokens. Replays buffered statuses
    /// so the browser that requested the code is not racing the HTTP response.
    /// </summary>
    public async Task Join(string channelToken)
    {
        if (!_channels.TryJoin(channelToken, Context.ConnectionId, out var join))
        {
            throw new HubException("Invalid or expired delivery channel.");
        }

        var groupName = GetGroupName(join.ChannelId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Context.Items[nameof(join.ChannelId)] = join.ChannelId;

        foreach (var status in join.BufferedStatuses)
        {
            await Clients.Caller.SendAsync("codeDeliveryStatus", status);
        }

        _logger.LogInformation(
            "Client {ConnectionId} joined voter-code delivery channel",
            Context.ConnectionId);
    }

    /// <summary>
    /// Leaves the delivery-status group and releases the live-joiner slot.
    /// </summary>
    public async Task Leave()
    {
        if (Context.Items.TryGetValue(nameof(VoterCodeChannelJoin.ChannelId), out var stored)
            && stored is string channelId
            && !string.IsNullOrWhiteSpace(channelId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(channelId));
        }

        _channels.Leave(Context.ConnectionId);
        _logger.LogInformation(
            "Client {ConnectionId} left voter-code delivery channel",
            Context.ConnectionId);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _channels.Leave(Context.ConnectionId);
        _logger.LogInformation(
            "Client {ConnectionId} disconnected from VoterCodeHub",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Per-request group. Channel id is a server GUID, not the raw token.
    /// </summary>
    public static string GetGroupName(string channelId) => $"VoterCode{channelId}";
}
