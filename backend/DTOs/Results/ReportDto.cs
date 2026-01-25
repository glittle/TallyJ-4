namespace TallyJ4.DTOs.Results;

public class ElectionReportDto
{
    public string ElectionName { get; set; } = string.Empty;
    public DateTime? ElectionDate { get; set; }
    public int NumToElect { get; set; }
    public int TotalBallots { get; set; }
    public int SpoiledBallots { get; set; }
    public int TotalVotes { get; set; }
    public List<CandidateReportDto> Elected { get; set; } = new();
    public List<CandidateReportDto> Extra { get; set; } = new();
    public List<CandidateReportDto> Other { get; set; } = new();
    public List<TieReportDto> Ties { get; set; } = new();
}

public class CandidateReportDto
{
    public int Rank { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public string Section { get; set; } = string.Empty;
}

public class TieReportDto
{
    public int TieBreakGroup { get; set; }
    public string Section { get; set; } = string.Empty;
    public List<string> CandidateNames { get; set; } = new();
}

public class BallotReportDto
{
    public Guid BallotGuid { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<VoteReportDto> Votes { get; set; } = new();
}

public class VoteReportDto
{
    public string FullName { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class VoterReportDto
{
    public Guid PersonGuid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool Voted { get; set; }
    public DateTime? VoteTime { get; set; }
}

public class LocationReportDto
{
    public string LocationName { get; set; } = string.Empty;
    public int TotalVoters { get; set; }
    public int Voted { get; set; }
    public int BallotsEntered { get; set; }
    public int TotalVotes { get; set; }
}

public class ReportDataResponseDto
{
    public string ReportType { get; set; } = string.Empty;
    public object Data { get; set; } = new object();
}