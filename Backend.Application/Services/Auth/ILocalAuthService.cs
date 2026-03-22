using Backend.Application.DTOs.Auth;

namespace Backend.Application.Services.Auth;

public interface ILocalAuthService
{
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
}
