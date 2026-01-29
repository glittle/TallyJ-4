namespace TallyJ4.DTOs.Results;

/// <summary>
/// Data structure for comparing multiple elections
/// </summary>
public class ElectionComparisonDto
{
    /// <summary>
    /// List of elections being compared.
    /// </summary>
    public List<ElectionSummaryDto> Elections { get; set; } = new();

    /// <summary>
    /// Aggregated metrics across all compared elections.
    /// </summary>
    public ComparisonMetricsDto Metrics { get; set; } = new();

    /// <summary>
    /// Trend data showing changes over time across elections.
    /// </summary>
    public List<TrendDataDto> Trends { get; set; } = new();
}

/// <summary>
/// Summary information for a single election.
/// </summary>
public class ElectionSummaryDto
{
    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;

    /// <summary>
    /// The date when the election was held.
    /// </summary>
    public DateTime? ElectionDate { get; set; }

    /// <summary>
    /// The total number of registered voters.
    /// </summary>
    public int TotalRegisteredVoters { get; set; }

    /// <summary>
    /// The total number of ballots cast.
    /// </summary>
    public int TotalBallotsCast { get; set; }

    /// <summary>
    /// The turnout percentage (ballots cast / registered voters).
    /// </summary>
    public decimal TurnoutPercentage { get; set; }

    /// <summary>
    /// The total number of votes cast across all positions.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int PositionsToElect { get; set; }

    /// <summary>
    /// The number of candidates who were elected.
    /// </summary>
    public int ElectedCount { get; set; }
}

/// <summary>
/// Aggregated metrics for comparing multiple elections.
/// </summary>
public class ComparisonMetricsDto
{
    /// <summary>
    /// The average turnout percentage across all elections.
    /// </summary>
    public decimal AverageTurnout { get; set; }

    /// <summary>
    /// The percentage change in turnout from the previous period.
    /// </summary>
    public decimal TurnoutChange { get; set; } // Percentage change from previous

    /// <summary>
    /// The total number of elections being compared.
    /// </summary>
    public int TotalElections { get; set; }

    /// <summary>
    /// Average values for various metrics across all elections.
    /// </summary>
    public Dictionary<string, decimal> MetricAverages { get; set; } = new();
}

/// <summary>
/// Trend data for a specific metric across multiple elections.
/// </summary>
public class TrendDataDto
{
    /// <summary>
    /// The metric being tracked (turnout, votes, etc.).
    /// </summary>
    public string Metric { get; set; } = string.Empty; // "turnout", "votes", etc.

    /// <summary>
    /// Data points for this metric over time.
    /// </summary>
    public List<TrendPointDto> Points { get; set; } = new();
}

/// <summary>
/// A single data point in a trend series.
/// </summary>
public class TrendPointDto
{
    /// <summary>
    /// The date of this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The value of the metric at this point in time.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// The name of the election this point represents.
    /// </summary>
    public string ElectionName { get; set; } = string.Empty;
}