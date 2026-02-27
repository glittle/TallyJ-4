using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for Google OAuth authentication for online voters.
/// </summary>
public class GoogleAuthForVoterDto
{
    /// <summary>
    /// The Google credential (JWT token) from Google One Tap or Sign-In.
    /// </summary>
    [Required]
    public string Credential { get; set; } = null!;
}
