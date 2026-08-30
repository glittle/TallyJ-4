namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object for online voter authentication response.
/// </summary>
public class OnlineVoterAuthResponse
{
    /// <summary>
    /// Deprecated: the JWT is issued in the httpOnly <c>voter_token</c> cookie.
    /// Left optional so older clients do not break; new responses omit it.
    /// </summary>
    public string? Token { get; set; }

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
    public DateTimeOffset ExpiresAt { get; set; }
}



