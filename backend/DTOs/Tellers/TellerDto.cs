namespace TallyJ4.DTOs.Tellers;

/// <summary>
/// Data transfer object representing a teller in an election.
/// </summary>
public class TellerDto
{
    /// <summary>
    /// The unique row identifier for the teller.
    /// </summary>
    public int RowId { get; set; }
    
    /// <summary>
    /// The unique identifier of the election this teller belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }
    
    /// <summary>
    /// The name of the teller.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// The code of the computer the teller is using.
    /// </summary>
    public string? UsingComputerCode { get; set; }
    
    /// <summary>
    /// Indicates whether this teller is the head teller.
    /// </summary>
    public bool IsHeadTeller { get; set; }
}
