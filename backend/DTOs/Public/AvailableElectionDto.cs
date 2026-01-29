namespace TallyJ4.DTOs.Public;

/// <summary>
/// Data transfer object representing an election available for public access.
/// </summary>
public class AvailableElectionDto
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
    /// The type of election (e.g., "normal", "single-name").
    /// </summary>
    public string ElectionType { get; set; } = string.Empty;
}
