namespace TallyJ4.DTOs.Results;

/// <summary>
/// Comprehensive statistical analysis of election data
/// </summary>
public class StatisticalAnalysisDto
{
    public ElectionOverviewDto Overview { get; set; } = new();
    public VotingPatternAnalysisDto VotingPatterns { get; set; } = new();
    public CandidateAnalysisDto CandidateAnalysis { get; set; } = new();
    public LocationAnalysisDto LocationAnalysis { get; set; } = new();
    public TimeBasedAnalysisDto TimeBasedAnalysis { get; set; } = new();
    public PredictiveMetricsDto PredictiveMetrics { get; set; } = new();
}

public class VotingPatternAnalysisDto
{
    public decimal AverageVotesPerBallot { get; set; }
    public Dictionary<int, int> VoteDistribution { get; set; } = new(); // Position -> Count
    public decimal StrategicVotingIndex { get; set; } // Measure of tactical voting
    public decimal BallotCompletenessRate { get; set; }
    public List<VotingPatternDto> Patterns { get; set; } = new();
}

public class VotingPatternDto
{
    public string PatternType { get; set; } = string.Empty; // "bullet", "full", "truncated"
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class CandidateAnalysisDto
{
    public decimal AverageVotePercentage { get; set; }
    public decimal VotePercentageVariance { get; set; }
    public List<CandidateClusterDto> Clusters { get; set; } = new();
    public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();
}

public class CandidateClusterDto
{
    public string ClusterName { get; set; } = string.Empty;
    public List<string> CandidateNames { get; set; } = new();
    public decimal AveragePerformance { get; set; }
}

public class LocationAnalysisDto
{
    public decimal TurnoutVariance { get; set; }
    public Dictionary<string, decimal> LocationCorrelations { get; set; } = new();
    public List<LocationClusterDto> Clusters { get; set; } = new();
}

public class LocationClusterDto
{
    public string ClusterName { get; set; } = string.Empty;
    public List<string> LocationNames { get; set; } = new();
    public decimal AverageTurnout { get; set; }
}

public class TimeBasedAnalysisDto
{
    public decimal VotingRateAcceleration { get; set; }
    public List<TimeSegmentDto> Segments { get; set; } = new();
    public Dictionary<string, decimal> PeakVotingHours { get; set; } = new();
}

public class TimeSegmentDto
{
    public string TimeRange { get; set; } = string.Empty;
    public int BallotsCast { get; set; }
    public decimal VotingRate { get; set; }
}

public class PredictiveMetricsDto
{
    public decimal ProjectedTurnout { get; set; }
    public List<PredictionDto> Predictions { get; set; } = new();
    public Dictionary<string, decimal> ConfidenceIntervals { get; set; } = new();
}

public class PredictionDto
{
    public string Metric { get; set; } = string.Empty;
    public decimal PredictedValue { get; set; }
    public decimal ConfidenceLevel { get; set; }
}