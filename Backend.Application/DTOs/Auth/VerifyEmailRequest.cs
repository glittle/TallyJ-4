using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;
}

