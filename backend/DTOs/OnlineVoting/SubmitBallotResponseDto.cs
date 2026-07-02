namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Response after submitting an online ballot.
/// </summary>
public class SubmitBallotResponseDto
{
    /// <summary>
    /// Human-readable confirmation message.
    /// </summary>
    public string Message { get; set; } = null!;
}