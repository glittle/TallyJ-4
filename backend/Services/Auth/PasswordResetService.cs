using Backend.Context;
using Backend.DTOs.Auth;
using Backend.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

public class PasswordResetService : IPasswordResetService
{
    public const string AcceptedKey = "auth.forgotPasswordPage.sent";
    public const string CompletedKey = "auth.resetPasswordPage.success";
    public const string InvalidTokenKey = "auth.errors.invalidResetToken";
    public const string FailedKey = "auth.errors.failedToResetPassword";

    private readonly UserManager<AppUser> _userManager;
    private readonly EmailService _emailService;
    private readonly MainDbContext _context;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        UserManager<AppUser> userManager,
        EmailService emailService,
        MainDbContext context,
        ILogger<PasswordResetService> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> GenerateResetTokenAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || string.IsNullOrWhiteSpace(user.Email) || !CanReset(user))
            {
                return (true, null);
            }

            if (!await _userManager.HasPasswordAsync(user))
            {
                return (true, null);
            }

            // Drop any leftover plaintext column value. Identity holds the real token.
            if (user.PasswordResetToken != null || user.PasswordResetExpiry != null)
            {
                user.PasswordResetToken = null;
                user.PasswordResetExpiry = null;
                await _userManager.UpdateAsync(user);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailService.SendPasswordResetEmailAsync(user.Email, token);
            return (true, null);
        }
        catch (Exception ex)
        {
            // Same reply as an unknown address. Do not log the address, token, or exception text.
            _logger.LogError(
                "Password reset email could not be sent ({ExceptionType})",
                ex.GetType().Name);
            return (true, null);
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !CanReset(user))
            {
                return (false, InvalidTokenKey);
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return (false, MapErrors(result));
            }

            try
            {
                user.PasswordResetToken = null;
                user.PasswordResetExpiry = null;
                await _userManager.UpdateAsync(user);
                await RevokeRefreshTokensAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Password reset cleanup failed ({ExceptionType})",
                    ex.GetType().Name);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("Password reset failed ({ExceptionType})", ex.GetType().Name);
            return (false, FailedKey);
        }
    }

    /// <summary>
    /// Local is one entry in <see cref="AppUser.AuthMethod"/> (a linked Google account is "Local,Google").
    /// </summary>
    internal static bool CanReset(AppUser user)
    {
        if (string.IsNullOrWhiteSpace(user.AuthMethod))
        {
            return false;
        }

        foreach (var part in user.AuthMethod.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task RevokeRefreshTokensAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ToListAsync();

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedReason = "Password reset";
        }

        await _context.SaveChangesAsync();
    }

    private static string MapErrors(IdentityResult result)
    {
        if (result.Errors.Any(error => error.Code == "InvalidToken"))
        {
            return InvalidTokenKey;
        }

        var keys = result.Errors
            .Select(error => error.Code switch
            {
                "PasswordTooShort" => "auth.passwordMinLength12",
                "PasswordRequiresDigit" => "auth.passwordRequireDigit",
                "PasswordRequiresLower" => "auth.passwordRequireLowercase",
                "PasswordRequiresUpper" => "auth.passwordRequireUppercase",
                "PasswordRequiresNonAlphanumeric" => "auth.passwordRequireSpecial",
                "PasswordRequiresUniqueChars" => "auth.errors.invalidPassword",
                _ => null
            })
            .Where(key => key != null)
            .Distinct()
            .ToList();

        return keys.Count == 0 ? FailedKey : string.Join('\n', keys);
    }
}
