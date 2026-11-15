using Backend.DTOs.Auth;

namespace Backend.Services.Auth;

public interface ILocalAuthService
{
    /// <summary>
    /// Creates a local email/password user and sends the verification email.
    /// Open <c>POST /api/auth/registerAccount</c> and invite redemption both
    /// call this. The new row is restricted. Invite redemption then marks
    /// that account approved.
    /// </summary>
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Local email/password login. See <see cref="LocalAuthService.LoginAsync"/>.
    /// </summary>
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
}
