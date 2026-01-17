namespace TallyJ4.DTOs.Elections;

public class ElectionSummaryDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = null!;
    public DateTime? DateOfElection { get; set; }
    public string? TallyStatus { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
}
