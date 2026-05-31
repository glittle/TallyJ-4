using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class RefreshTokenRequest
{
    // Optional - can be provided in request body or read from httpOnly cookie
    public string? RefreshToken { get; set; }
}

