namespace Backend.Helpers;

/// <summary>
/// Reasons a login code was not sent. Voters do not see these values.
/// </summary>
public static class CodeSendBlockReason
{
    /// <summary>
    /// No owner or admin on the election is approved for paid sends.
    /// </summary>
    public const string NotApproved = "not-approved";

    /// <summary>
    /// The election has used its SMS, voice, and WhatsApp allowance.
    /// </summary>
    public const string ElectionCap = "election-cap";

    /// <summary>
    /// The owner has used today's paid-send cap.
    /// </summary>
    public const string OwnerDailyCap = "owner-daily-cap";

    /// <summary>
    /// A super admin froze sends for the election or for every owner on it.
    /// </summary>
    public const string Frozen = "frozen";

    /// <summary>
    /// The voter list flagged the election. Online voting is suspended.
    /// </summary>
    public const string Flagged = "flagged";

    /// <summary>
    /// Stored outcome when the provider accepted the send.
    /// </summary>
    public const string Sent = "sent";

    /// <summary>
    /// Stored outcome when the provider did not accept the send after the cap was consumed.
    /// </summary>
    public const string SendFailed = "send-failed";

    /// <summary>
    /// Prefix stored on blocked outcomes.
    /// </summary>
    public static string Blocked(string reason) => "blocked-" + reason;
}
