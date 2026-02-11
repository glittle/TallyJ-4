using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Application.Services.Auth;

public class LocalAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly MainDbContext _context;
    private readonly IStringLocalizer<LocalAuthService> _localizer;

    public LocalAuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenService jwtTokenService,
        MainDbContext context,
        IStringLocalizer<LocalAuthService> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _localizer = localizer;
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, _localizer["auth.errors.emailAlreadyExists"], null);
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
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return (true, null, new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Email = user.Email!,
            Name = user.DisplayName,
            AuthMethod = user.AuthMethod,
            Requires2FA = false
        });
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, _localizer["auth.errors.invalidCredentials"], null);
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, _localizer["auth.errors.accountLocked"], null);
        }

        // Use SignInManager.PasswordSignInAsync to enable lockout tracking
        var signInResult = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return (false, _localizer["auth.errors.accountLocked"], null);
        }

        if (!signInResult.Succeeded)
        {
            return (false, _localizer["auth.errors.invalidCredentials"], null);
        }

        // Handle 2FA if enabled
        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return (true, null, new AuthResponse
                {
                    Token = "",
                    RefreshToken = "",
                    Email = user.Email!,
                    Name = user.DisplayName,
                    AuthMethod = user.AuthMethod,
                    Requires2FA = true
                });
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return (true, null, new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Email = user.Email!,
            Name = user.DisplayName,
            AuthMethod = user.AuthMethod,
            Requires2FA = false
        });
    }
}
