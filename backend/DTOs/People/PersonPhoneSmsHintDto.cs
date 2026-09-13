namespace Backend.DTOs.People;

/// <summary>
/// Compact phone SMS/auth hint for people list and Front Desk.
/// Same P-row contract as <see cref="PersonPhoneOnlineVoterDto"/>:
/// <c>VoterId == Person.Phone</c> and <c>VoterIdType == "P"</c>.
/// A non-P occupant of that <c>VoterId</c> is treated as no phone row.
/// Null on the parent DTO when the person has no phone.
/// Omits last-login and recent SmsLog — those stay on person detail.
/// </summary>
public class PersonPhoneSmsHintDto
{
    /// <summary>
    /// True when that P row exists. False means never seen (no matching P row).
    /// </summary>
    public bool HasPhoneRow { get; set; }

    /// <summary>
    /// <c>OnlineVoter.WhenRegistered</c> from the matching P row.
    /// Null when there is no P row or the phone has not been used for auth yet
    /// (imported-only).
    /// </summary>
    public DateTimeOffset? WhenRegistered { get; set; }

    /// <summary>
    /// <c>OnlineVoter.SmsStatus</c> from the matching P row.
    /// null = not yet checked; "OK" = allowed; any other value is the block reason.
    /// Null when there is no matching P row.
    /// </summary>
    public string? SmsStatus { get; set; }
}
