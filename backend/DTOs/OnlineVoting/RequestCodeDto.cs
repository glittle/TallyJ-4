namespace TallyJ4.DTOs.OnlineVoting;

public class RequestCodeDto
{
    public string VoterId { get; set; } = null!;
    
    public string VoterIdType { get; set; } = null!;
    
    public string DeliveryMethod { get; set; } = null!;
}
