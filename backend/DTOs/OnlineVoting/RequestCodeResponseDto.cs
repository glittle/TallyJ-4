namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Response for a verification code request.
/// </summary>
public class RequestCodeResponseDto
{
    /// <summary>
    /// Localization key describing the outcome.
    /// </summary>
    public string MessageKey { get; set; } = null!;

    /// <summary>
    /// Development-only echo of the generated verification code (not sent in production).
    /// </summary>
    public string? DevVerificationCode { get; set; }

    /// <summary>
    /// Opaque, server-issued token used to join <c>/hubs/voter-code</c> for live
    /// delivery status. Present only when a send was actually attempted (after
    /// pumping / eligibility gates). Never the one-time verification code.
    /// </summary>
    public string? ChannelToken { get; set; }
}