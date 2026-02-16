namespace Backend.DTOs.People;

/// <summary>
/// Data transfer object for creating a new person in an election.
/// </summary>
public class CreatePersonDto
{
    /// <summary>
    /// The GUID of the election this person belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The person's last name (required).
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// The person's first name.
    /// </summary>
    public string? FirstName { get; set; }

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
    /// The area or region the person belongs to.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// The person's BahÃ¡'Ã­ ID.
    /// </summary>
    public string? BahaiId { get; set; }

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
    /// The person's age group.
    /// </summary>
    public string? AgeGroup { get; set; }

    /// <summary>
    /// The GUID of the reason why the person is ineligible (if applicable).
    /// </summary>
    public Guid? IneligibleReasonGuid { get; set; }
}



