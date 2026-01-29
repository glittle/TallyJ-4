namespace TallyJ4.DTOs.SignalR;

/// <summary>
/// Data transfer object for person update notifications via SignalR.
/// </summary>
public class PersonUpdateDto
{
    /// <summary>
    /// The GUID of the election this person belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The GUID of the person being updated.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The action performed on the person (e.g., "created", "updated", "deleted").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The first name of the person.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the person.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// The timestamp when the person was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
