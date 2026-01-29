namespace TallyJ4.DTOs.Account;

/// <summary>
/// Data transfer object for changing a user's password.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// The user's current password.
    /// </summary>
    public string CurrentPassword { get; set; } = null!;

    /// <summary>
    /// The new password to set.
    /// </summary>
    public string NewPassword { get; set; } = null!;

    /// <summary>
    /// Confirmation of the new password.
    /// </summary>
    public string ConfirmPassword { get; set; } = null!;
}
