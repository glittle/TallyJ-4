namespace Backend.Helpers;

/// <summary>
/// Per-person outcomes from the head-teller WhatsApp notify queue.
/// Skip/cancel codes are not a <c>WhatsAppStatus</c> write.
/// </summary>
public static class WhatsAppNotifyOutcome
{
    /// <summary>
    /// Known-OK number waiting for a provider call.
    /// </summary>
    public const string Queued = "queued";

    /// <summary>
    /// GreenAPI <c>sendMessage</c> succeeded. SmsLog written when a SID was returned.
    /// </summary>
    public const string Sent = "sent";

    /// <summary>
    /// GreenAPI was called and the send failed. SmsLog not written.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// Person has no stored phone. Provider was not called.
    /// </summary>
    public const string SkippedNoPhone = "skipped-no-phone";

    /// <summary>
    /// A non-P occupant already owns that <c>VoterId</c>. Not converted. Provider was not called.
    /// </summary>
    public const string SkippedNonP = "skipped-non-p";

    /// <summary>
    /// Person is not in the requested election. Ignored. Provider was not called.
    /// </summary>
    public const string SkippedOtherElection = "skipped-other-election";

    /// <summary>
    /// Phone P row has no <c>WhatsAppStatus</c> yet. Notify does not send to unchecked numbers.
    /// </summary>
    public const string SkippedUnchecked = "skipped-unchecked";

    /// <summary>
    /// Phone P row is <c>no-wa</c>.
    /// </summary>
    public const string SkippedNoWa = "skipped-no-wa";

    /// <summary>
    /// Phone P row is <c>check-failed</c>.
    /// </summary>
    public const string SkippedCheckFailed = "skipped-check-failed";

    /// <summary>
    /// Phone P row has some other non-OK <c>WhatsAppStatus</c>.
    /// </summary>
    public const string SkippedNotOk = "skipped-not-ok";

    /// <summary>
    /// Queue abort stopped this send. Already-sent messages stay sent.
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// Maps a stored <c>WhatsAppStatus</c> that is not OK to a skip outcome.
    /// </summary>
    public static string SkipForStatus(string? whatsAppStatus)
    {
        if (whatsAppStatus is null)
        {
            return SkippedUnchecked;
        }

        return whatsAppStatus switch
        {
            OnlineVoterWhatsAppStatus.NoWa => SkippedNoWa,
            OnlineVoterWhatsAppStatus.CheckFailed => SkippedCheckFailed,
            _ => SkippedNotOk
        };
    }

    public static bool IsSkip(string outcome) =>
        outcome is SkippedNoPhone or SkippedNonP or SkippedOtherElection
            or SkippedUnchecked or SkippedNoWa or SkippedCheckFailed or SkippedNotOk;
}
