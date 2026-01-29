namespace TallyJ4.DTOs.Setup;

/// <summary>
/// Data transfer object for election setup step 2.
/// Contains configuration details for election type, mode, and number of positions to elect.
/// </summary>
public class ElectionStep2Dto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The number of positions to be elected in this election.
    /// </summary>
    public int NumberToElect { get; set; }

    /// <summary>
    /// The type of election (e.g., "normal", "single-name").
    /// </summary>
    public string ElectionType { get; set; } = string.Empty;

    /// <summary>
    /// The mode of the election (e.g., "online", "offline").
    /// </summary>
    public string ElectionMode { get; set; } = string.Empty;
}
