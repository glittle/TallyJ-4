using Backend.DTOs.Auth;

namespace Backend.Services.Auth;

public interface ILocalAuthService
{
    /// <summary>
    /// Creates a local email/password user. Open HTTP register is disabled;
    /// only the SuperAdmin one-time invite redeem path calls this.
    /// </summary>
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
}
