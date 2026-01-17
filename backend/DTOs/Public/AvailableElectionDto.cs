namespace TallyJ4.DTOs.Public;

public class AvailableElectionDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? DateOfElection { get; set; }
    public string ElectionType { get; set; } = string.Empty;
}
