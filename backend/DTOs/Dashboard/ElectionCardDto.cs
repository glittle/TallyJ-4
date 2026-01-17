namespace TallyJ4.DTOs.Dashboard;

public class ElectionCardDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = null!;
    public DateTime? DateOfElection { get; set; }
    public string? TallyStatus { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
    public int VoteCount { get; set; }
    public double PercentComplete { get; set; }
}
