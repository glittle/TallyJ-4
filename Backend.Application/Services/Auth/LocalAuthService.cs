using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Backend.Application.DTOs.Auth;
using Backend.Domain.Context;
using Backend.Domain.Identity;

namespace Backend.Application.Services.Auth;

public class LocalAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly MainDbContext _context;
    private readonly IStringLocalizer<LocalAuthService> _localizer;
    private readonly EmailService _emailService;
    private readonly TwoFactorService _twoFactorService;

    public LocalAuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenService jwtTokenService,
        MainDbContext context,
        IStringLocalizer<LocalAuthService> localizer,
        EmailService emailService,
        TwoFactorService twoFactorService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _localizer = localizer;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
    }

    // Parameterless constructor for testing purposes only
    [Obsolete("This constructor is for testing purposes only. Use the parameterized constructor in production.")]
    public LocalAuthService()
    {
        // This constructor exists only to allow Moq to create proxy instances for testing
        // It should never be used in production code
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

    public virtual async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
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

        // Use SignInManager.PasswordSignInAsync to enable lockout tracking
        var signInResult = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return (false, _localizer["auth.errors.accountLocked"], null);
        }

        if (signInResult.RequiresTwoFactor || user.TwoFactorEnabled)
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
        else if (!signInResult.Succeeded)
        {
            return (false, _localizer["auth.errors.invalidCredentials"], null);
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


