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
    /// Check-selected or notify-start list is longer than the per-request bound.
    /// </summary>
    public const string PhoneWhatsAppTooMany = "people.phoneOnlineVoter.whatsAppTooMany";

    /// <summary>
    /// Election has no <c>SmsText</c> for the head-teller WhatsApp notify body.
    /// </summary>
    public const string WhatsAppNotifyTextNotSet = "people.notifyWhatsApp.textNotSet";

    /// <summary>
    /// A notify queue is already running for this election.
    /// </summary>
    public const string WhatsAppNotifyAlreadyRunning = "people.notifyWhatsApp.alreadyRunning";

    /// <summary>
    /// Election GUID on the notify URL was not found.
    /// </summary>
    public const string WhatsAppNotifyElectionNotFound = "people.notifyWhatsApp.electionNotFound";

    /// <summary>
    /// No notify run (or matching token) for this election.
    /// </summary>
    public const string WhatsAppNotifyNotFound = "people.notifyWhatsApp.notFound";

    /// <summary>
    /// Guest teller tried to add a person while GuestTellersCanAddPeople is off (v3 GA).
    /// </summary>
    public const string GuestCannotAddPeople = "people.guestCannotAddPeople";

    /// <summary>
    /// Announcement channel was not email, SMS, or WhatsApp.
    /// </summary>
    public const string AnnouncementChannelInvalid = "people.announcement.channelInvalid";

    /// <summary>
    /// More people were requested than one announcement start accepts.
    /// </summary>
    public const string AnnouncementTooMany = "people.announcement.tooMany";

    /// <summary>
    /// An email or SMS announcement is already sending for this election and channel.
    /// </summary>
    public const string AnnouncementAlreadyRunning = "people.announcement.alreadyRunning";

    /// <summary>
    /// SMTP is not configured, so the email announcement was not started.
    /// </summary>
    public const string AnnouncementEmailNotConfigured = "people.announcement.emailNotConfigured";

    /// <summary>
    /// Twilio is not configured, so the SMS announcement was not started.
    /// </summary>
    public const string AnnouncementSmsNotConfigured = "people.announcement.smsNotConfigured";

    /// <summary>
    /// Election GUID on the announcement URL was not found.
    /// </summary>
    public const string AnnouncementElectionNotFound = "people.announcement.electionNotFound";

    /// <summary>
    /// The election has no body text for this channel.
    /// </summary>
    public const string AnnouncementTextNotSet = "people.announcement.textNotSet";

    /// <summary>
    /// No announcement run (or matching token) for this election and channel.
    /// </summary>
    public const string AnnouncementNotFound = "people.announcement.notFound";
}
