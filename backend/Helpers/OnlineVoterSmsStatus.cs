namespace Backend.Helpers;

/// <summary>
/// Durable paid-channel eligibility stored on <see cref="Entities.OnlineVoter.SmsStatus"/>.
/// </summary>
public static class OnlineVoterSmsStatus
{
    /// <summary>
    /// Checked and valid for SMS / voice / WhatsApp.
    /// </summary>
    public const string Ok = "OK";

    /// <summary>
    /// Paid send is allowed when status is unset (not yet checked) or explicitly OK.
    /// </summary>
    public static bool AllowsPaidSend(string? smsStatus) =>
        smsStatus is null || smsStatus == Ok;

    /// <summary>
    /// Twilio status-callback auto-learn may write only when the current value is
    /// unset or <see cref="Ok"/>. An existing block reason is left alone.
    /// </summary>
    public static bool CanLearnFromCallback(string? smsStatus) =>
        smsStatus is null || smsStatus == Ok;

    /// <summary>
    /// Lasting-unusable Twilio error stored as <c>twilio-{code}</c> (fits varchar(50)).
    /// </summary>
    public static string TwilioReason(int errorCode) => $"twilio-{errorCode}";

    /// <summary>
    /// Manual teller set: trim, reject empty / over 50 chars, and fold any
    /// case of <see cref="Ok"/> to the exact stored value so lowercase
    /// <c>ok</c> cannot accidentally become a block reason.
    /// </summary>
    public static bool TryNormalizeManualValue(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 50)
        {
            return false;
        }

        normalized = trimmed.Equals(Ok, StringComparison.OrdinalIgnoreCase)
            ? Ok
            : trimmed;
        return true;
    }
}
