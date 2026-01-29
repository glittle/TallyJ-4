namespace TallyJ4.DTOs.Results;

/// <summary>
/// Request DTO for comparing multiple elections
/// </summary>
public class ElectionComparisonRequestDto
{
    /// <summary>
    /// List of election GUIDs to compare.
    /// </summary>
    public List<Guid> ElectionIds { get; set; } = new();

    /// <summary>
    /// List of metrics to include in the comparison.
    /// </summary>
    public List<string> Metrics { get; set; } = new() { "turnout", "votes", "candidates" };
}