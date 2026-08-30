namespace Backend.Services.Auth;

/// <summary>
/// v3 <c>PublicController.SmsStatus</c> / <c>TwilioHelper.LogSmsStatus</c> path:
/// update an existing SmsLog row and auto-learn OnlineVoter.SmsStatus from terminal failures.
/// </summary>
public interface ITwilioSmsStatusService
{
    /// <summary>
    /// Processes one Twilio status callback. Updates SmsLog when a row exists for
    /// <paramref name="smsSid"/>. For terminal failures with a selected error code,
    /// stamps <c>twilio-{code}</c> on a matching phone OnlineVoter row when allowed.
    /// Never inserts an OnlineVoter row. Never writes SmsStatus to OK.
    /// </summary>
    Task ProcessCallbackAsync(
        string? smsSid,
        string? messageStatus,
        string? to,
        int? errorCode,
        CancellationToken cancellationToken = default);
}
