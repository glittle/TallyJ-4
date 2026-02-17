namespace Backend.DTOs.Eligibility;

/// <summary>
/// Data transfer object representing an eligibility reason.
/// </summary>
public class EligibilityReasonDto
{
    /// <summary>
    /// The unique GUID identifier for this reason.
    /// </summary>
    public Guid ReasonGuid { get; set; }

    /// <summary>
    /// The short code identifier (e.g., "X01", "V01").
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// The human-readable description of the reason.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Whether a person with this reason can vote.
    /// </summary>
    public bool CanVote { get; set; }

    /// <summary>
    /// Whether a person with this reason can receive votes (be a candidate).
    /// </summary>
    public bool CanReceiveVotes { get; set; }

    /// <summary>
    /// Whether this reason is for internal use only (not for person forms).
    /// </summary>
    public bool InternalOnly { get; set; }
}