namespace TallyJ4.DTOs.Elections;

/// <summary>
/// Data transfer object for creating a new election.
/// </summary>
public class CreateElectionDto
{
    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The type of election (e.g., "normal", "single-name").
    /// </summary>
    public string? ElectionType { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int? NumberToElect { get; set; }

    /// <summary>
    /// The name of the election convenor.
    /// </summary>
    public string? Convenor { get; set; }

    /// <summary>
    /// The mode of the election (e.g., "online", "offline").
    /// </summary>
    public string? ElectionMode { get; set; }

    /// <summary>
    /// The number of extra positions beyond the required number.
    /// </summary>
    public int? NumberExtra { get; set; }

    /// <summary>
    /// Whether to show the full election report.
    /// </summary>
    public bool? ShowFullReport { get; set; }

    /// <summary>
    /// Whether to list this election for public access.
    /// </summary>
    public bool? ListForPublic { get; set; }

    /// <summary>
    /// Whether to mark this election as a test election.
    /// </summary>
    public bool? ShowAsTest { get; set; }
}
