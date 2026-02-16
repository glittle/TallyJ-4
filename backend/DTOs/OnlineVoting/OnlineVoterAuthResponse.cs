namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for online voter authentication response.
/// </summary>
public class OnlineVoterAuthResponse
{
    /// <summary>
    /// The authentication token for the voter.
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// The voter's unique identifier.
    /// </summary>
    public string VoterId { get; set; } = null!;

    /// <summary>
    /// The type of voter ID.
    /// </summary>
    public string VoterIdType { get; set; } = null!;

    /// <summary>
    /// The timestamp when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}



