namespace Backend.Enumerations;

/// <summary>
/// i18n phrase keys returned to the client for people-record errors.
/// Values match keys in frontend/src/locales/en/people.json.
/// </summary>
public static class PeopleMessageKeys
{
    /// <summary>
    /// A person who has already voted (voting method or accepted online ballot)
    /// cannot be given an eligibility reason that removes the right to vote.
    /// </summary>
    public const string CannotMarkCannotVoteAfterVoted = "people.cannotMarkCannotVoteAfterVoted";

    /// <summary>
    /// Person has no stored phone, so there is no P row to set SmsStatus on.
    /// </summary>
    public const string PhoneSmsStatusNoPhone = "people.phoneOnlineVoter.noPhone";

    /// <summary>
    /// No phone OnlineVoter P row could be written (non-P occupant of that VoterId).
    /// </summary>
    public const string PhoneSmsStatusNoPhoneRow = "people.phoneOnlineVoter.noPhoneRow";

    /// <summary>
    /// Manual SmsStatus was empty, whitespace, or longer than 50 after trim.
    /// </summary>
    public const string PhoneSmsStatusInvalid = "people.phoneOnlineVoter.invalidStatus";

    /// <summary>
    /// Person has no stored phone, so there is no P row to check WhatsApp on.
    /// </summary>
    public const string PhoneWhatsAppNoPhone = "people.phoneOnlineVoter.whatsAppNoPhone";

    /// <summary>
    /// No phone OnlineVoter P row could be written (non-P occupant of that VoterId).
    /// </summary>
    public const string PhoneWhatsAppNoPhoneRow = "people.phoneOnlineVoter.whatsAppNoPhoneRow";

    /// <summary>
    /// GreenAPI is not configured, so WhatsApp presence was not checked or persisted.
    /// </summary>
    public const string PhoneWhatsAppNotConfigured = "people.phoneOnlineVoter.whatsAppNotConfigured";

    /// <summary>
    /// Guest teller tried to add a person while GuestTellersCanAddPeople is off (v3 GA).
    /// </summary>
    public const string GuestCannotAddPeople = "people.guestCannotAddPeople";
}
