namespace TallyJ4.DTOs.OnlineVoting;

public class SubmitOnlineBallotDto
{
    public Guid ElectionGuid { get; set; }
    
    public string VoterId { get; set; } = null!;
    
    public List<OnlineVoteDto> Votes { get; set; } = new();
}

public class OnlineVoteDto
{
    public Guid? PersonGuid { get; set; }
    
    public string? VoteName { get; set; }
    
    public int PositionOnBallot { get; set; }
}
