namespace Backend.DTOs.SignalR;

/// <summary>
/// Data transfer object for ballot update notifications via SignalR.
/// </summary>
public class BallotUpdateDto
{
    /// <summary>
    /// The GUID of the election this ballot belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The GUID of the ballot being updated.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// The action performed on the ballot (e.g., "created", "updated", "deleted").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The code identifying the ballot.
    /// </summary>
    public string? BallotCode { get; set; }

    /// <summary>
    /// The status code of the ballot.
    /// </summary>
    public string? StatusCode { get; set; }

    /// <summary>
    /// The number of votes recorded on this ballot.
    /// </summary>
    public int? VoteCount { get; set; }

    /// <summary>
    /// The timestamp when the ballot was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}


