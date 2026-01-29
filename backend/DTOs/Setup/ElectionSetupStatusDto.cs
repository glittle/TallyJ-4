namespace TallyJ4.DTOs.Setup;

/// <summary>
/// Data transfer object representing the current status of election setup.
/// Tracks completion of setup steps and overall progress.
/// </summary>
public class ElectionSetupStatusDto
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
    /// The current tally status of the election.
    /// </summary>
    public string TallyStatus { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether election setup step 1 is complete.
    /// </summary>
    public bool Step1Complete { get; set; }

    /// <summary>
    /// Indicates whether election setup step 2 is complete.
    /// </summary>
    public bool Step2Complete { get; set; }

    /// <summary>
    /// The overall progress percentage of election setup (0-100).
    /// </summary>
    public int ProgressPercent { get; set; }
}
