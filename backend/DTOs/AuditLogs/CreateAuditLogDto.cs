namespace TallyJ4.DTOs.AuditLogs;

public class CreateAuditLogDto
{
    public Guid? ElectionGuid { get; set; }
    
    public Guid? LocationGuid { get; set; }
    
    public string? VoterId { get; set; }
    
    public string? ComputerCode { get; set; }
    
    public string? Details { get; set; }
    
    public string? HostAndVersion { get; set; }
}
