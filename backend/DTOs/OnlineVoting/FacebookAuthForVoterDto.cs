using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for Facebook OAuth authentication for online voters.
/// </summary>
public class FacebookAuthForVoterDto
{
    /// <summary>
    /// The Facebook user access token obtained from the Facebook Login flow.
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
