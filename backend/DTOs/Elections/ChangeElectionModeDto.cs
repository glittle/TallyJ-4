using Backend.Domain.Enumerations;

namespace Backend.DTOs.Elections;

/// <summary>
/// Data transfer object for updating an existing election.
/// </summary>
public class ChangeElectionModeDto
{
    /// <summary>
    /// The current tally status of the election.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// Whether to list this election for tellers access.
    /// </summary>
    public bool? ListForPublic { get; set; }

    /// <summary>
    /// Whether to mark this election as a test election.
    /// </summary>
    public bool? ShowAsTest { get; set; }

    /// <summary>
    /// The date and time when online voting opens.
    /// </summary>
    public DateTimeOffset? OnlineWhenOpen { get; set; }

    /// <summary>
    /// The date and time when online voting closes.
    /// </summary>
    public DateTimeOffset? OnlineWhenClose { get; set; }

}



