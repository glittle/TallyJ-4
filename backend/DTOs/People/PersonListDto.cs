namespace Backend.DTOs.People;

/// <summary>
/// Lightweight DTO for displaying people in a list view.
/// Contains only the essential fields needed for list display and filtering.
/// </summary>
public class PersonListDto
{
    /// <summary>
    /// The unique identifier for the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The person's full name (combination of first and last names).
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// The area or region the person belongs to.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Whether the person can vote.
    /// </summary>
    public bool? CanVote { get; set; }

    /// <summary>
    /// Whether the person can receive votes (be a candidate).
    /// </summary>
    public bool? CanReceiveVotes { get; set; }

    /// <summary>
    /// The code of the reason why the person is ineligible (if applicable).
    /// </summary>
    public string? IneligibleReasonCode { get; set; }
}
