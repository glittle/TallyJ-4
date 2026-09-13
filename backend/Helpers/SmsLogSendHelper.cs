using Backend.Entities;

namespace Backend.Helpers;

/// <summary>
/// Builds an <see cref="SmsLog"/> row for a successful paid send (v3 TwilioHelper insert).
/// Does not insert from a status callback — callbacks update by SID only.
/// </summary>
public static class SmsLogSendHelper
{
    /// <summary>
    /// Initial <see cref="SmsLog.LastStatus"/> when the provider response has none (v3 SMS used
    /// <c>submitted</c>).
    /// </summary>
    public const string DefaultLastStatus = "submitted";

    /// <summary>
    /// A new log row for a successful send, or null when SID or phone is missing.
    /// <see cref="SmsLog.ElectionGuid"/> and <see cref="SmsLog.PersonGuid"/> stay null —
    /// request-code SMS is pre-election (person detail looks up by phone).
    /// </summary>
    public static SmsLog? TryCreate(string? sid, string? phone, string? lastStatus)
    {
        if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var utcNow = DateTimeOffset.UtcNow;
        var status = string.IsNullOrWhiteSpace(lastStatus)
            ? DefaultLastStatus
            : Clip(lastStatus, 50);

        return new SmsLog
        {
            SmsSid = Clip(sid, 40),
            Phone = Clip(phone, 50),
            SentDate = utcNow,
            LastDate = utcNow,
            LastStatus = status
        };
    }

    private static string Clip(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
