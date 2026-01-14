using System.ComponentModel.DataAnnotations;

namespace TallyJ4.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;
    
    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = null!;
}
