namespace TallyJ4.DTOs.Account;

/// <summary>
/// Data transfer object for updating user profile information.
/// </summary>
public class UpdateUserProfileDto
{
    /// <summary>
    /// The new username for the user.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The new email address for the user.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The new phone number for the user.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
