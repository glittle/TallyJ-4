using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Entities;

namespace Backend.Identity;

public class AppUser : IdentityUser
{
    [StringLength(200)]
    public string? DisplayName { get; set; }

    public string? GoogleId { get; set; }

    /// <summary>
    /// The Telegram user ID for users who authenticate via Telegram Login Widget.
    /// </summary>
    public string? TelegramId { get; set; }

    [Required]
    [StringLength(20)]
    public string AuthMethod { get; set; } = "Local";

    public string? PasswordResetToken { get; set; }

    [Precision(0)]
    public DateTimeOffset? PasswordResetExpiry { get; set; }

    /// <summary>
    /// Email address awaiting confirmation for an in-progress email change (current Email remains active until confirmed).
    /// </summary>
    [StringLength(256)]
    public string? PendingEmail { get; set; }

    /// <summary>
    /// Short numeric code sent to PendingEmail for manual confirmation.
    /// </summary>
    [StringLength(10)]
    public string? PendingEmailCode { get; set; }

    /// <summary>
    /// Opaque token for link-based confirmation of PendingEmail.
    /// </summary>
    [StringLength(128)]
    public string? PendingEmailToken { get; set; }

    [Precision(0)]
    public DateTimeOffset? PendingEmailExpiry { get; set; }

    public virtual TwoFactorToken? TwoFactorToken { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}


