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
}
