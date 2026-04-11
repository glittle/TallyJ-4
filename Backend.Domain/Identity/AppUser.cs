using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Domain.Entities;

namespace Backend.Domain.Identity;

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

    public virtual TwoFactorToken? TwoFactorToken { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}


