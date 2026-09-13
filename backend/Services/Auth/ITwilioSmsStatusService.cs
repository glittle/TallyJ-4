namespace Backend.Services.Auth;

/// <summary>
/// v3 <c>PublicController.SmsStatus</c> / <c>TwilioHelper.LogSmsStatus</c> path:
/// update an existing SmsLog row, auto-learn OnlineVoter.SmsStatus from terminal
/// failures, and set SmsStatus to OK on delivered success when that SID exists.
/// </summary>
public interface ITwilioSmsStatusService
{
    /// <summary>
    /// Processes one Twilio status callback. Updates SmsLog when a row exists for
    /// <paramref name="smsSid"/>. For terminal failures with a selected error code,
    /// stamps <c>twilio-{code}</c> on a matching phone OnlineVoter row when allowed.
    /// For SMS <c>delivered</c> or voice <c>completed</c> when that SID already has
    /// an SmsLog row, sets the matching P row to <c>OK</c>.
    /// Never inserts an SmsLog or OnlineVoter row.
    /// </summary>
    Task ProcessCallbackAsync(
        string? smsSid,
        string? messageStatus,
        string? to,
        int? errorCode,
        CancellationToken cancellationToken = default);
}
