namespace TallyJ4.DTOs.Results;

public class PresentationDto
{
    public string ElectionName { get; set; } = string.Empty;
    public DateTime? ElectionDate { get; set; }
    public int NumToElect { get; set; }
    public int TotalBallots { get; set; }
    public int TotalVotes { get; set; }
    public List<PresentationCandidateDto> ElectedCandidates { get; set; } = new();
    public List<PresentationCandidateDto> ExtraCandidates { get; set; } = new();
    public bool HasTies { get; set; }
    public List<PresentationTieDto> Ties { get; set; } = new();
    public string Status { get; set; } = "Final"; // "Preliminary", "Final", "In Progress"
}

public class PresentationCandidateDto
{
    public int Rank { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public bool IsTied { get; set; }
    public bool IsWinner { get; set; }
}

public class PresentationTieDto
{
    public int TieBreakGroup { get; set; }
    public string Section { get; set; } = string.Empty;
    public List<string> CandidateNames { get; set; } = new();
    public bool TieBreakRequired { get; set; }
}