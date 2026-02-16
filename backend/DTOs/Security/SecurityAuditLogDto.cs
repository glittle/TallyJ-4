namespace Backend.DTOs.Security;

/// <summary>
/// Data transfer object for security audit log entries.
/// </summary>
public class SecurityAuditLogDto
{
    /// <summary>
    /// The unique identifier of the security audit log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The timestamp when the security event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The type of security event.
    /// </summary>
    public Backend.Domain.SecurityEventType EventType { get; set; }

    /// <summary>
    /// The user ID associated with the event (if applicable).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The email address associated with the event (if applicable).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The IP address of the client.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent string from the client.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional details about the security event.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Whether the event indicates suspicious activity.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// The severity level of the security event.
    /// </summary>
    public Backend.Domain.SecurityEventSeverity Severity { get; set; }

    /// <summary>
    /// Additional metadata as key-value pairs.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}



