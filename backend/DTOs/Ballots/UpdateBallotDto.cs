namespace Backend.DTOs.Ballots;

/// <summary>
/// Data transfer object for updating ballot information.
/// </summary>
public class UpdateBallotDto
{
    /// <summary>
    /// The status code of the ballot.
    /// </summary>
    public string StatusCode { get; set; } = null!;

    /// <summary>
    /// The name of the first teller who processed the ballot.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller who processed the ballot.
    /// </summary>
    public string? Teller2 { get; set; }
}



