namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for an election available to a voter.
/// </summary>
public class AvailableElectionDto
{
    /// <summary>
    /// The election GUID.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// When online voting opens.
    /// </summary>
    public DateTime? OnlineWhenOpen { get; set; }

    /// <summary>
    /// When online voting closes.
    /// </summary>
    public DateTime? OnlineWhenClose { get; set; }

    /// <summary>
    /// The date of the election.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// Whether the voter has already voted in this election.
    /// </summary>
    public bool HasVoted { get; set; }
}
