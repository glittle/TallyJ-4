namespace Backend.DTOs.People;

/// <summary>
/// Data transfer object representing a person in an election.
/// </summary>
public class PersonDto
{
    /// <summary>
    /// The unique identifier for the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

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
    /// The person's BahÃ¡'Ã­ ID.
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
    /// Combined phonetic sound codes for search matching (Soundex-based).
    /// </summary>
    public string? CombinedSoundCodes { get; set; }

    /// <summary>
    /// The person's age group.
    /// </summary>
    public string? AgeGroup { get; set; }

    /// <summary>
    /// The GUID of the reason why the person is ineligible (if applicable).
    /// </summary>
    public Guid? IneligibleReasonGuid { get; set; }

    /// <summary>
    /// The number of votes this person has received.
    /// </summary>
    public int VoteCount { get; set; }
}



