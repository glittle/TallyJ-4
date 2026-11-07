namespace Backend.DTOs.People;

/// <summary>
/// Resend the last failed announcement for one channel in this election.
/// SMS and WhatsApp retries reserve a paid-send slot the same way as the first attempt.
/// </summary>
public class RetryFailedAnnouncementDto
{
    /// <summary>
    /// <c>email</c>, <c>sms</c>, or <c>whatsapp</c>.
    /// </summary>
    public string Channel { get; set; } = string.Empty;
}
