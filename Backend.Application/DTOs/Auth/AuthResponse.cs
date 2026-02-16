namespace Backend.Application.DTOs.Auth;

public class AuthResponse
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string Email { get; set; } = null!;
    public string? Name { get; set; }
    public string AuthMethod { get; set; } = "Local";
    public bool Requires2FA { get; set; }
    public bool RequiresEmailVerification { get; set; }
}


