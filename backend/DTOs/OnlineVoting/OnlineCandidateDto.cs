namespace TallyJ4.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object representing a candidate in an online election.
/// </summary>
public class OnlineCandidateDto
{
    /// <summary>
    /// The unique identifier of the person/candidate.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// The geographical area of the candidate.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Additional information about the candidate.
    /// </summary>
    public string? OtherInfo { get; set; }
}
