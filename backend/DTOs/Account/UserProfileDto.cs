namespace Backend.DTOs.Account;

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
    /// The display name shown in the UI.
    /// </summary>
    public string? DisplayName { get; set; }

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

    /// <summary>
    /// New email awaiting confirmation (current Email remains active until confirmed).
    /// </summary>
    public string? PendingEmail { get; set; }

    /// <summary>
    /// Login methods for this account (e.g. Local).
    /// </summary>
    public string? AuthMethod { get; set; }

    /// <summary>
    /// Whether this user may start a self-service email change (Local-only).
    /// </summary>
    public bool CanChangeEmail { get; set; }
}



