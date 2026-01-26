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

public class DetailedStatisticsDto
{
    public ElectionOverviewDto Overview { get; set; } = new();
    public VoteDistributionDto VoteDistribution { get; set; } = new();
    public CandidatePerformanceDto[] CandidatePerformance { get; set; } = Array.Empty<CandidatePerformanceDto>();
    public TurnoutAnalysisDto TurnoutAnalysis { get; set; } = new();
    public List<LocationStatisticsDto> LocationStatistics { get; set; } = new();
}

public class ElectionOverviewDto
{
    public string ElectionName { get; set; } = string.Empty;
    public DateTime? ElectionDate { get; set; }
    public int TotalRegisteredVoters { get; set; }
    public int TotalBallotsCast { get; set; }
    public int ValidBallots { get; set; }
    public int SpoiledBallots { get; set; }
    public int TotalVotes { get; set; }
    public int PositionsToElect { get; set; }
    public decimal OverallTurnoutPercentage { get; set; }
    public TimeSpan? ElectionDuration { get; set; }
}

public class VoteDistributionDto
{
    public int[] VotesPerPosition { get; set; } = Array.Empty<int>();
    public Dictionary<string, int> VoteCountDistribution { get; set; } = new();
    public double AverageVotesPerBallot { get; set; }
    public int MaxVotesOnSingleBallot { get; set; }
    public int MinVotesOnSingleBallot { get; set; }
    public Dictionary<int, int> BallotLengthDistribution { get; set; } = new();
}

public class CandidatePerformanceDto
{
    public Guid PersonGuid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalVotes { get; set; }
    public decimal VotePercentage { get; set; }
    public int Rank { get; set; }
    public bool IsElected { get; set; }
    public bool IsEliminated { get; set; }
    public Dictionary<int, int> VotesByPosition { get; set; } = new();
    public decimal FirstChoicePercentage { get; set; }
    public decimal LastChoicePercentage { get; set; }
}

public class TurnoutAnalysisDto
{
    public decimal OverallTurnout { get; set; }
    public Dictionary<string, decimal> TurnoutByLocation { get; set; } = new();
    public Dictionary<string, int> TurnoutTrends { get; set; } = new(); // Time-based turnout
    public int EarlyVotingCount { get; set; }
    public int ElectionDayVotingCount { get; set; }
    public decimal EarlyVotingPercentage { get; set; }
    public List<DemographicTurnoutDto> DemographicBreakdown { get; set; } = new();
    public List<TimeBasedTurnoutDto> TimeBasedTurnout { get; set; } = new();
    public ParticipationRateDto ParticipationRates { get; set; } = new();
}

public class DemographicTurnoutDto
{
    public string DemographicCategory { get; set; } = string.Empty; // AgeGroup, Area, etc.
    public string DemographicValue { get; set; } = string.Empty;
    public int TotalVoters { get; set; }
    public int Voted { get; set; }
    public decimal TurnoutPercentage { get; set; }
}

public class TimeBasedTurnoutDto
{
    public DateTime TimePeriod { get; set; }
    public string PeriodType { get; set; } = string.Empty; // Hour, Day, etc.
    public int BallotsCast { get; set; }
    public decimal CumulativeTurnout { get; set; }
}

public class ParticipationRateDto
{
    public decimal FirstTimeVoters { get; set; }
    public decimal ReturningVoters { get; set; }
    public decimal OnlineVoters { get; set; }
    public decimal InPersonVoters { get; set; }
    public Dictionary<string, decimal> ParticipationByMethod { get; set; } = new();
}

public class LocationStatisticsDto
{
    public string LocationName { get; set; } = string.Empty;
    public int RegisteredVoters { get; set; }
    public int BallotsCast { get; set; }
    public int ValidBallots { get; set; }
    public int SpoiledBallots { get; set; }
    public decimal TurnoutPercentage { get; set; }
    public int TotalVotes { get; set; }
    public Dictionary<string, int> TopCandidates { get; set; } = new();
}