namespace Backend.DTOs.SignalR;

/// <summary>
/// Wire status values and i18n keys for <see cref="VoterCodeDeliveryStatusDto"/>.
/// </summary>
public static class VoterCodeDeliveryStatuses
{
    public const string Sending = "sending";
    public const string Sent = "sent";
    public const string Delivered = "delivered";
    public const string Failed = "failed";
    public const string Final = "final";

    public const string SendingKey = "voting.auth.delivery.sending";
    public const string SentKey = "voting.auth.delivery.sent";
    public const string DeliveredKey = "voting.auth.delivery.delivered";
    public const string FailedKey = "voting.auth.delivery.failed";
    public const string FinalOkKey = "voting.auth.delivery.finalOk";
    public const string FinalFailedKey = "voting.auth.delivery.finalFailed";

    /// <summary>
    /// Maps a Twilio (or voice) callback status onto the voter-facing progress values
    /// used before <c>final</c>. Unknown values stay <see cref="Sending"/>.
    /// </summary>
    public static string FromProviderStatus(string? providerStatus)
    {
        var known = SanitizeProviderStatus(providerStatus);
        return known switch
        {
            "delivered" or "completed" => Delivered,
            "undelivered" or "failed" or "canceled" or "cancelled"
                or "busy" or "no-answer" => Failed,
            "sent" => Sent,
            _ => Sending
        };
    }

    public static bool IsTerminalProgress(string status) =>
        status is Delivered or Failed;

    /// <summary>
    /// Allow-list so unexpected provider strings never ride the hub wire.
    /// </summary>
    public static string? SanitizeProviderStatus(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
        {
            return null;
        }

        return providerStatus.Trim().ToLowerInvariant() switch
        {
            "queued" => "queued",
            "accepted" => "accepted",
            "sending" => "sending",
            "sent" => "sent",
            "delivered" => "delivered",
            "undelivered" => "undelivered",
            "failed" => "failed",
            "canceled" => "canceled",
            "cancelled" => "cancelled",
            "completed" => "completed",
            "busy" => "busy",
            "no-answer" => "no-answer",
            "ringing" => "ringing",
            "in-progress" => "in-progress",
            _ => "other"
        };
    }
}
