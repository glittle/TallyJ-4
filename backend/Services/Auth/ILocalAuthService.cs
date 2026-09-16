using Backend.DTOs.Auth;

namespace Backend.Services.Auth;

public interface ILocalAuthService
{
    /// <summary>
    /// Creates a local email/password user. Open HTTP register is disabled;
    /// leftover invite-only email signup may call this later.
    /// </summary>
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
}
