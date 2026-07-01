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
}