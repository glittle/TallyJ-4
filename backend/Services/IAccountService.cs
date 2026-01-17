using TallyJ4.DTOs.Account;

namespace TallyJ4.Services;

public interface IAccountService
{
    Task<UserProfileDto?> GetUserProfileAsync(string userId);
    Task<UserProfileDto?> UpdateUserProfileAsync(string userId, UpdateUserProfileDto updateDto);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
}
