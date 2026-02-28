namespace Backend.DTOs.Elections;

/// <summary>
/// Data transfer object for toggling teller access on an election.
/// </summary>
public class ToggleTellerAccessDto
{
    /// <summary>
    /// Whether to open (true) or close (false) teller access.
    /// </summary>
    public bool IsOpen { get; set; }
}
