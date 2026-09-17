using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// One-time SuperAdmin invite that authorizes creating a single local email/password account.
/// The raw token is never stored; only <see cref="TokenHash"/> is persisted.
/// </summary>
[Table("AccountInvites")]
[Index(nameof(TokenHash), IsUnique = true, Name = "IX_AccountInvites_TokenHash")]
public class AccountInvite
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// SHA-256 hex of the raw invite token. The raw token is returned once at issue time.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = null!;

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = null!;

    [Precision(0)]
    public DateTimeOffset CreatedAt { get; set; }

    [Precision(0)]
    public DateTimeOffset ExpiresAt { get; set; }

    [Precision(0)]
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>
    /// Identity user created when this invite was redeemed. Null until successful register.
    /// </summary>
    [StringLength(450)]
    public string? CreatedUserId { get; set; }
}
