namespace TallyJ4.DTOs.Results;

public class TallyResultDto
{
    public Guid ElectionGuid { get; set; }
    public string ElectionName { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public TallyStatisticsDto Statistics { get; set; } = new();
    public List<CandidateResultDto> Results { get; set; } = new();
    public List<TieInfoDto> Ties { get; set; } = new();
}

public class CandidateResultDto
{
    public Guid PersonGuid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public int Rank { get; set; }
    public string Section { get; set; } = string.Empty;
    public bool IsTied { get; set; }
    public int? TieBreakGroup { get; set; }
    public bool TieBreakRequired { get; set; }
    public bool CloseToNext { get; set; }
    public bool CloseToPrev { get; set; }
}
