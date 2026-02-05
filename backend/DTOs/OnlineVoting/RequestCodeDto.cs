namespace TallyJ4.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for requesting a verification code for online voting.
/// </summary>
public class RequestCodeDto
{
    /// <summary>
    /// The voter's unique identifier (email, phone, or custom code).
    /// </summary>
    public string VoterId { get; set; } = null!;
    
    /// <summary>
    /// The type of voter ID: 'E' (email), 'P' (phone), or 'C' (code).
    /// </summary>
    public string VoterIdType { get; set; } = null!;
    
    /// <summary>
    /// The delivery method for the verification code: 'email', 'sms', or 'voice'.
    /// </summary>
    public string DeliveryMethod { get; set; } = null!;
}
