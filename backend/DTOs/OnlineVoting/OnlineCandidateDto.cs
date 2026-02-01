namespace TallyJ4.DTOs.OnlineVoting;

public class OnlineCandidateDto
{
    public Guid PersonGuid { get; set; }
    
    public string FullName { get; set; } = null!;
    
    public string? Area { get; set; }
    
    public string? OtherInfo { get; set; }
}
