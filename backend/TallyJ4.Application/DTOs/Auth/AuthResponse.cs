namespace TallyJ4.Application.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Name { get; set; }
    public string AuthMethod { get; set; } = "Local";
    public bool Requires2FA { get; set; }
    public bool RequiresEmailVerification { get; set; }
}
