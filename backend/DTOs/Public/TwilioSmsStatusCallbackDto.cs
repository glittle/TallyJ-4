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

    /// <summary>
    /// Voice status-callback SID. Same update-by-SID path as MessageSid/SmsSid.
    /// </summary>
    public string? CallSid { get; set; }

    /// <summary>
    /// Voice status-callback status (initiated, ringing, completed, …).
    /// </summary>
    public string? CallStatus { get; set; }

    public string? To { get; set; }

    public int? ErrorCode { get; set; }

    /// <summary>
    /// SID used to find an existing SmsLog. MessageSid, else SmsSid, else CallSid.
    /// Never used to insert a row.
    /// </summary>
    public string? Sid => FirstNonEmpty(MessageSid, SmsSid, CallSid);

    public string? Status => FirstNonEmpty(MessageStatus, SmsStatus, CallStatus);

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
