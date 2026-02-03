namespace TallyJ4.DTOs.Tellers;

public class CreateTellerDto
{
    public Guid ElectionGuid { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string? UsingComputerCode { get; set; }
    
    public bool IsHeadTeller { get; set; }
}
