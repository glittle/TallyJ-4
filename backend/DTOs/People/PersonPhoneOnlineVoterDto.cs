namespace Backend.DTOs.People;

/// <summary>
/// Fields from the global <c>OnlineVoter</c> row for a person's phone:
/// <c>VoterId == Person.Phone</c> and <c>VoterIdType == "P"</c>.
/// A non-P row occupying the same <c>VoterId</c> is not this DTO.
/// </summary>
public class PersonPhoneOnlineVoterDto
{
    /// <summary>
    /// True when that P row exists. False means never seen (no matching P row).
    /// </summary>
    public bool HasPhoneRow { get; set; }

    /// <summary>
    /// <c>OnlineVoter.WhenRegistered</c> from the matching P row.
    /// Null when there is no P row or the phone has not been used for auth yet.
    /// </summary>
    public DateTimeOffset? WhenRegistered { get; set; }

    /// <summary>
    /// <c>OnlineVoter.WhenLastLogin</c> from the matching P row.
    /// The stored value; null when unset or there is no P row.
    /// </summary>
    public DateTimeOffset? WhenLastLogin { get; set; }

    /// <summary>
    /// <c>OnlineVoter.SmsStatus</c> from the matching P row.
    /// null = not yet checked; "OK" = allowed; any other value is the block reason.
    /// Null when there is no matching P row.
    /// </summary>
    public string? SmsStatus { get; set; }
}
