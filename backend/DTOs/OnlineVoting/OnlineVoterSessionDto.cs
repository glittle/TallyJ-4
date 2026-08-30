namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Identity for an established online-voter cookie session (no JWT in the body).
/// </summary>
public class OnlineVoterSessionDto
{
    /// <summary>
    /// The voter's unique identifier (email, phone, or kiosk code).
    /// </summary>
    public string VoterId { get; set; } = null!;

    /// <summary>
    /// The type of voter ID (E / P / C).
    /// </summary>
    public string VoterIdType { get; set; } = null!;
}
