namespace TallyJ4.DTOs.Computers;

public class RegisterComputerDto
{
    public Guid ElectionGuid { get; set; }
    
    public Guid LocationGuid { get; set; }
    
    public string? ComputerCode { get; set; }
    
    public string? BrowserInfo { get; set; }
    
    public string? IpAddress { get; set; }
}
