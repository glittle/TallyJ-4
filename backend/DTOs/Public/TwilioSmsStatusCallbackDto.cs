namespace Backend.DTOs.Public;

/// <summary>
/// Twilio message status-callback form fields (application/x-www-form-urlencoded).
/// </summary>
public class TwilioSmsStatusCallbackDto
{
    public string? MessageSid { get; set; }

    public string? SmsSid { get; set; }

    public string? MessageStatus { get; set; }

    public string? SmsStatus { get; set; }

    public string? To { get; set; }

    public int? ErrorCode { get; set; }

    public string? Sid => string.IsNullOrWhiteSpace(MessageSid) ? SmsSid : MessageSid;

    public string? Status => string.IsNullOrWhiteSpace(MessageStatus) ? SmsStatus : MessageStatus;
}
