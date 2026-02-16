using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Backend.Domain.Entities;

namespace Backend.EF.Identity;

/// <summary>
/// Extended Identity user class for the TallyJ application.
/// Includes additional properties for authentication methods and password reset functionality.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>
    /// The Google ID for users who authenticate via Google OAuth.
    /// </summary>
    public string? GoogleId { get; set; }

    /// <summary>
    /// The authentication method used by this user (e.g., "Local", "Google").
    /// </summary>
    [Required]
    [StringLength(20)]
    public string AuthMethod { get; set; } = "Local";

    /// <summary>
    /// Token used for password reset operations.
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Expiry date and time for the password reset token.
    /// </summary>
    public DateTime? PasswordResetExpiry { get; set; }

    /// <summary>
    /// The two-factor authentication token associated with this user.
    /// </summary>
    public virtual TwoFactorToken? TwoFactorToken { get; set; }
}


