namespace Backend.DTOs.Results;

/// <summary>
/// Comprehensive statistical analysis of election data
/// </summary>
public class StatisticalAnalysisDto
{
    /// <summary>
    /// Overview statistics for the election.
    /// </summary>
    public ElectionOverviewDto Overview { get; set; } = new();

    /// <summary>
    /// Analysis of voting patterns and behaviors.
    /// </summary>
    public VotingPatternAnalysisDto VotingPatterns { get; set; } = new();

    /// <summary>
    /// Analysis of candidate performance and clustering.
    /// </summary>
    public CandidateAnalysisDto CandidateAnalysis { get; set; } = new();

    /// <summary>
    /// Analysis of voting patterns by location.
    /// </summary>
    public LocationAnalysisDto LocationAnalysis { get; set; } = new();

    /// <summary>
    /// Time-based analysis of voting patterns.
    /// </summary>
    public TimeBasedAnalysisDto TimeBasedAnalysis { get; set; } = new();

    /// <summary>
    /// Predictive metrics and forecasting data.
    /// </summary>
    public PredictiveMetricsDto PredictiveMetrics { get; set; } = new();
}

/// <summary>
/// Analysis of voting patterns and ballot completion.
/// </summary>
public class VotingPatternAnalysisDto
{
    /// <summary>
    /// The average number of votes cast per ballot.
    /// </summary>
    public decimal AverageVotesPerBallot { get; set; }

    /// <summary>
    /// Distribution of votes by position (position number -> vote count).
    /// </summary>
    public Dictionary<int, int> VoteDistribution { get; set; } = new(); // Position -> Count

    /// <summary>
    /// Index measuring the level of strategic/tactical voting.
    /// </summary>
    public decimal StrategicVotingIndex { get; set; } // Measure of tactical voting

    /// <summary>
    /// The rate at which ballots are fully completed.
    /// </summary>
    public decimal BallotCompletenessRate { get; set; }

    /// <summary>
    /// List of identified voting patterns.
    /// </summary>
    public List<VotingPatternDto> Patterns { get; set; } = new();
}

/// <summary>
/// Represents a specific voting pattern identified in the analysis.
/// </summary>
public class VotingPatternDto
{
    /// <summary>
    /// The type of voting pattern (bullet, full, truncated).
    /// </summary>
    public string PatternType { get; set; } = string.Empty; // "bullet", "full", "truncated"

    /// <summary>
    /// The number of ballots exhibiting this pattern.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The percentage of total ballots exhibiting this pattern.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Analysis of candidate performance and clustering.
/// </summary>
public class CandidateAnalysisDto
{
    /// <summary>
    /// The average vote percentage across all candidates.
    /// </summary>
    public decimal AverageVotePercentage { get; set; }

    /// <summary>
    /// The variance in vote percentages among candidates.
    /// </summary>
    public decimal VotePercentageVariance { get; set; }

    /// <summary>
    /// Clusters of candidates with similar performance characteristics.
    /// </summary>
    public List<CandidateClusterDto> Clusters { get; set; } = new();

    /// <summary>
    /// Various performance metrics for candidates.
    /// </summary>
    public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// A cluster of candidates with similar performance characteristics.
/// </summary>
public class CandidateClusterDto
{
    /// <summary>
    /// The name or identifier of this cluster.
    /// </summary>
    public string ClusterName { get; set; } = string.Empty;

    /// <summary>
    /// List of candidate names in this cluster.
    /// </summary>
    public List<string> CandidateNames { get; set; } = new();

    /// <summary>
    /// The average performance metric for candidates in this cluster.
    /// </summary>
    public decimal AveragePerformance { get; set; }
}

/// <summary>
/// Analysis of voting patterns by geographic location.
/// </summary>
public class LocationAnalysisDto
{
    /// <summary>
    /// The variance in turnout rates across different locations.
    /// </summary>
    public decimal TurnoutVariance { get; set; }

    /// <summary>
    /// Correlations between location characteristics and voting behavior.
    /// </summary>
    public Dictionary<string, decimal> LocationCorrelations { get; set; } = new();

    /// <summary>
    /// Clusters of locations with similar voting characteristics.
    /// </summary>
    public List<LocationClusterDto> Clusters { get; set; } = new();
}

/// <summary>
/// A cluster of locations with similar voting characteristics.
/// </summary>
public class LocationClusterDto
{
    /// <summary>
    /// The name or identifier of this location cluster.
    /// </summary>
    public string ClusterName { get; set; } = string.Empty;

    /// <summary>
    /// List of location names in this cluster.
    /// </summary>
    public List<string> LocationNames { get; set; } = new();

    /// <summary>
    /// The average turnout rate for locations in this cluster.
    /// </summary>
    public decimal AverageTurnout { get; set; }
}

/// <summary>
/// Time-based analysis of voting patterns and rates.
/// </summary>
public class TimeBasedAnalysisDto
{
    /// <summary>
    /// The acceleration rate of voting over time.
    /// </summary>
    public decimal VotingRateAcceleration { get; set; }

    /// <summary>
    /// Voting data segmented by time periods.
    /// </summary>
    public List<TimeSegmentDto> Segments { get; set; } = new();

    /// <summary>
    /// Peak voting hours with their voting rates.
    /// </summary>
    public Dictionary<string, decimal> PeakVotingHours { get; set; } = new();
}

/// <summary>
/// A time segment with voting statistics.
/// </summary>
public class TimeSegmentDto
{
    /// <summary>
    /// The time range represented by this segment.
    /// </summary>
    public string TimeRange { get; set; } = string.Empty;

    /// <summary>
    /// The number of ballots cast during this time segment.
    /// </summary>
    public int BallotsCast { get; set; }

    /// <summary>
    /// The voting rate during this time segment.
    /// </summary>
    public decimal VotingRate { get; set; }
}

/// <summary>
/// Predictive metrics and forecasting data for the election.
/// </summary>
public class PredictiveMetricsDto
{
    /// <summary>
    /// The projected turnout rate based on current trends.
    /// </summary>
    public decimal ProjectedTurnout { get; set; }

    /// <summary>
    /// List of predictions for various election metrics.
    /// </summary>
    public List<PredictionDto> Predictions { get; set; } = new();

    /// <summary>
    /// Confidence intervals for the predictions.
    /// </summary>
    public Dictionary<string, decimal> ConfidenceIntervals { get; set; } = new();
}

/// <summary>
/// A prediction for a specific election metric.
/// </summary>
public class PredictionDto
{
    /// <summary>
    /// The metric being predicted.
    /// </summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// The predicted value for the metric.
    /// </summary>
    public decimal PredictedValue { get; set; }

    /// <summary>
    /// The confidence level of the prediction (0-1).
    /// </summary>
    public decimal ConfidenceLevel { get; set; }
}


