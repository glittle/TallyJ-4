namespace TallyJ4.DTOs.AuditLogs;

public class AuditLogDto
{
    public int RowId { get; set; }
    
    public DateTime AsOf { get; set; }
    
    public Guid? ElectionGuid { get; set; }
    
    public Guid? LocationGuid { get; set; }
    
    public string? VoterId { get; set; }
    
    public string? ComputerCode { get; set; }
    
    public string? Details { get; set; }
    
    public string? HostAndVersion { get; set; }
}
