namespace TallyJ4.DTOs.OnlineVoting;

public class VerifyCodeDto
{
    public string VoterId { get; set; } = null!;
    
    public string VerifyCode { get; set; } = null!;
}
