using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

/// <summary>
/// Entity representing a security audit log entry.
/// </summary>
[Table("SecurityAuditLogs")]
[Index("Timestamp", Name = "IX_SecurityAuditLogs_Timestamp")]
[Index("EventType", Name = "IX_SecurityAuditLogs_EventType")]
[Index("UserId", Name = "IX_SecurityAuditLogs_UserId")]
[Index("IsSuspicious", Name = "IX_SecurityAuditLogs_IsSuspicious")]
public class SecurityAuditLog
{
    /// <summary>
    /// The unique identifier of the security audit log entry.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// The timestamp when the security event occurred.
    /// </summary>
    [Required]
    [Precision(2)]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The type of security event.
    /// </summary>
    [Required]
    public Backend.Domain.SecurityEventType EventType { get; set; }

    /// <summary>
    /// The user ID associated with the event (if applicable).
    /// </summary>
    [StringLength(450)] // Matches ASP.NET Identity User.Id length
    public string? UserId { get; set; }

    /// <summary>
    /// The email address associated with the event (if applicable).
    /// </summary>
    [StringLength(256)] // Standard email length
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// The IP address of the client.
    /// </summary>
    [StringLength(45)] // IPv6 addresses can be up to 45 characters
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent string from the client.
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional details about the security event.
    /// </summary>
    [StringLength(2000)]
    public string? Details { get; set; }

    /// <summary>
    /// Whether the event indicates suspicious activity.
    /// </summary>
    [Required]
    public bool IsSuspicious { get; set; } = false;

    /// <summary>
    /// The severity level of the security event.
    /// </summary>
    [Required]
    public Backend.Domain.SecurityEventSeverity Severity { get; set; } = Backend.Domain.SecurityEventSeverity.Info;

    /// <summary>
    /// Additional metadata stored as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }
}


