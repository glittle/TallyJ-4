using Backend.DTOs.SignalR;

namespace Backend.Services;

/// <summary>
/// Process-wide store of pre-auth voter-code delivery channels.
/// Tokens are server-issued, hashed at rest, short-TTL, and allow one live joiner.
/// </summary>
public interface IVoterCodeDeliveryChannelService
{
    /// <summary>
    /// Mints a high-entropy token and an opaque channel id. The raw token is returned
    /// once; only its hash is stored.
    /// </summary>
    VoterCodeChannelIssue Issue();

    /// <summary>
    /// Validates <paramref name="rawToken"/> and binds this connection as the live joiner.
    /// A second concurrent joiner is rejected. After <see cref="Leave"/>, the same token
    /// may rejoin until expiry (SignalR reconnect).
    /// </summary>
    bool TryJoin(string rawToken, string connectionId, out VoterCodeChannelJoin join);

    /// <summary>
    /// Clears the live joiner for this connection so the token can be reused until TTL.
    /// </summary>
    void Leave(string connectionId);

    /// <summary>
    /// Associates a provider message/call SID with the channel so Twilio callbacks can push.
    /// </summary>
    void BindProviderSid(string channelId, string providerSid);

    /// <summary>
    /// Looks up the channel for a Twilio (or similar) SID.
    /// </summary>
    bool TryGetChannelIdByProviderSid(string providerSid, out string channelId);

    /// <summary>
    /// Appends a status for replay-on-join. Does not broadcast — callers push via SignalR.
    /// </summary>
    void RecordStatus(string channelId, VoterCodeDeliveryStatusDto status);
}

/// <summary>
/// Result of <see cref="IVoterCodeDeliveryChannelService.Issue"/>.
/// </summary>
public sealed class VoterCodeChannelIssue
{
    public required string RawToken { get; init; }
    public required string ChannelId { get; init; }
}

/// <summary>
/// Result of a successful <see cref="IVoterCodeDeliveryChannelService.TryJoin"/>.
/// </summary>
public sealed class VoterCodeChannelJoin
{
    public required string ChannelId { get; init; }
    public required IReadOnlyList<VoterCodeDeliveryStatusDto> BufferedStatuses { get; init; }
}
