using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// Audit trail of AppUser email address changes (self-service or SuperAdmin).
/// </summary>
[Table("UserEmailChangeLogs")]
[Index(nameof(UserId), nameof(ChangedAt), Name = "IX_UserEmailChangeLogs_UserId_ChangedAt")]
public class UserEmailChangeLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = null!;

    [Required]
    [StringLength(256)]
    public string OldEmail { get; set; } = null!;

    [Required]
    [StringLength(256)]
    public string NewEmail { get; set; } = null!;

    [Required]
    [Precision(0)]
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// User who performed the change (same as UserId for self-service; SuperAdmin id when admin-initiated).
    /// </summary>
    [StringLength(450)]
    public string? ChangedByUserId { get; set; }

    /// <summary>
    /// "Self" or "SuperAdmin".
    /// </summary>
    [Required]
    [StringLength(40)]
    public string Source { get; set; } = "Self";
}
