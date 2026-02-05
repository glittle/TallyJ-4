namespace TallyJ4.DTOs.AuditLogs;

/// <summary>
/// Data transfer object for filtering audit logs.
/// </summary>
public class AuditLogFilterDto
{
    /// <summary>
    /// Filter by election GUID.
    /// </summary>
    public Guid? ElectionGuid { get; set; }

    /// <summary>
    /// Filter by location GUID.
    /// </summary>
    public Guid? LocationGuid { get; set; }

    /// <summary>
    /// Filter by voter ID.
    /// </summary>
    public string? VoterId { get; set; }

    /// <summary>
    /// Filter by computer code.
    /// </summary>
    public string? ComputerCode { get; set; }

    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Search term to filter audit log details.
    /// </summary>
    public string? SearchTerm { get; set; }
}
