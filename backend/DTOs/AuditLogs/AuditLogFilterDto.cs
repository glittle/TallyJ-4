namespace TallyJ4.DTOs.AuditLogs;

public class AuditLogFilterDto
{
    public Guid? ElectionGuid { get; set; }
    
    public Guid? LocationGuid { get; set; }
    
    public string? VoterId { get; set; }
    
    public string? ComputerCode { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public string? SearchTerm { get; set; }
}
