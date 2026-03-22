using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OtpNet;
using QRCoder;
using Backend.Application.DTOs.Auth;
using Backend.Domain.Entities;
using Backend.Domain.Identity;
using Backend.Domain.Context;

namespace Backend.Application.Services.Auth;

public class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IStringLocalizer<TwoFactorService> _localizer;
    private readonly MainDbContext _dbContext;
    private readonly EmailService _emailService;
    private readonly EncryptionService _encryptionService;

    public TwoFactorService(
        UserManager<AppUser> userManager,
        IStringLocalizer<TwoFactorService> localizer,
        MainDbContext dbContext,
        EmailService emailService,
        EncryptionService encryptionService)
    {
        _userManager = userManager;
        _localizer = localizer;
        _dbContext = dbContext;
        _emailService = emailService;
        _encryptionService = encryptionService;
    }

    public async Task<(bool Success, string? Error, TwoFactorSetupResponse? Response)> SetupAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, _localizer["auth.errors.userNotFound"], null);
        }

        if (user.TwoFactorEnabled)
        {
            return (false, _localizer["auth.errors.twoFactorAlreadyEnabled"], null);
        }

        var secret = GenerateSecret();
        var encryptedSecret = _encryptionService.Encrypt(secret);

        var twoFactorToken = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = user.Id,
            Secret = encryptedSecret,
            IsEnabled = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Set<TwoFactorToken>().Add(twoFactorToken);
        await _dbContext.SaveChangesAsync();

        var qrCodeDataUrl = GenerateQrCode(user.Email!, secret);

        return (true, null, new TwoFactorSetupResponse
        {
            Secret = secret,
            QrCodeDataUrl = qrCodeDataUrl
        });
    }

    public async Task<(bool Success, string? Error)> EnableAsync(string userId, Enable2FARequest request)
    {
        var user = await _userManager.Users
            .Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (false, _localizer["auth.errors.userNotFound"]);
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
        user.TwoFactorToken.VerifiedAt = DateTime.UtcNow;
        user.TwoFactorEnabled = true;

        await _dbContext.SaveChangesAsync();

        var qrCodeDataUrl = GenerateQrCode(user.Email!, secret);
        await _emailService.Send2FASetupEmailAsync(user.Email!, qrCodeDataUrl);

        return (true, null);
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
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
    }

    private static string GenerateQrCode(string email, string secret)
    {
        var totpUri = $"otpauth://totp/Backend:{email}?secret={secret}&issuer=Backend";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }
}


