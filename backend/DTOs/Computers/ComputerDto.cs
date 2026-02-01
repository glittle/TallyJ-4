namespace TallyJ4.DTOs.Computers;

public class ComputerDto
{
    public Guid ComputerGuid { get; set; }
    
    public Guid ElectionGuid { get; set; }
    
    public Guid LocationGuid { get; set; }
    
    public string ComputerCode { get; set; } = null!;
    
    public string? BrowserInfo { get; set; }
    
    public string? IpAddress { get; set; }
    
    public DateTime? LastActivity { get; set; }
    
    public DateTime? RegisteredAt { get; set; }
    
    public bool? IsActive { get; set; }
}
