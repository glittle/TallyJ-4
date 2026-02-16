using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class Disable2FARequest
{
    [Required]
    public string Password { get; set; } = null!;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = null!;
}


