namespace Backend.Helpers;

/// <summary>
/// Per-person outcomes from <c>CheckMultipleWhatsAppAsync</c>.
/// Persisted statuses reuse <see cref="OnlineVoterWhatsAppStatus"/> codes.
/// Skip/cancel codes are not written to <c>OnlineVoter.WhatsAppStatus</c>.
/// </summary>
public static class WhatsAppCheckOutcome
{
    /// <summary>
    /// GreenAPI reported the number has WhatsApp. Persisted as <see cref="OnlineVoterWhatsAppStatus.Ok"/>.
    /// </summary>
    public const string Ok = OnlineVoterWhatsAppStatus.Ok;

    /// <summary>
    /// GreenAPI reported the number does not have WhatsApp. Persisted as <see cref="OnlineVoterWhatsAppStatus.NoWa"/>.
    /// </summary>
    public const string NoWa = OnlineVoterWhatsAppStatus.NoWa;

    /// <summary>
    /// GreenAPI check failed. Persisted as <see cref="OnlineVoterWhatsAppStatus.CheckFailed"/>.
    /// </summary>
    public const string CheckFailed = OnlineVoterWhatsAppStatus.CheckFailed;

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
    /// Request was cancelled before this person was checked. Not persisted.
    /// </summary>
    public const string Cancelled = "cancelled";
}
