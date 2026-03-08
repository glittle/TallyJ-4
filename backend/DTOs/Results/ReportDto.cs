using Backend.Domain.Enumerations;

namespace Backend.DTOs.Results;

/// <summary>
/// Data transfer object containing comprehensive election report data.
/// </summary>
public class ElectionReportDto
{
    /// <summary>
    /// Name of the election.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;

    /// <summary>
    /// Date when the election was held.
    /// </summary>
    public DateTime? ElectionDate { get; set; }

    /// <summary>
    /// Number of candidates to be elected.
    /// </summary>
    public int NumToElect { get; set; }

    /// <summary>
    /// Total number of ballots cast in the election.
    /// </summary>
    public int TotalBallots { get; set; }

    /// <summary>
    /// Number of spoiled or invalid ballots.
    /// </summary>
    public int SpoiledBallots { get; set; }

    /// <summary>
    /// Total number of votes recorded across all ballots.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// List of candidates who have been elected.
    /// </summary>
    public List<CandidateReportDto> Elected { get; set; } = new();

    /// <summary>
    /// List of additional candidates who received votes but were not elected.
    /// </summary>
    public List<CandidateReportDto> Extra { get; set; } = new();

    /// <summary>
    /// List of other candidates who participated but received no votes.
    /// </summary>
    public List<CandidateReportDto> Other { get; set; } = new();

    /// <summary>
    /// List of tie situations in the election results.
    /// </summary>
    public List<TieReportDto> Ties { get; set; } = new();
}

