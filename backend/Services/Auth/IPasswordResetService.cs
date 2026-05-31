using Backend.DTOs.Auth;

namespace Backend.Services.Auth;

public interface IPasswordResetService
{
    Task<(bool Success, string? Error)> GenerateResetTokenAsync(ForgotPasswordRequest request);
    Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request);
}
