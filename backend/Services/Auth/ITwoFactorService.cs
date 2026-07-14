using Backend.DTOs.Auth;

namespace Backend.Services.Auth;

public interface ITwoFactorService
{
    Task<(bool Success, string? Error, TwoFactorSetupResponse? Response)> SetupAsync(string userId);
    Task<(bool Success, string? Error)> EnableAsync(string userId, Enable2FARequest request);
    Task<(bool Success, string? Error)> VerifyAsync(string userId, string code);
    Task<(bool Success, string? Error)> DisableAsync(string userId, Disable2FARequest request);

    /// <summary>
    /// Returns whether 2FA is enabled for the user. Non-Local-only accounts cannot use app TOTP;
    /// any leftover 2FA state is cleared so status never implies protection that is not enforced.
    /// </summary>
    Task<(bool Success, string? Error, bool IsEnabled, string? Method)> GetStatusAsync(string userId);
}
