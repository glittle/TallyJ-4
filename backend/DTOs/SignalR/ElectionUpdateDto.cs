namespace Backend.DTOs.SignalR;

/// <summary>
/// Data transfer object for election update notifications via SignalR.
/// </summary>
public class ElectionUpdateDto
{
    /// <summary>
    /// The GUID of the election being updated.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The tally status of the election.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// The overall status of the election.
    /// </summary>
    public string? ElectionStatus { get; set; }

    /// <summary>
    /// The timestamp when the election was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}



