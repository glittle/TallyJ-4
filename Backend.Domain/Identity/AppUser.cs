using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Backend.Domain.Entities;

namespace Backend.Domain.Identity;

public class AppUser : IdentityUser
{
    [StringLength(200)]
    public string? DisplayName { get; set; }

    public string? GoogleId { get; set; }

    [Required]
    [StringLength(20)]
    public string AuthMethod { get; set; } = "Local";

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetExpiry { get; set; }

    public virtual TwoFactorToken? TwoFactorToken { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}


