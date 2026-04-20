using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Backend.Application.DTOs.Auth;
using Backend.Domain.Context;
using Backend.Domain.Identity;

namespace Backend.Application.Services.Auth;

public class LocalAuthService : ILocalAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly MainDbContext _context;
    private readonly IStringLocalizer<LocalAuthService> _localizer;
    private readonly EmailService _emailService;
    private readonly ITwoFactorService _twoFactorService;

    public LocalAuthService(
        UserManager<AppUser> userManager,
        IJwtTokenService jwtTokenService,
        MainDbContext context,
        IStringLocalizer<LocalAuthService> localizer,
        EmailService emailService,
        ITwoFactorService twoFactorService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _localizer = localizer;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
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

        // Generate email verification token
        var emailVerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Send verification email
        try
        {
            await _emailService.SendEmailVerificationEmailAsync(user.Email!, emailVerificationToken);
        }
        catch (Exception ex)
        {
            // Log the error but don't fail registration - user can request verification later
            // In a production system, you might want to handle this differently
            Console.WriteLine($"Failed to send verification email: {ex.Message}");
        }

        // Note: We don't generate JWT tokens for unverified users
        // They need to verify their email first before they can log in

        return (true, null, new AuthResponse
        {
            Token = "",
            RefreshToken = "",
            Email = user.Email!,
            Name = user.DisplayName,
            AuthMethod = user.AuthMethod,
            Requires2FA = false,
            RequiresEmailVerification = true
        });
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, _localizer["auth.errors.invalidCredentials"], null);
        }

        // Check if email is verified
        if (!user.EmailConfirmed)
        {
            return (false, "Email not verified. Please check your email and verify your account before logging in.", null);
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, _localizer["auth.errors.accountLocked"], null);
        }

        // Check password and handle lockout manually
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            // Increment access failed count for lockout tracking
            await _userManager.AccessFailedAsync(user);

            // Check if account is now locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                return (false, _localizer["auth.errors.accountLocked"], null);
            }

            return (false, _localizer["auth.errors.invalidCredentials"], null);
        }

        // Reset access failed count on successful password check
        await _userManager.ResetAccessFailedCountAsync(user);

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

            var (codeValid, codeError) = await _twoFactorService.VerifyAsync(user.Id, request.TwoFactorCode);
            if (!codeValid)
            {
                return (false, codeError ?? _localizer["auth.errors.invalid2FACode"], null);
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


