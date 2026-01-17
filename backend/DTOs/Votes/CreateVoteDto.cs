namespace TallyJ4.DTOs.Votes;

public class CreateVoteDto
{
    public Guid BallotGuid { get; set; }
    public Guid? PersonGuid { get; set; }
    public int PositionOnBallot { get; set; }
    public string StatusCode { get; set; } = "ok";
}
