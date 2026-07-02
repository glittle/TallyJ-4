namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object representing a person in an online election.
/// </summary>
public class OnlinePersonDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the person.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// The geographical area of the person.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Additional information about the person.
    /// </summary>
    public string? OtherInfo { get; set; }
}



