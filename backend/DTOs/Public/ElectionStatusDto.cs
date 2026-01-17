namespace TallyJ4.DTOs.Public;

public class ElectionStatusDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? DateOfElection { get; set; }
    public string ElectionType { get; set; } = string.Empty;
    public string TallyStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int RegisteredVoters { get; set; }
    public int BallotsSubmitted { get; set; }
}
