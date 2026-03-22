using Backend.Application.DTOs.Auth;

namespace Backend.Application.Services.Auth;

public interface ITwoFactorService
{
    Task<(bool Success, string? Error, TwoFactorSetupResponse? Response)> SetupAsync(string userId);
    Task<(bool Success, string? Error)> EnableAsync(string userId, Enable2FARequest request);
    Task<(bool Success, string? Error)> VerifyAsync(string userId, string code);
    Task<(bool Success, string? Error)> DisableAsync(string userId, Disable2FARequest request);
}
