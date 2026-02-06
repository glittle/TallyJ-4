namespace TallyJ4.DTOs.AuditLogs;

/// <summary>
/// Data transfer object for creating a new audit log entry.
/// </summary>
public class CreateAuditLogDto
{
    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid? ElectionGuid { get; set; }

    /// <summary>
    /// The unique identifier of the location.
    /// </summary>
    public Guid? LocationGuid { get; set; }

    /// <summary>
    /// The voter ID.
    /// </summary>
    public string? VoterId { get; set; }

    /// <summary>
    /// The computer code.
    /// </summary>
    public string? ComputerCode { get; set; }

    /// <summary>
    /// Details of the action being audited.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// The host and version information.
    /// </summary>
    public string? HostAndVersion { get; set; }
}
