namespace Backend.DTOs.FrontDesk;

/// <summary>
/// Data transfer object for updating a person's flags.
/// </summary>
public class UpdatePersonFlagsDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// Comma-separated list of flags to set for this person.
    /// </summary>
    public string? Flags { get; set; }
}
