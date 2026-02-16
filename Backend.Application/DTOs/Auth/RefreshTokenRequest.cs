using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}

