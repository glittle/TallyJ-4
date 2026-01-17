namespace TallyJ4.DTOs.Results;

public class TallyStatisticsDto
{
    public int TotalBallots { get; set; }
    public int BallotsReceived { get; set; }
    public int SpoiledBallots { get; set; }
    public int BallotsNeedingReview { get; set; }
    public int TotalVotes { get; set; }
    public int ValidVotes { get; set; }
    public int InvalidVotes { get; set; }
    public int NumVoters { get; set; }
    public int NumEligibleCandidates { get; set; }
    public int NumberToElect { get; set; }
    public int NumberExtra { get; set; }
}
