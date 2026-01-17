using Microsoft.AspNetCore.Identity;
using TallyJ4.DTOs.Account;
using TallyJ4.EF.Identity;

namespace TallyJ4.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountService> _logger;

    public AccountService(UserManager<AppUser> userManager, ILogger<AccountService> logger)
    {
        _userManager = userManager;
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

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed
        };
    }

    public async Task<UserProfileDto?> UpdateUserProfileAsync(string userId, UpdateUserProfileDto updateDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", userId);
            return null;
        }

        var needsUpdate = false;

        if (!string.IsNullOrWhiteSpace(updateDto.UserName) && updateDto.UserName != user.UserName)
        {
            var existingUser = await _userManager.FindByNameAsync(updateDto.UserName);
            if (existingUser != null && existingUser.Id != userId)
            {
                _logger.LogWarning("Username already taken: {UserName}", updateDto.UserName);
                throw new InvalidOperationException("Username already taken");
            }
            user.UserName = updateDto.UserName;
            needsUpdate = true;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(updateDto.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                _logger.LogWarning("Email already in use: {Email}", updateDto.Email);
                throw new InvalidOperationException("Email already in use");
            }
            user.Email = updateDto.Email;
            user.EmailConfirmed = false;
            needsUpdate = true;
        }

        if (updateDto.PhoneNumber != user.PhoneNumber)
        {
            user.PhoneNumber = updateDto.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            {
                user.PhoneNumberConfirmed = false;
            }
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update user profile: {Errors}", errors);
                throw new InvalidOperationException($"Failed to update profile: {errors}");
            }

            _logger.LogInformation("User profile updated: {UserId}", userId);
        }

        return await GetUserProfileAsync(userId);
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
            _logger.LogWarning("Password confirmation mismatch for user: {UserId}", userId);
            throw new InvalidOperationException("New password and confirmation password do not match");
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, errors);
            throw new InvalidOperationException($"Failed to change password: {errors}");
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
        return true;
    }
}
