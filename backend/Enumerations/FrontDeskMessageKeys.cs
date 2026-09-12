namespace Backend.Enumerations;

/// <summary>
/// i18n phrase keys returned to the client for Front Desk errors.
/// Values match keys in frontend/src/locales/en/frontDesk.json.
/// </summary>
public static class FrontDeskMessageKeys
{
    /// <summary>
    /// Check-in refused because Accept-all already processed this person's
    /// online ballot. Pending Draft/Submitted does not use this key.
    /// </summary>
    public const string AlreadyAcceptedOnline = "frontDesk.errors.alreadyAcceptedOnline";

    /// <summary>
    /// Check-in refused while Accept-all has claimed the online row
    /// (<c>Processing</c>).
    /// </summary>
    public const string AlreadyProcessingOnline = "frontDesk.errors.alreadyProcessingOnline";

    /// <summary>
    /// Check-in refused for method <c>O</c>. Online ballots are voter-initiated;
    /// tellers record paper/mail/kiosk/imported methods only.
    /// </summary>
    public const string OnlineIsVoterInitiated = "frontDesk.errors.onlineIsVoterInitiated";
}
