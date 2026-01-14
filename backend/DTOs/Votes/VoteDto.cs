namespace TallyJ4.DTOs.Votes;

public class VoteDto
{
    public Guid BallotGuid { get; set; }
    public int PositionOnBallot { get; set; }
    public Guid? PersonGuid { get; set; }
    public string? PersonFullName { get; set; }
    public string StatusCode { get; set; } = null!;
    public string? PersonCombinedInfo { get; set; }
    public string? OnlineVoteRaw { get; set; }
}
