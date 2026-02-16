using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}


