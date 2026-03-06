using Backend.Domain.Enumerations;

namespace Backend.DTOs.Ballots;

/// <summary>
/// Data transfer object for updating ballot information.
/// </summary>
public class UpdateBallotDto
{
    /// <summary>
    /// The status of the ballot.
    /// </summary>
    public BallotStatus StatusCode { get; set; }

    /// <summary>
    /// The name of the first teller who processed the ballot.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller who processed the ballot.
    /// </summary>
    public string? Teller2 { get; set; }
}



