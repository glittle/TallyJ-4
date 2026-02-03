namespace TallyJ4.DTOs.OnlineVoting;

public class OnlineVoteStatusDto
{
    public bool HasVoted { get; set; }
    
    public DateTime? WhenSubmitted { get; set; }
    
    public string? Message { get; set; }
}
