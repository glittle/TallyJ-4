namespace Backend.DTOs.Account;

/// <summary>
/// Request to update the authenticated user's display name.
/// </summary>
public class ChangeDisplayNameDto
{
    public string DisplayName { get; set; } = null!;
}
