using System.ComponentModel.DataAnnotations;

namespace TallyJ4.DTOs.Security;

/// <summary>
/// Data transfer object for creating a new security audit log entry.
/// </summary>
public class CreateSecurityAuditLogDto
{
    /// <summary>
    /// The type of security event being logged.
    /// </summary>
    [Required]
    public TallyJ4.Domain.SecurityEventType EventType { get; set; }

    /// <summary>
    /// The user ID associated with the event (if applicable).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The email address associated with the event (if applicable).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The IP address of the client making the request.
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
    /// Whether the event indicates a security threat or suspicious activity.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Severity level of the security event.
    /// </summary>
    public TallyJ4.Domain.SecurityEventSeverity Severity { get; set; } = TallyJ4.Domain.SecurityEventSeverity.Info;

    /// <summary>
    /// Additional metadata as key-value pairs (JSON serialized).
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}