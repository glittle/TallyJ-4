namespace TallyJ4.DTOs.Public;

/// <summary>
/// Data transfer object representing the status of an election for public viewing.
/// </summary>
public class ElectionStatusDto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The type of election (LSA, LSA1, LSA2, NSA, Con, Reg, Oth).
    /// </summary>
    public string ElectionType { get; set; } = string.Empty;

    /// <summary>
    /// The current tally status of the election.
    /// </summary>
    public string TallyStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the election is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The number of registered voters.
    /// </summary>
    public int RegisteredVoters { get; set; }

    /// <summary>
    /// The number of ballots that have been submitted.
    /// </summary>
    public int BallotsSubmitted { get; set; }
}
