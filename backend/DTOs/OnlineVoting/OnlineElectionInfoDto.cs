namespace TallyJ4.DTOs.OnlineVoting;

public class OnlineElectionInfoDto
{
    public Guid ElectionGuid { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string? Convenor { get; set; }
    
    public DateTime? DateOfElection { get; set; }
    
    public int? NumberToElect { get; set; }
    
    public DateTime? OnlineWhenOpen { get; set; }
    
    public DateTime? OnlineWhenClose { get; set; }
    
    public bool IsOpen { get; set; }
    
    public string? Instructions { get; set; }
}
