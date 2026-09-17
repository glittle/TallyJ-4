using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

/// <summary>
/// Anonymous create of one local email/password account, gated by a SuperAdmin one-time invite.
/// </summary>
public class RegisterWithInviteRequest : RegisterRequest
{
    [Required]
    public string Token { get; set; } = null!;
}
