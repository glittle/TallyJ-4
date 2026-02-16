using Backend.DTOs.Account;

namespace Backend.Services;

/// <summary>
/// Service interface for managing user account operations.
/// Provides methods for retrieving and updating user profiles and changing passwords.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gets the user profile for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The user profile data, or null if not found.</returns>
    Task<UserProfileDto?> GetUserProfileAsync(string userId);

    /// <summary>
    /// Updates the user profile for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="updateDto">The updated profile data.</param>
    /// <returns>The updated user profile data, or null if the user was not found.</returns>
    Task<UserProfileDto?> UpdateUserProfileAsync(string userId, UpdateUserProfileDto updateDto);

    /// <summary>
    /// Changes the password for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="changePasswordDto">The password change data including current and new passwords.</param>
    /// <returns>True if the password was changed successfully, false otherwise.</returns>
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
}



