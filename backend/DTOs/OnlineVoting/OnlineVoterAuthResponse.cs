namespace TallyJ4.DTOs.OnlineVoting;

public class OnlineVoterAuthResponse
{
    public string Token { get; set; } = null!;
    
    public string VoterId { get; set; } = null!;
    
    public string VoterIdType { get; set; } = null!;
    
    public DateTime ExpiresAt { get; set; }
}
