namespace Backend.DTOs.People;

/// <summary>
/// One selected person's WhatsApp notify outcome.
/// </summary>
public class WhatsAppNotifyPersonResultDto
{
    /// <summary>
    /// The person that was requested.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// <c>sent</c>, <c>failed</c>, <c>cancelled</c>, or a <c>skipped-*</c> reason.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Provider text or paid-send block reason for a failed attempt.
    /// No raw phone or email.
    /// </summary>
    public string? Error { get; set; }
}
