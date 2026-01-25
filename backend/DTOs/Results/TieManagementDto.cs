namespace TallyJ4.DTOs.Results;

public class TieDetailsDto
{
    public int TieBreakGroup { get; set; }
    public string Section { get; set; } = string.Empty;
    public List<TieCandidateDto> Candidates { get; set; } = new();
    public string Instructions { get; set; } = "Enter tie-break vote counts for these candidates";
}

public class TieCandidateDto
{
    public Guid PersonGuid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public int? TieBreakCount { get; set; }
}

public class SaveTieCountsRequestDto
{
    public List<TieCountDto> Counts { get; set; } = new();
}

public class TieCountDto
{
    public Guid PersonGuid { get; set; }
    public int TieBreakCount { get; set; }
}

public class SaveTieCountsResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ReAnalysisTriggered { get; set; }
}