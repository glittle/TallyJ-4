namespace TallyJ4.DTOs.Tellers;

public class UpdateTellerDto
{
    public string Name { get; set; } = null!;
    
    public string? UsingComputerCode { get; set; }
    
    public bool IsHeadTeller { get; set; }
}
