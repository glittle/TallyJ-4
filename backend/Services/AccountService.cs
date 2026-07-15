using System.Security.Cryptography;
using Backend.Context;
using Backend.DTOs.Account;
using Backend.DTOs.Security;
using Backend.Entities;
using Backend.Identity;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing user account operations including profile management and password changes.
/// </summary>
public class AccountService : IAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly MainDbContext _context;
    private readonly EmailService _emailService;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        UserManager<AppUser> userManager,
        MainDbContext context,
        EmailService emailService,
        ISecurityAuditService securityAuditService,
        ILogger<AccountService> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return null;
        }

        return MapProfile(user);
    }

    public async Task<UserProfileDto?> ChangeDisplayNameAsync(string userId, string displayName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var trimmed = displayName.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException("Display name cannot be empty");
        }

        if (trimmed.Length > 200)
        {
            throw new InvalidOperationException("Display name cannot exceed 200 characters");
        }

        user.DisplayName = trimmed;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update display name: {errors}");
        }

        return MapProfile(user);
    }

    public async Task RequestEmailChangeAsync(
        string userId,
        RequestEmailChangeDto dto,
        string? ipAddress,
        string? userAgent)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (!IsLocalOnlyAccount(user))
        {
            throw new InvalidOperationException("Email can only be changed for Local login accounts");
        }

        var newEmail = dto.NewEmail.Trim();
        if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("New email is the same as the current email");
        }

        if (!await _userManager.CheckPasswordAsync(user, dto.CurrentPassword))
        {
            throw new InvalidOperationException("Current password is incorrect");
        }

        var existing = await _userManager.FindByEmailAsync(newEmail);
        if (existing != null && existing.Id != userId)
        {
            throw new InvalidOperationException("Email already in use");
        }

        // Also reject if another user has this as pending (reduce races)
        var pendingTaken = await _context.Users.AnyAsync(u =>
            u.Id != userId &&
            u.PendingEmail != null &&
            u.PendingEmail.ToLower() == newEmail.ToLower());
        if (pendingTaken)
        {
            throw new InvalidOperationException("Email already in use");
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        user.PendingEmail = newEmail;
        user.PendingEmailCode = code;
        user.PendingEmailToken = token;
        user.PendingEmailExpiry = DateTimeOffset.UtcNow.AddHours(24);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to start email change: {errors}");
        }

        await _emailService.SendEmailChangeConfirmationAsync(userId, newEmail, token, code);

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.EmailChangeRequested,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = "Email change requested",
            Severity = SecurityEventSeverity.Info,
            Metadata = new Dictionary<string, string> { ["source"] = "Self" }
        });

        _logger.LogInformation("Email change requested for user {UserId}", userId);
    }

    public async Task ConfirmEmailChangeAsync(
        string? authenticatedUserId,
        ConfirmEmailChangeDto dto,
        string? ipAddress,
        string? userAgent)
    {
        AppUser? user = null;

        if (!string.IsNullOrWhiteSpace(dto.Token))
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.PendingEmailToken == dto.Token);
        }
        else if (!string.IsNullOrWhiteSpace(dto.Code) && !string.IsNullOrEmpty(authenticatedUserId))
        {
            user = await _userManager.FindByIdAsync(authenticatedUserId);
            if (user != null &&
                !string.Equals(user.PendingEmailCode, dto.Code.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid confirmation code");
            }
        }
        else
        {
            throw new InvalidOperationException("A confirmation token or code is required");
        }

        if (user == null)
        {
            throw new InvalidOperationException("Invalid or expired email change request");
        }

        if (string.IsNullOrEmpty(user.PendingEmail) ||
            user.PendingEmailExpiry == null ||
            user.PendingEmailExpiry < DateTimeOffset.UtcNow)
        {
            ClearPendingEmail(user);
            await _userManager.UpdateAsync(user);
            throw new InvalidOperationException("Email change request has expired. Please request a new change.");
        }

        var oldEmail = user.Email ?? "";
        var newEmail = user.PendingEmail;

        var existing = await _userManager.FindByEmailAsync(newEmail);
        if (existing != null && existing.Id != user.Id)
        {
            ClearPendingEmail(user);
            await _userManager.UpdateAsync(user);
            throw new InvalidOperationException("Email already in use");
        }

        var syncUserName = string.Equals(user.UserName, oldEmail, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(user.UserName);

        user.Email = newEmail;
        user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
        user.EmailConfirmed = true;
        if (syncUserName)
        {
            user.UserName = newEmail;
            user.NormalizedUserName = _userManager.NormalizeName(newEmail);
        }

        ClearPendingEmail(user);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to confirm email change: {errors}");
        }

        _context.UserEmailChangeLogs.Add(new UserEmailChangeLog
        {
            UserId = user.Id,
            OldEmail = oldEmail,
            NewEmail = newEmail,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = authenticatedUserId ?? user.Id,
            Source = "Self"
        });
        await _context.SaveChangesAsync();

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.EmailChanged,
            UserId = user.Id,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = "Email change confirmed",
            Severity = SecurityEventSeverity.Info,
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "Self"
            }
        });

        _logger.LogInformation("Email change confirmed for user {UserId}", user.Id);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for password change: {UserId}", userId);
            return false;
        }

        if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
        {
            throw new InvalidOperationException("New password and confirmation password do not match");
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to change password: {errors}");
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
        return true;
    }

    /// <summary>
    /// True when the account is Local-only (not multi-provider).
    /// </summary>
    public static bool IsLocalOnlyAccount(AppUser user)
    {
        if (string.IsNullOrWhiteSpace(user.AuthMethod))
        {
            return false;
        }

        var methods = user.AuthMethod
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return methods.Length == 1 &&
               methods[0].Equals("Local", StringComparison.OrdinalIgnoreCase) &&
               string.IsNullOrEmpty(user.GoogleId) &&
               string.IsNullOrEmpty(user.TelegramId);
    }

    private static void ClearPendingEmail(AppUser user)
    {
        user.PendingEmail = null;
        user.PendingEmailCode = null;
        user.PendingEmailToken = null;
        user.PendingEmailExpiry = null;
    }

    private static UserProfileDto MapProfile(AppUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        EmailConfirmed = user.EmailConfirmed,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        PendingEmail = user.PendingEmail,
        AuthMethod = user.AuthMethod,
        CanChangeEmail = IsLocalOnlyAccount(user)
    };
}
