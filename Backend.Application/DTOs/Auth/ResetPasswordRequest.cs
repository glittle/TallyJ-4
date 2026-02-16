using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = null!;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = null!;
    
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = null!;
}


