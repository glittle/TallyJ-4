namespace Backend.DTOs.People;

/// <summary>
/// One recent <c>SmsLog</c> row for a person phone. Does not include the phone
/// or SID (person detail already has the phone; logs must not add extra PII).
/// </summary>
public class PersonSmsLogDto
{
    /// <summary>
    /// When the message was first recorded.
    /// </summary>
    public DateTimeOffset SentDate { get; set; }

    /// <summary>
    /// Last status-callback time, when a callback has updated this row.
    /// </summary>
    public DateTimeOffset? LastDate { get; set; }

    /// <summary>
    /// Last Twilio message status (e.g. queued, sent, delivered, undelivered, failed).
    /// </summary>
    public string? LastStatus { get; set; }

    /// <summary>
    /// Twilio error code when the last status included one.
    /// </summary>
    public int? ErrorCode { get; set; }
}
