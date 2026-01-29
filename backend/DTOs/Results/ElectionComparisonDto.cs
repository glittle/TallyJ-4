namespace TallyJ4.DTOs.Results;

/// <summary>
/// Data structure for comparing multiple elections
/// </summary>
public class ElectionComparisonDto
{
    public List<ElectionSummaryDto> Elections { get; set; } = new();
    public ComparisonMetricsDto Metrics { get; set; } = new();
    public List<TrendDataDto> Trends { get; set; } = new();
}

public class ElectionSummaryDto
{
    public Guid ElectionGuid { get; set; }
    public string ElectionName { get; set; } = string.Empty;
    public DateTime? ElectionDate { get; set; }
    public int TotalRegisteredVoters { get; set; }
    public int TotalBallotsCast { get; set; }
    public decimal TurnoutPercentage { get; set; }
    public int TotalVotes { get; set; }
    public int PositionsToElect { get; set; }
    public int ElectedCount { get; set; }
}

public class ComparisonMetricsDto
{
    public decimal AverageTurnout { get; set; }
    public decimal TurnoutChange { get; set; } // Percentage change from previous
    public int TotalElections { get; set; }
    public Dictionary<string, decimal> MetricAverages { get; set; } = new();
}

public class TrendDataDto
{
    public string Metric { get; set; } = string.Empty; // "turnout", "votes", etc.
    public List<TrendPointDto> Points { get; set; } = new();
}

public class TrendPointDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string ElectionName { get; set; } = string.Empty;
}