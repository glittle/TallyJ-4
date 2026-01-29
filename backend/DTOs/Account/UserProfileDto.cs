namespace TallyJ4.DTOs.Account;

/// <summary>
/// Data transfer object representing a user's profile information.
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// The username of the user.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Indicates whether the user's email address has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Indicates whether the user's phone number has been confirmed.
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }
}
