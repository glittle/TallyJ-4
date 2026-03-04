using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for Kakao OAuth authentication for online voters.
/// </summary>
public class KakaoAuthForVoterDto
{
    /// <summary>
    /// The Kakao access token obtained from the Kakao Login flow.
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
