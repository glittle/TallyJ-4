namespace Backend.DTOs.People;

/// <summary>
/// Detailed DTO for editing a person.
/// Contains all editable fields plus registration and vote history.
/// </summary>
public class PersonDetailDto
{
    /// <summary>
    /// The unique identifier for the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The GUID of the election this person belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The person's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// The person's last name.
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// The person's full name (combination of first and last names).
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// The person's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The person's phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Whether the person can receive votes (be a candidate).
    /// </summary>
    public bool? CanReceiveVotes { get; set; }

    /// <summary>
    /// Whether the person can vote.
    /// </summary>
    public bool? CanVote { get; set; }

    /// <summary>
    /// The area or region the person belongs to.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// The person's Bahá'í ID.
    /// </summary>
    public string? BahaiId { get; set; }

    /// <summary>
    /// Other last names associated with the person.
    /// </summary>
    public string? OtherLastNames { get; set; }

    /// <summary>
    /// Other names associated with the person.
    /// </summary>
    public string? OtherNames { get; set; }

    /// <summary>
    /// Additional information about the person.
    /// </summary>
    public string? OtherInfo { get; set; }

    /// <summary>
    /// The person's age group.
    /// </summary>
    public string? AgeGroup { get; set; }

    /// <summary>
    /// The GUID of the reason why the person is ineligible (if applicable).
    /// </summary>
    public Guid? IneligibleReasonGuid { get; set; }

    /// <summary>
    /// The code of the reason why the person is ineligible (if applicable).
    /// </summary>
    public string? IneligibleReasonCode { get; set; }

    /// <summary>
    /// The time when the person was registered (checked in).
    /// </summary>
    public DateTime? RegistrationTime { get; set; }

    /// <summary>
    /// The GUID of the voting location where the person is registered.
    /// </summary>
    public Guid? VotingLocationGuid { get; set; }

    /// <summary>
    /// The voting method (e.g., 'P' for paper, 'O' for online).
    /// </summary>
    public string? VotingMethod { get; set; }

    /// <summary>
    /// Envelope number assigned to the person.
    /// </summary>
    public int? EnvNum { get; set; }

    /// <summary>
    /// Name of the first teller assigned to this person.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// Name of the second teller assigned to this person.
    /// </summary>
    public string? Teller2 { get; set; }

    /// <summary>
    /// Whether the person has submitted an online ballot.
    /// </summary>
    public bool? HasOnlineBallot { get; set; }

    /// <summary>
    /// JSON array of registration history entries.
    /// Each entry contains timestamp, action, and metadata.
    /// </summary>
    public string? RegistrationHistory { get; set; }

    /// <summary>
    /// List of votes cast by this person on various ballots.
    /// </summary>
    public List<VoteHistoryDto> VoteHistory { get; set; } = new();

    /// <summary>
    /// The number of votes this person has received.
    /// </summary>
    public int VoteCount { get; set; }
}

/// <summary>
/// Represents a single vote in a person's vote history.
/// </summary>
public class VoteHistoryDto
{
    /// <summary>
    /// The GUID of the ballot this vote belongs to.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// The position of this vote on the ballot.
    /// </summary>
    public int PositionOnBallot { get; set; }

    /// <summary>
    /// The GUID of the person who received this vote.
    /// </summary>
    public Guid? PersonGuid { get; set; }

    /// <summary>
    /// The full name of the person who received this vote.
    /// </summary>
    public string? PersonName { get; set; }

    /// <summary>
    /// Status code of the vote (e.g., 'ok', 'extra', 'invalid').
    /// </summary>
    public string StatusCode { get; set; } = null!;

    /// <summary>
    /// The GUID of the reason why this vote is invalid (if applicable).
    /// </summary>
    public Guid? InvalidReasonGuid { get; set; }

    /// <summary>
    /// The ballot number for display purposes.
    /// </summary>
    public int? BallotNumber { get; set; }

    /// <summary>
    /// The status code of the ballot.
    /// </summary>
    public string? BallotStatusCode { get; set; }
}
