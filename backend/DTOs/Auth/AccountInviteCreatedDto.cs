namespace Backend.DTOs.Auth;

/// <summary>
/// Returned once when a SuperAdmin issues an invite. The raw token is not stored.
/// </summary>
public class AccountInviteCreatedDto
{
    public string Token { get; set; } = null!;

    public string InviteUrl { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
}
