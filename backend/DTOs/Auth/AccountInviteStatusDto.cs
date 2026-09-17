namespace Backend.DTOs.Auth;

/// <summary>
/// Public peek of a one-time account invite. Invalid, used, and expired tokens all return
/// <c>valid: false</c> so callers cannot distinguish those states.
/// </summary>
public class AccountInviteStatusDto
{
    public bool Valid { get; set; }
}
