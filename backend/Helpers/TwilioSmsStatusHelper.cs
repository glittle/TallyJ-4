namespace Backend.Helpers;

/// <summary>
/// Twilio message-status callback rules for SmsLog updates and OnlineVoter.SmsStatus auto-learn.
/// </summary>
public static class TwilioSmsStatusHelper
{
    /// <summary>
    /// Terminal delivery failures. Intermediate and success statuses are ignored for auto-learn.
    /// </summary>
    public static bool IsTerminalFailure(string? messageStatus) =>
        messageStatus is not null
        && (messageStatus.Equals("undelivered", StringComparison.OrdinalIgnoreCase)
            || messageStatus.Equals("failed", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Twilio error codes that mean the destination is lastingly unusable for paid SMS.
    /// Transient or unlisted codes must not permanently block.
    /// </summary>
    public static bool IsLastingUnusableError(int errorCode) => errorCode switch
    {
        30003 => true, // Unreachable destination handset
        30004 => true, // Message blocked / filtered
        30005 => true, // Unknown destination handset
        30006 => true, // Landline or unreachable carrier
        21211 => true, // Invalid To phone number
        21614 => true, // To is not a mobile number
        _ => false
    };

    /// <summary>
    /// Reason to stamp on a phone OnlineVoter row, or null when this callback must not write SmsStatus.
    /// </summary>
    public static string? TryLearnReason(string? messageStatus, int? errorCode)
    {
        if (!IsTerminalFailure(messageStatus) || errorCode is null)
        {
            return null;
        }

        if (!IsLastingUnusableError(errorCode.Value))
        {
            return null;
        }

        return OnlineVoterSmsStatus.TwilioReason(errorCode.Value);
    }

    /// <summary>
    /// Lookup keys for <see cref="Entities.OnlineVoter.VoterId"/> from Twilio <c>To</c>.
    /// Auth still matches <c>Person.Phone == voterId</c> exactly; stored phones may omit
    /// the leading <c>+</c> that Twilio includes on E.164 <c>To</c>. Try the callback value
    /// as received (trimmed), then the +/- variant. Does not rewrite Person phones.
    /// </summary>
    public static IReadOnlyList<string> VoterIdLookupKeys(string? twilioTo)
    {
        if (string.IsNullOrWhiteSpace(twilioTo))
        {
            return [];
        }

        var trimmed = twilioTo.Trim();
        if (trimmed.Length == 0)
        {
            return [];
        }

        if (trimmed[0] == '+')
        {
            return trimmed.Length == 1 ? [trimmed] : [trimmed, trimmed[1..]];
        }

        return [trimmed, "+" + trimmed];
    }
}
