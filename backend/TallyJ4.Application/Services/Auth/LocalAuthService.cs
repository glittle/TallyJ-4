using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Application.Services.Auth;

public class LocalAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IStringLocalizer<LocalAuthService> _localizer;

    public LocalAuthService(
        UserManager<AppUser> userManager,
        JwtTokenService jwtTokenService,
        IStringLocalizer<LocalAuthService> localizer)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _localizer = localizer;
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, _localizer["EmailAlreadyExists"], null);
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false,
            AuthMethod = "Local"
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, errors, null);
        }

        var token = _jwtTokenService.GenerateToken(user);
        return (true, null, new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            Requires2FA = false
        });
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, _localizer["InvalidCredentials"], null);
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            return (false, _localizer["InvalidCredentials"], null);
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return (true, null, new AuthResponse
                {
                    Token = "",
                    Email = user.Email!,
                    Requires2FA = true
                });
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        return (true, null, new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            Requires2FA = false
        });
    }
}
