namespace Backend.DTOs.Account;

/// <summary>
/// Request to begin changing the authenticated Local user's email address.
/// </summary>
public class RequestEmailChangeDto
{
    /// <summary>
    /// The desired new email address (must be confirmed before it becomes active).
    /// </summary>
    public string NewEmail { get; set; } = null!;

    /// <summary>
    /// Current password for the Local account.
    /// </summary>
    public string CurrentPassword { get; set; } = null!;
}
