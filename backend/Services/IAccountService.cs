using Backend.DTOs.Account;

namespace Backend.Services;

/// <summary>
/// Service interface for managing user account operations.
/// </summary>
public interface IAccountService
{
    Task<UserProfileDto?> GetUserProfileAsync(string userId);

    Task<UserProfileDto?> ChangeDisplayNameAsync(string userId, string displayName);

    /// <summary>
    /// Starts a Local-only email change: validates password, stores pending email, emails confirmation.
    /// </summary>
    Task RequestEmailChangeAsync(string userId, RequestEmailChangeDto dto, string? ipAddress, string? userAgent);

    /// <summary>
    /// Confirms a pending email change via link token and/or short code.
    /// </summary>
    Task ConfirmEmailChangeAsync(string? authenticatedUserId, ConfirmEmailChangeDto dto, string? ipAddress, string? userAgent);

    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
}
