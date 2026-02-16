namespace Backend.DTOs.AuditLogs;

/// <summary>
/// Data transfer object representing an audit log entry.
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// The unique row identifier for the audit log entry.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// The timestamp when the audit log entry was created.
    /// </summary>
    public DateTime AsOf { get; set; }

    /// <summary>
    /// The unique identifier of the election associated with this audit log entry.
    /// </summary>
    public Guid? ElectionGuid { get; set; }

    /// <summary>
    /// The unique identifier of the location associated with this audit log entry.
    /// </summary>
    public Guid? LocationGuid { get; set; }

    /// <summary>
    /// The voter ID associated with this audit log entry.
    /// </summary>
    public string? VoterId { get; set; }

    /// <summary>
    /// The computer code associated with this audit log entry.
    /// </summary>
    public string? ComputerCode { get; set; }

    /// <summary>
    /// Details of the audited action.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// The host and version information.
    /// </summary>
    public string? HostAndVersion { get; set; }
}



