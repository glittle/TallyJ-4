namespace TallyJ4.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for verifying a voter's code for online voting.
/// </summary>
public class VerifyCodeDto
{
    /// <summary>
    /// The voter's unique identifier.
    /// </summary>
    public string VoterId { get; set; } = null!;
    
    /// <summary>
    /// The verification code to validate.
    /// </summary>
    public string VerifyCode { get; set; } = null!;
}
