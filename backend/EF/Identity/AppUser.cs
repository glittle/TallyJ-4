using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using TallyJ4.Domain.Entities;

namespace TallyJ4.EF.Identity;

public class AppUser : IdentityUser
{
    public string? GoogleId { get; set; }
    
    [Required]
    [StringLength(20)]
    public string AuthMethod { get; set; } = "Local";
    
    public string? PasswordResetToken { get; set; }
    
    public DateTime? PasswordResetExpiry { get; set; }
    
    public virtual TwoFactorToken? TwoFactorToken { get; set; }
}