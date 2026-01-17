namespace TallyJ4.DTOs.Results;

public class TieInfoDto
{
    public int TieBreakGroup { get; set; }
    public int VoteCount { get; set; }
    public bool TieBreakRequired { get; set; }
    public string Section { get; set; } = string.Empty;
    public List<string> CandidateNames { get; set; } = new();
}
