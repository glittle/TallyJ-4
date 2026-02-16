using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class Verify2FARequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = null!;
}


