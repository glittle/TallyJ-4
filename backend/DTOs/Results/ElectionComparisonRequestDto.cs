namespace TallyJ4.DTOs.Results;

/// <summary>
/// Request DTO for comparing multiple elections
/// </summary>
public class ElectionComparisonRequestDto
{
    public List<Guid> ElectionIds { get; set; } = new();
    public List<string> Metrics { get; set; } = new() { "turnout", "votes", "candidates" };
}