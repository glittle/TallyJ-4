using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;
using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Identity;
using Backend.Context;

namespace Backend.Services.Auth;

public class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IStringLocalizer<TwoFactorService> _localizer;
    private readonly MainDbContext _dbContext;
    private readonly EmailService _emailService;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        UserManager<AppUser> userManager,
        IStringLocalizer<TwoFactorService> localizer,
        MainDbContext dbContext,
        EmailService emailService,
        EncryptionService encryptionService,
        ILogger<TwoFactorService> logger)
    {
        _userManager = userManager;
        _localizer = localizer;
        _dbContext = dbContext;
        _emailService = emailService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, TwoFactorSetupResponse? Response)> SetupAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, _localizer["auth.errors.userNotFound"], null);
            }

            if (!IsLocalCapable(user))
            {
                return (false, _localizer["auth.errors.twoFactorOnlyForLocal"], null);
            }

            if (user.TwoFactorEnabled)
            {
                return (false, _localizer["auth.errors.twoFactorAlreadyEnabled"], null);
            }

            var secret = GenerateSecret();
            var encryptedSecret = _encryptionService.Encrypt(secret);

            // Incomplete prior setup leaves a row (unique on UserId). Replace it so
            // restarting setup issues a fresh secret/QR instead of a duplicate-key error.
            var existingToken = await _dbContext.Set<TwoFactorToken>()
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (existingToken != null)
            {
                if (existingToken.IsEnabled)
                {
                    // Defensive: token enabled but user flag not — treat as already on.
                    return (false, _localizer["auth.errors.twoFactorAlreadyEnabled"], null);
                }

                existingToken.TokenGuid = Guid.NewGuid();
                existingToken.Secret = encryptedSecret;
                existingToken.IsEnabled = false;
                existingToken.CreatedAt = DateTimeOffset.UtcNow;
                existingToken.VerifiedAt = null;
            }
            else
            {
                _dbContext.Set<TwoFactorToken>().Add(new TwoFactorToken
                {
                    TokenGuid = Guid.NewGuid(),
                    UserId = user.Id,
                    Secret = encryptedSecret,
                    IsEnabled = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();

            var qrCodeDataUrl = GenerateQrCode(user.Email!, secret);

            return (true, null, new TwoFactorSetupResponse
            {
                Secret = secret,
                QrCodeDataUrl = qrCodeDataUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up 2FA for user {UserId}", userId);
            return (false, _localizer["auth.errors.failedToSetup2FA"], null);
        }
    }

    public async Task<(bool Success, string? Error)> EnableAsync(string userId, Enable2FARequest request)
    {
        try
        {
            var user = await _userManager.Users
                .Include(u => u.TwoFactorToken)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return (false, _localizer["auth.errors.userNotFound"]);
            }

            if (!IsLocalCapable(user))
            {
                return (false, _localizer["auth.errors.twoFactorOnlyForLocal"]);
            }

            if (user.TwoFactorToken == null)
            {
                return (false, _localizer["auth.errors.twoFactorNotSetup"]);
            }

            var secret = _encryptionService.Decrypt(user.TwoFactorToken.Secret);
            var isValid = VerifyCode(secret, request.Code);

            if (!isValid)
            {
                return (false, _localizer["auth.errors.invalid2FACode"]);
            }

            user.TwoFactorToken.IsEnabled = true;
            user.TwoFactorToken.VerifiedAt = DateTimeOffset.UtcNow;
            user.TwoFactorEnabled = true;

            await _dbContext.SaveChangesAsync();

            var qrCodeDataUrl = GenerateQrCode(user.Email!, secret);
            await _emailService.Send2FASetupEmailAsync(user.Email!, qrCodeDataUrl);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for user {UserId}", userId);
            return (false, _localizer["auth.errors.failedToEnable2FA"]);
        }
    }

    public async Task<(bool Success, string? Error)> VerifyAsync(string userId, string code)
    {
        var user = await _userManager.Users
            .Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.TwoFactorToken == null)
        {
            return (false, _localizer["auth.errors.invalid2FACode"]);
        }

        // Only Local password login enforces TOTP; reject verification for pure OAuth accounts.
        if (!IsLocalCapable(user))
        {
            return (false, _localizer["auth.errors.twoFactorOnlyForLocal"]);
        }

        var secret = _encryptionService.Decrypt(user.TwoFactorToken.Secret);
        var isValid = VerifyCode(secret, code);

        if (!isValid)
        {
            return (false, _localizer["auth.errors.invalid2FACode"]);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DisableAsync(string userId, Disable2FARequest request)
    {
        var user = await _userManager.Users
            .Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (false, _localizer["auth.errors.userNotFound"]);
        }

        if (!IsLocalCapable(user))
        {
            return (false, _localizer["auth.errors.twoFactorOnlyForLocal"]);
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            return (false, _localizer["auth.errors.invalidPassword"]);
        }

        if (user.TwoFactorToken == null)
        {
            return (false, _localizer["auth.errors.twoFactorNotSetup"]);
        }

        var secret = _encryptionService.Decrypt(user.TwoFactorToken.Secret);
        var isValidCode = VerifyCode(secret, request.Code);

        if (!isValidCode)
        {
            return (false, _localizer["auth.errors.invalid2FACode"]);
        }

        _dbContext.Set<TwoFactorToken>().Remove(user.TwoFactorToken);
        user.TwoFactorEnabled = false;

        await _dbContext.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error, bool IsEnabled, string? Method)> GetStatusAsync(string userId)
    {
        var user = await _userManager.Users
            .Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (false, _localizer["auth.errors.userNotFound"], false, null);
        }

        if (!IsLocalCapable(user))
        {
            await ClearOrphanedTwoFactorAsync(user);
            return (true, null, false, null);
        }

        var isEnabled = user.TwoFactorEnabled;
        return (true, null, isEnabled, isEnabled ? "totp" : null);
    }

    /// <summary>
    /// App TOTP is only for accounts that can log in with email/password.
    /// <c>AuthMethod</c> may be multi-value (e.g. "Local,Google").
    /// </summary>
    internal static bool IsLocalCapable(AppUser user)
    {
        if (string.IsNullOrWhiteSpace(user.AuthMethod))
        {
            return false;
        }

        return user.AuthMethod
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(m => string.Equals(m, "Local", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Removes leftover 2FA state for OAuth-only users so status never claims protection
    /// that external login paths do not enforce.
    /// </summary>
    private async Task ClearOrphanedTwoFactorAsync(AppUser user)
    {
        var changed = false;

        if (user.TwoFactorToken != null)
        {
            _dbContext.Set<TwoFactorToken>().Remove(user.TwoFactorToken);
            changed = true;
        }

        if (user.TwoFactorEnabled)
        {
            user.TwoFactorEnabled = false;
            changed = true;
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation(
                "Cleared orphaned 2FA state for non-Local user {UserId} (AuthMethod={AuthMethod})",
                user.Id,
                user.AuthMethod);
        }
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encoding.ToString(bytes);
    }

    private static bool VerifyCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(5, 5));
    }

    /// <summary>
    /// Builds a TOTP otpauth QR for authenticator apps (Authy, Google Authenticator, etc.).
    /// The issuer is the localized site title (English: "TallyJ v4") so tokens show a
    /// recognizable product name. Apps do not support a custom icon URL in this URI.
    /// </summary>
    private string GenerateQrCode(string email, string secret)
    {
        var issuer = ResolveTotpIssuer();
        var label = $"{issuer}:{email}";
        var totpUri =
            $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }

    /// <summary>
    /// Prefer <c>common.appTitle</c> so the authenticator label tracks the product name in i18n.
    /// Falls back to the English brand if localization is unavailable (e.g. unit tests).
    /// </summary>
    private string ResolveTotpIssuer()
    {
        var localized = _localizer["common.appTitle"];
        if (!localized.ResourceNotFound && !string.IsNullOrWhiteSpace(localized.Value))
        {
            return localized.Value;
        }

        return "TallyJ v4";
    }
}