/// <summary>
/// Data transfer object containing information about a candidate in election reports.
/// </summary>
public class CandidateReportDto
{
    /// <summary>
    /// The ranking position of this candidate in the election results.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Number of votes received by this candidate.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Section or category this candidate belongs to.
    /// </summary>
    public string Section { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object containing information about a tie situation in election reports.
/// </summary>
public class TieReportDto
{
    /// <summary>
    /// Group identifier for this tie situation.
    /// </summary>
    public int TieBreakGroup { get; set; }

    /// <summary>
    /// Section or category where the tie occurred.
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// List of candidate names involved in this tie.
    /// </summary>
    public List<string> CandidateNames { get; set; } = new();
}

/// <summary>
/// Data transfer object containing information about a ballot in election reports.
/// </summary>
public class BallotReportDto
{
    /// <summary>
    /// Unique identifier of the ballot.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// Name of the location where this ballot was cast.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the ballot.
    /// </summary>
    public BallotStatus Status { get; set; }

    /// <summary>
    /// List of votes recorded on this ballot.
    /// </summary>
    public List<VoteReportDto> Votes { get; set; } = new();
}

/// <summary>
/// Data transfer object containing information about a single vote in election reports.
/// </summary>
public class VoteReportDto
{
    /// <summary>
    /// Full name of the candidate who received this vote.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Position/order of this vote on the ballot.
    /// </summary>
    public int Position { get; set; }
}

/// <summary>
/// Data transfer object containing information about a voter in election reports.
/// </summary>
public class VoterReportDto
{
    /// <summary>
    /// Unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// Full name of the voter.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the location where this voter is registered.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this voter has cast their ballot.
    /// </summary>
    public bool Voted { get; set; }

    /// <summary>
    /// Timestamp when this voter cast their ballot.
    /// </summary>
    public DateTime? VoteTime { get; set; }
}

/// <summary>
/// Data transfer object containing statistics for a specific voting location.
/// </summary>
public class LocationReportDto
{
    /// <summary>
    /// Name of the voting location.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of registered voters at this location.
    /// </summary>
    public int TotalVoters { get; set; }

    /// <summary>
    /// Number of voters who have cast their ballots.
    /// </summary>
    public int Voted { get; set; }

    /// <summary>
    /// Number of ballots that have been entered into the system.
    /// </summary>
    public int BallotsEntered { get; set; }

    /// <summary>
    /// Total number of votes recorded at this location.
    /// </summary>
    public int TotalVotes { get; set; }
}

/// <summary>
/// Data transfer object containing report data response.
/// </summary>
public class ReportDataResponseDto
{
    /// <summary>
    /// Type of report being returned.
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// The actual report data.
    /// </summary>
    public object Data { get; set; } = new object();
}

/// <summary>
/// Data transfer object containing detailed statistical analysis of election data.
/// </summary>
public class DetailedStatisticsDto
{
    /// <summary>
    /// Overview statistics for the election.
    /// </summary>
    public ElectionOverviewDto Overview { get; set; } = new();

    /// <summary>
    /// Analysis of how votes are distributed across ballots.
    /// </summary>
    public VoteDistributionDto VoteDistribution { get; set; } = new();

    /// <summary>
    /// Performance metrics for each candidate.
    /// </summary>
    public CandidatePerformanceDto[] CandidatePerformance { get; set; } = Array.Empty<CandidatePerformanceDto>();

    /// <summary>
    /// Analysis of voter turnout patterns.
    /// </summary>
    public TurnoutAnalysisDto TurnoutAnalysis { get; set; } = new();

    /// <summary>
    /// Statistics broken down by voting location.
    /// </summary>
    public List<LocationStatisticsDto> LocationStatistics { get; set; } = new();
}

/// <summary>
/// Data transfer object containing overview statistics for an election.
/// </summary>
public class ElectionOverviewDto
{
    /// <summary>
    /// Name of the election.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;

    /// <summary>
    /// Date when the election was held.
    /// </summary>
    public DateTime? ElectionDate { get; set; }

    /// <summary>
    /// Total number of registered voters.
    /// </summary>
    public int TotalRegisteredVoters { get; set; }

    /// <summary>
    /// Total number of ballots cast.
    /// </summary>
    public int TotalBallotsCast { get; set; }

    /// <summary>
    /// Number of valid ballots.
    /// </summary>
    public int ValidBallots { get; set; }

    /// <summary>
    /// Number of spoiled or invalid ballots.
    /// </summary>
    public int SpoiledBallots { get; set; }

    /// <summary>
    /// Total number of votes recorded.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Number of positions to be elected.
    /// </summary>
    public int PositionsToElect { get; set; }

    /// <summary>
    /// Overall voter turnout percentage.
    /// </summary>
    public decimal OverallTurnoutPercentage { get; set; }

    /// <summary>
    /// Duration of the election period.
    /// </summary>
    public TimeSpan? ElectionDuration { get; set; }
}

/// <summary>
/// Data transfer object containing analysis of vote distribution patterns.
/// </summary>
public class VoteDistributionDto
{
    /// <summary>
    /// Array showing number of votes cast for each position on ballots.
    /// </summary>
    public int[] VotesPerPosition { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Distribution of vote counts across different values.
    /// </summary>
    public Dictionary<string, int> VoteCountDistribution { get; set; } = new();

    /// <summary>
    /// Average number of votes per ballot.
    /// </summary>
    public double AverageVotesPerBallot { get; set; }

    /// <summary>
    /// Maximum number of votes on any single ballot.
    /// </summary>
    public int MaxVotesOnSingleBallot { get; set; }

    /// <summary>
    /// Minimum number of votes on any single ballot.
    /// </summary>
    public int MinVotesOnSingleBallot { get; set; }

    /// <summary>
    /// Distribution of ballot lengths (number of votes per ballot).
    /// </summary>
    public Dictionary<int, int> BallotLengthDistribution { get; set; } = new();
}

/// <summary>
/// Data transfer object containing performance metrics for a candidate.
/// </summary>
public class CandidatePerformanceDto
{
    /// <summary>
    /// Unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// Full name of the candidate.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of votes received by this candidate.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Percentage of total votes received by this candidate.
    /// </summary>
    public decimal VotePercentage { get; set; }

    /// <summary>
    /// Final ranking position of this candidate.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Indicates whether this candidate has been elected.
    /// </summary>
    public bool IsElected { get; set; }

    /// <summary>
    /// Indicates whether this candidate has been eliminated from the race.
    /// </summary>
    public bool IsEliminated { get; set; }

    /// <summary>
    /// Breakdown of votes received by position on ballots.
    /// </summary>
    public Dictionary<int, int> VotesByPosition { get; set; } = new();

    /// <summary>
    /// Percentage of ballots where this candidate was the first choice.
    /// </summary>
    public decimal FirstChoicePercentage { get; set; }

    /// <summary>
    /// Percentage of ballots where this candidate was the last choice.
    /// </summary>
    public decimal LastChoicePercentage { get; set; }
}

/// <summary>
/// Data transfer object containing analysis of voter turnout patterns.
/// </summary>
public class TurnoutAnalysisDto
{
    /// <summary>
    /// Overall voter turnout percentage for the election.
    /// </summary>
    public decimal OverallTurnout { get; set; }

    /// <summary>
    /// Turnout percentages broken down by voting location.
    /// </summary>
    public Dictionary<string, decimal> TurnoutByLocation { get; set; } = new();

    /// <summary>
    /// Time-based turnout trends and patterns.
    /// </summary>
    public Dictionary<string, int> TurnoutTrends { get; set; } = new();

    /// <summary>
    /// Number of ballots cast during early voting period.
    /// </summary>
    public int EarlyVotingCount { get; set; }

    /// <summary>
    /// Number of ballots cast on election day.
    /// </summary>
    public int ElectionDayVotingCount { get; set; }

    /// <summary>
    /// Percentage of total votes that were cast during early voting.
    /// </summary>
    public decimal EarlyVotingPercentage { get; set; }

    /// <summary>
    /// Turnout breakdown by demographic categories.
    /// </summary>
    public List<DemographicTurnoutDto> DemographicBreakdown { get; set; } = new();

    /// <summary>
    /// Time-based turnout analysis.
    /// </summary>
    public List<TimeBasedTurnoutDto> TimeBasedTurnout { get; set; } = new();

    /// <summary>
    /// Participation rates by different voting methods.
    /// </summary>
    public ParticipationRateDto ParticipationRates { get; set; } = new();
}

/// <summary>
/// Data transfer object containing turnout statistics for a demographic group.
/// </summary>
public class DemographicTurnoutDto
{
    /// <summary>
    /// Category of demographic data (AgeGroup, Area, etc.).
    /// </summary>
    public string DemographicCategory { get; set; } = string.Empty;

    /// <summary>
    /// Specific value within the demographic category.
    /// </summary>
    public string DemographicValue { get; set; } = string.Empty;

    /// <summary>
    /// Total number of voters in this demographic group.
    /// </summary>
    public int TotalVoters { get; set; }

    /// <summary>
    /// Number of voters in this group who cast ballots.
    /// </summary>
    public int Voted { get; set; }

    /// <summary>
    /// Turnout percentage for this demographic group.
    /// </summary>
    public decimal TurnoutPercentage { get; set; }
}

/// <summary>
/// Data transfer object containing time-based turnout statistics.
/// </summary>
public class TimeBasedTurnoutDto
{
    /// <summary>
    /// The time period for this turnout data.
    /// </summary>
    public DateTime TimePeriod { get; set; }

    /// <summary>
    /// Type of time period (Hour, Day, etc.).
    /// </summary>
    public string PeriodType { get; set; } = string.Empty;

    /// <summary>
    /// Number of ballots cast during this time period.
    /// </summary>
    public int BallotsCast { get; set; }

    /// <summary>
    /// Cumulative turnout percentage up to this time period.
    /// </summary>
    public decimal CumulativeTurnout { get; set; }
}

/// <summary>
/// Data transfer object containing participation rates by different voting methods.
/// </summary>
public class ParticipationRateDto
{
    /// <summary>
    /// Percentage of first-time voters.
    /// </summary>
    public decimal FirstTimeVoters { get; set; }

    /// <summary>
    /// Percentage of returning voters.
    /// </summary>
    public decimal ReturningVoters { get; set; }

    /// <summary>
    /// Percentage of voters who voted online.
    /// </summary>
    public decimal OnlineVoters { get; set; }

    /// <summary>
    /// Percentage of voters who voted in person.
    /// </summary>
    public decimal InPersonVoters { get; set; }

    /// <summary>
    /// Participation rates broken down by voting method.
    /// </summary>
    public Dictionary<string, decimal> ParticipationByMethod { get; set; } = new();
}

/// <summary>
/// Data transfer object containing statistics for a specific voting location.
/// </summary>
public class LocationStatisticsDto
{
    /// <summary>
    /// Name of the voting location.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Number of registered voters at this location.
    /// </summary>
    public int RegisteredVoters { get; set; }

    /// <summary>
    /// Number of ballots cast at this location.
    /// </summary>
    public int BallotsCast { get; set; }

    /// <summary>
    /// Number of valid ballots at this location.
    /// </summary>
    public int ValidBallots { get; set; }

    /// <summary>
    /// Number of spoiled ballots at this location.
    /// </summary>
    public int SpoiledBallots { get; set; }

    /// <summary>
    /// Turnout percentage at this location.
    /// </summary>
    public decimal TurnoutPercentage { get; set; }

    /// <summary>
    /// Total number of votes recorded at this location.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Top candidates and their vote counts at this location.
    /// </summary>
    public Dictionary<string, int> TopCandidates { get; set; } = new();
}


