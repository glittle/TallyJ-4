namespace Backend.DTOs.FrontDesk;

/// <summary>
/// Data transfer object containing roll call information for front desk operations.
/// </summary>
public class RollCallDto
{
    /// <summary>
    /// List of voters at the front desk.
    /// </summary>
    public List<FrontDeskVoterDto> Voters { get; set; } = new();

    /// <summary>
    /// Statistics for the front desk.
    /// </summary>
    public FrontDeskStatsDto Stats { get; set; } = new();
}



