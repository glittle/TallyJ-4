namespace Backend.DTOs.SignalR;

/// <summary>
/// Live delivery progress for a voter login code (v3 VoterCodeHub <c>setStatus</c> / <c>final</c>).
/// Status messages only — never includes the one-time code, voter id, or extra PII.
/// </summary>
public class VoterCodeDeliveryStatusDto
{
    /// <summary>
    /// <c>sending</c>, <c>sent</c>, <c>delivered</c>, <c>failed</c>, or <c>final</c>.
    /// </summary>
    public string Status { get; set; } = VoterCodeDeliveryStatuses.Sending;

    /// <summary>
    /// i18n key for the voter UI. Never the OTP or a contact identifier.
    /// </summary>
    public string? MessageKey { get; set; }

    /// <summary>
    /// Set only when <see cref="Status"/> is <c>final</c>.
    /// </summary>
    public bool? Okay { get; set; }

    /// <summary>
    /// Allow-listed provider status (Twilio <c>queued</c> / <c>delivered</c> / …). Never the OTP.
    /// </summary>
    public string? ProviderStatus { get; set; }

    public static VoterCodeDeliveryStatusDto Sending(string? providerStatus = null) =>
        new()
        {
            Status = VoterCodeDeliveryStatuses.Sending,
            MessageKey = VoterCodeDeliveryStatuses.SendingKey,
            ProviderStatus = VoterCodeDeliveryStatuses.SanitizeProviderStatus(providerStatus)
        };

    public static VoterCodeDeliveryStatusDto Sent(string? providerStatus = null) =>
        new()
        {
            Status = VoterCodeDeliveryStatuses.Sent,
            MessageKey = VoterCodeDeliveryStatuses.SentKey,
            ProviderStatus = VoterCodeDeliveryStatuses.SanitizeProviderStatus(providerStatus)
        };

    public static VoterCodeDeliveryStatusDto Delivered(string? providerStatus = null) =>
        new()
        {
            Status = VoterCodeDeliveryStatuses.Delivered,
            MessageKey = VoterCodeDeliveryStatuses.DeliveredKey,
            ProviderStatus = VoterCodeDeliveryStatuses.SanitizeProviderStatus(providerStatus)
        };

    public static VoterCodeDeliveryStatusDto Failed(string? providerStatus = null) =>
        new()
        {
            Status = VoterCodeDeliveryStatuses.Failed,
            MessageKey = VoterCodeDeliveryStatuses.FailedKey,
            ProviderStatus = VoterCodeDeliveryStatuses.SanitizeProviderStatus(providerStatus)
        };

    public static VoterCodeDeliveryStatusDto Final(bool okay, string? providerStatus = null) =>
        new()
        {
            Status = VoterCodeDeliveryStatuses.Final,
            Okay = okay,
            MessageKey = okay
                ? VoterCodeDeliveryStatuses.FinalOkKey
                : VoterCodeDeliveryStatuses.FinalFailedKey,
            ProviderStatus = VoterCodeDeliveryStatuses.SanitizeProviderStatus(providerStatus)
        };
}
