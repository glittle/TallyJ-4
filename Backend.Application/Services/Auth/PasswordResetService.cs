using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Backend.Application.DTOs.Auth;
using Backend.Domain.Identity;

namespace Backend.Application.Services.Auth;

public class PasswordResetService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IStringLocalizer<PasswordResetService> _localizer;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public PasswordResetService(
        UserManager<AppUser> userManager,
        IStringLocalizer<PasswordResetService> localizer,
        IConfiguration configuration,
        EmailService emailService)
    {
        _userManager = userManager;
        _localizer = localizer;
        _configuration = configuration;
        _emailService = emailService;
    }

    [Obsolete("This constructor is for testing purposes only. Use the parameterized constructor in production.")]
    public PasswordResetService()
    {
    }

    public async Task<(bool Success, string? Error)> GenerateResetTokenAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (true, null);
        }

        if (user.AuthMethod != "Local")
        {
            return (false, _localizer["auth.errors.passwordResetNotAvailableForOAuth"]);
        }

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, _localizer["auth.errors.failedToGenerateResetToken"]);
        }

        await _emailService.SendPasswordResetEmailAsync(user.Email!, token);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, _localizer["auth.errors.invalidResetToken"]);
        }

        if (user.PasswordResetToken != request.Token)
        {
            return (false, _localizer["auth.errors.invalidResetToken"]);
        }

        if (user.PasswordResetExpiry == null || user.PasswordResetExpiry < DateTime.UtcNow)
        {
            return (false, _localizer["auth.errors.resetTokenExpired"]);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }

        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        await _userManager.UpdateAsync(user);

        return (true, null);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}


