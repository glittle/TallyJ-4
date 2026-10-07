using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Backend.DTOs.Auth;
using Backend.Context;
using Backend.Identity;
using Backend.Middleware;

namespace Backend.Services.Auth;

public class LocalAuthService : ILocalAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly MainDbContext _context;
    private readonly IStringLocalizer<LocalAuthService> _localizer;
    private readonly EmailService _emailService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalAuthService(
        UserManager<AppUser> userManager,
        IJwtTokenService jwtTokenService,
        MainDbContext context,
        IStringLocalizer<LocalAuthService> localizer,
        EmailService emailService,
        ITwoFactorService twoFactorService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _localizer = localizer;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Creates a local email/password user. Not used by open HTTP register
    /// (<c>POST /api/auth/registerAccount</c> is disabled). Called by
    /// <see cref="AccountInviteService"/> after a one-time invite is consumed.
    /// </summary>
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
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? null
                : request.DisplayName.Trim(),
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
            MarkInvalidCredential();
            return (false, _localizer["auth.errors.invalidCredentials"], null);
        }

        // Check if email is verified
        if (!user.EmailConfirmed)
        {
            return (false, _localizer["auth.errors.emailNotVerified"], null);
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

            MarkInvalidCredential();
            return (false, _localizer["auth.errors.invalidCredentials"], null);
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                // Password matched and no code was offered. This is the prompt, not a guess.
                // Clear earlier password failures here. A supplied code is checked below
                // without this reset, or a wrong code would start from zero every time.
                await _userManager.ResetAccessFailedCountAsync(user);
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
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return (false, _localizer["auth.errors.accountLocked"], null);
                }

                MarkInvalidCredential();
                return (false, codeError ?? _localizer["auth.errors.invalid2FACode"], null);
            }
        }

        // Password matched, and the two-factor code matched when one was required.
        await _userManager.ResetAccessFailedCountAsync(user);

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

    private void MarkInvalidCredential()
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            RateLimitingMiddleware.MarkIpFailure(_httpContextAccessor.HttpContext);
        }
    }
}


