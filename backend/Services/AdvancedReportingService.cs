using Backend.DTOs.Results;
namespace Backend.Services;

/// <summary>
/// Service for advanced reporting features including charts, comparisons, and analytics
/// </summary>
public class AdvancedReportingService : IAdvancedReportingService
{
    private readonly ITallyService _tallyService;
    private readonly ILogger<AdvancedReportingService> _logger;

    /// <summary>
    /// Initializes a new instance of the AdvancedReportingService.
    /// </summary>
    /// <param name="tallyService">The tally service for retrieving election data.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public AdvancedReportingService(ITallyService tallyService, ILogger<AdvancedReportingService> logger)
    {
        _tallyService = tallyService;
        _logger = logger;
    }

    /// <summary>
    /// Generates chart data for various election statistics visualizations.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="chartType">The type of chart to generate (e.g., "turnout-by-location", "candidate-votes").</param>
    /// <returns>The chart data for the specified election and chart type.</returns>
    public async Task<ChartDataDto> GenerateChartDataAsync(Guid electionId, string chartType)
    {
        _logger.LogInformation("Generating chart data for election {ElectionId}, type {ChartType}", electionId, chartType);

        var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);

        return chartType.ToLower() switch
        {
            "turnout-by-location" => GenerateTurnoutByLocationChart(detailedStats),
            "candidate-votes" => await GenerateCandidateVotesChartAsync(electionId),
            "vote-distribution" => GenerateVoteDistributionChart(detailedStats),
            "turnout-over-time" => GenerateTurnoutOverTimeChart(detailedStats),
            _ => throw new ArgumentException($"Unsupported chart type: {chartType}")
        };
    }

    /// <summary>
    /// Compares multiple elections across specified metrics.
    /// </summary>
    /// <param name="electionIds">The list of election IDs to compare.</param>
    /// <param name="metrics">The list of metrics to compare (e.g., turnout, vote distribution).</param>
    /// <returns>Comparison data for the specified elections and metrics.</returns>
    public async Task<ElectionComparisonDto> CompareElectionsAsync(List<Guid> electionIds, List<string> metrics)
    {
        _logger.LogInformation("Comparing {Count} elections with metrics: {Metrics}", electionIds.Count, string.Join(", ", metrics));

        var elections = new List<ResultsElectionSummaryDto>();
        var trends = new List<TrendDataDto>();

        foreach (var electionId in electionIds)
        {
            var report = await _tallyService.GetElectionReportAsync(electionId);
            var stats = await _tallyService.GetDetailedStatisticsAsync(electionId);

            elections.Add(new ResultsElectionSummaryDto
            {
                ElectionGuid = electionId,
                ElectionName = report.ElectionName,
                ElectionDate = report.ElectionDate,
                TotalRegisteredVoters = stats.Overview.TotalRegisteredVoters,
                TotalBallotsCast = stats.Overview.TotalBallotsCast,
                TurnoutPercentage = stats.Overview.OverallTurnoutPercentage,
                TotalVotes = stats.Overview.TotalVotes,
                PositionsToElect = stats.Overview.PositionsToElect,
                ElectedCount = report.Elected.Count
            });
        }

        // Generate trend data for requested metrics
        foreach (var metric in metrics)
        {
            var trend = new TrendDataDto { Metric = metric };
            foreach (var election in elections.OrderBy(e => e.ElectionDate))
            {
                decimal value = metric switch
                {
                    "turnout" => election.TurnoutPercentage,
                    "votes" => election.TotalVotes,
                    "voters" => election.TotalRegisteredVoters,
                    _ => 0
                };

                trend.Points.Add(new TrendPointDto
                {
                    Date = election.ElectionDate ?? DateTimeOffset.UtcNow,
                    Value = value,
                    ElectionName = election.ElectionName
                });
            }
            trends.Add(trend);
        }

        var comparison = new ElectionComparisonDto
        {
            Elections = elections,
            Trends = trends,
            Metrics = new ComparisonMetricsDto
            {
                AverageTurnout = elections.Average(e => e.TurnoutPercentage),
                TotalElections = elections.Count,
                MetricAverages = metrics.ToDictionary(m => m, m =>
                    elections.Average(e => m switch
                    {
                        "turnout" => e.TurnoutPercentage,
                        "votes" => e.TotalVotes,
                        "voters" => e.TotalRegisteredVoters,
                        _ => 0
                    }))
            }
        };

        // Calculate turnout change
        if (elections.Count >= 2)
        {
            var sorted = elections.OrderBy(e => e.ElectionDate).ToList();
            comparison.Metrics.TurnoutChange = sorted.Last().TurnoutPercentage - sorted.First().TurnoutPercentage;
        }

        return comparison;
    }

    /// <summary>
    /// Generates a filtered report for the specified election based on the provided filter criteria.
    /// </summary>
    /// <param name="electionId">The GUID of the election to generate the report for.</param>
    /// <param name="filters">The filter criteria to apply to the report data.</param>
    /// <returns>A filtered report containing election results that match the specified criteria.</returns>
    public async Task<FilteredReportDto> GenerateFilteredReportAsync(Guid electionId, AdvancedFilterDto filters)
    {
        _logger.LogInformation("Generating filtered report for election {ElectionId}", electionId);

        // Get base data
        var report = await _tallyService.GetElectionReportAsync(electionId);
        var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);

        // Apply filters (simplified implementation)
        var filteredCandidates = ApplyCandidateFilters(report.Elected.Concat(report.Other).Concat(report.Extra), filters);
        var filteredLocations = ApplyLocationFilters(detailedStats.LocationStatistics, filters);

        return new FilteredReportDto
        {
            AppliedFilters = filters,
            TotalRecords = report.Elected.Count + report.Other.Count + report.Extra.Count,
            FilteredRecords = filteredCandidates.Count(),
            Summary = report,
            Candidates = filteredCandidates.ToList(),
            Locations = filteredLocations.Select(l => new LocationReportDto
            {
                LocationName = l.LocationName,
                TotalVoters = l.RegisteredVoters,
                Voted = l.BallotsCast,
                BallotsEntered = l.ValidBallots,
                TotalVotes = l.TotalVotes
            }).ToList()
        };
    }

    /// <summary>
    /// Generates a custom report based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration specifying the type and parameters of the custom report.</param>
    /// <returns>A custom report generated according to the specified configuration.</returns>
    public async Task<CustomReportDto> GenerateCustomReportAsync(CustomReportConfigDto config)
    {
        _logger.LogInformation("Generating custom report: {ReportName}", config.ReportName);

        var generatedData = new Dictionary<string, object>
        {
            ["title"] = config.ReportName,
            ["description"] = config.Description,
            ["generatedAt"] = DateTimeOffset.UtcNow
        };

        foreach (var section in config.Sections.OrderBy(s => s.Order))
        {
            var electionId = section.Parameters.TryGetValue("electionGuid", out var egObj) && egObj is string egStr && Guid.TryParse(egStr, out var eg) ? eg : Guid.Empty;

            if (electionId == Guid.Empty)
                continue;

            try
            {
                switch (section.SectionType.ToLower())
                {
                    case "summary":
                        var stats = await _tallyService.GetDetailedStatisticsAsync(electionId);
                        generatedData[$"section_{section.Order}_summary"] = stats.Overview;
                        break;

                    case "candidates":
                        var report = await _tallyService.GetElectionReportAsync(electionId);
                        var candidates = report.Elected.Concat(report.Extra).Concat(report.Other).ToList();
                        if (config.DefaultFilters != null)
                            candidates = ApplyCandidateFilters(candidates, config.DefaultFilters).ToList();
                        generatedData[$"section_{section.Order}_candidates"] = candidates;
                        break;

                    case "locations":
                        var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);
                        var locations = detailedStats.LocationStatistics.ToList();
                        if (config.DefaultFilters != null)
                            locations = ApplyLocationFilters(locations, config.DefaultFilters).ToList();
                        generatedData[$"section_{section.Order}_locations"] = locations;
                        break;

                    case "chart":
                        var chartType = section.Parameters.TryGetValue("chartType", out var ctObj) && ctObj is string ct ? ct : "candidate-votes";
                        var chartData = await GenerateChartDataAsync(electionId, chartType);
                        generatedData[$"section_{section.Order}_chart"] = chartData;
                        break;

                    case "statistics":
                        var analysis = await GenerateStatisticalAnalysisAsync(electionId);
                        generatedData[$"section_{section.Order}_statistics"] = analysis;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate section {SectionType} for report {ReportName}", section.SectionType, config.ReportName);
                generatedData[$"section_{section.Order}_error"] = $"Failed to generate {section.SectionType}: {ex.Message}";
            }
        }

        return new CustomReportDto
        {
            ReportGuid = Guid.NewGuid(),
            Config = config,
            GeneratedData = generatedData,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Generates a statistical analysis report for the specified election.
    /// </summary>
    /// <param name="electionId">The GUID of the election to generate statistical analysis for.</param>
    /// <returns>A statistical analysis containing various metrics and insights about the election.</returns>
    public async Task<StatisticalAnalysisDto> GenerateStatisticalAnalysisAsync(Guid electionId)
    {
        _logger.LogInformation("Generating statistical analysis for election {ElectionId}", electionId);

        var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);

        return new StatisticalAnalysisDto
        {
            Overview = detailedStats.Overview,
            VotingPatterns = new VotingPatternAnalysisDto
            {
                AverageVotesPerBallot = detailedStats.Overview.TotalVotes / (decimal)detailedStats.Overview.TotalBallotsCast,
                VoteDistribution = detailedStats.VoteDistribution.BallotLengthDistribution,
                BallotCompletenessRate = detailedStats.Overview.ValidBallots / (decimal)detailedStats.Overview.TotalBallotsCast * 100
            },
            CandidateAnalysis = GenerateCandidateAnalysis(detailedStats),
            LocationAnalysis = GenerateLocationAnalysis(detailedStats),
            TimeBasedAnalysis = new TimeBasedAnalysisDto
            {
                VotingRateAcceleration = 0, // Placeholder
                Segments = new List<TimeSegmentDto>() // Placeholder
            },
            PredictiveMetrics = new PredictiveMetricsDto
            {
                ProjectedTurnout = detailedStats.Overview.OverallTurnoutPercentage,
                Predictions = new List<PredictionDto>() // Placeholder
            }
        };
    }

    private ChartDataDto GenerateTurnoutByLocationChart(DetailedStatisticsDto stats)
    {
        return new ChartDataDto
        {
            ChartType = "bar",
            Title = "Turnout by Location",
            Labels = stats.LocationStatistics.Select(l => l.LocationName).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new ChartDatasetDto
                {
                    Label = "Turnout %",
                    Data = stats.LocationStatistics.Select(l => l.TurnoutPercentage).ToList(),
                    BackgroundColors = GenerateColors(stats.LocationStatistics.Count)
                }
            }
        };
    }

    private async Task<ChartDataDto> GenerateCandidateVotesChartAsync(Guid electionId)
    {
        var report = await _tallyService.GetElectionReportAsync(electionId);
        var candidates = report.Elected.Concat(report.Other).Concat(report.Extra)
            .OrderByDescending(c => c.VoteCount)
            .Take(10); // Top 10

        return new ChartDataDto
        {
            ChartType = "horizontalBar",
            Title = "Candidate Votes",
            Labels = candidates.Select(c => c.FullName).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new ChartDatasetDto
                {
                    Label = "Votes",
                    Data = candidates.Select(c => (decimal)c.VoteCount).ToList(),
                    BackgroundColors = GenerateColors(candidates.Count())
                }
            }
        };
    }

    private ChartDataDto GenerateVoteDistributionChart(DetailedStatisticsDto stats)
    {
        return new ChartDataDto
        {
            ChartType = "pie",
            Title = "Vote Distribution",
            Labels = stats.VoteDistribution.VoteCountDistribution.Keys.Select(k => k.ToString()).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new ChartDatasetDto
                {
                    Label = "Votes",
                    Data = stats.VoteDistribution.VoteCountDistribution.Values.Select(v => (decimal)v).ToList(),
                    BackgroundColors = GenerateColors(stats.VoteDistribution.VoteCountDistribution.Count)
                }
            }
        };
    }

    private ChartDataDto GenerateTurnoutOverTimeChart(DetailedStatisticsDto stats)
    {
        var timeData = stats.TurnoutAnalysis?.TimeBasedTurnout ?? new List<TimeBasedTurnoutDto>();

        if (timeData.Count == 0)
        {
            return new ChartDataDto
            {
                ChartType = "line",
                Title = "Turnout Over Time",
                Labels = new List<string> { "Total" },
                Datasets = new List<ChartDatasetDto>
                {
                    new ChartDatasetDto
                    {
                        Label = "Turnout %",
                        Data = new List<decimal> { stats.Overview.OverallTurnoutPercentage },
                        BorderColors = new List<string> { "#409eff" }
                    }
                }
            };
        }

        return new ChartDataDto
        {
            ChartType = "line",
            Title = "Turnout Over Time",
            Labels = timeData.Select(t => t.TimePeriod.ToString("HH:mm")).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new ChartDatasetDto
                {
                    Label = "Turnout %",
                    Data = timeData.Select(t => t.CumulativeTurnout).ToList(),
                    BorderColors = new List<string> { "#409eff" }
                }
            }
        };
    }

    private IEnumerable<CandidateReportDto> ApplyCandidateFilters(IEnumerable<CandidateReportDto> candidates, AdvancedFilterDto filters)
    {
        var query = candidates.AsQueryable();

        if (filters.CandidateNames?.Any() == true)
            query = query.Where(c => filters.CandidateNames.Contains(c.FullName));

        if (filters.VoteCountRange != null)
        {
            if (filters.VoteCountRange.Min.HasValue)
                query = query.Where(c => c.VoteCount >= filters.VoteCountRange.Min.Value);
            if (filters.VoteCountRange.Max.HasValue)
                query = query.Where(c => c.VoteCount <= filters.VoteCountRange.Max.Value);
        }

        if (filters.OnlyElected == true)
            query = query.Where(c => c.Section == "E");

        // Apply sorting
        if (!string.IsNullOrEmpty(filters.SortBy))
        {
            query = filters.SortBy.ToLower() switch
            {
                "name" => filters.SortOrder == "desc" ? query.OrderByDescending(c => c.FullName) : query.OrderBy(c => c.FullName),
                "votes" => filters.SortOrder == "desc" ? query.OrderByDescending(c => c.VoteCount) : query.OrderBy(c => c.VoteCount),
                _ => query
            };
        }

        return query;
    }

    private IEnumerable<LocationStatisticsDto> ApplyLocationFilters(IEnumerable<LocationStatisticsDto> locations, AdvancedFilterDto filters)
    {
        var query = locations.AsQueryable();

        if (filters.Locations?.Any() == true)
            query = query.Where(l => filters.Locations.Contains(l.LocationName));

        if (filters.TurnoutRange != null)
        {
            if (filters.TurnoutRange.Min.HasValue)
                query = query.Where(l => l.TurnoutPercentage >= filters.TurnoutRange.Min.Value);
            if (filters.TurnoutRange.Max.HasValue)
                query = query.Where(l => l.TurnoutPercentage <= filters.TurnoutRange.Max.Value);
        }

        return query;
    }

    private CandidateAnalysisDto GenerateCandidateAnalysis(DetailedStatisticsDto stats)
    {
        var performances = stats.CandidatePerformance;
        return new CandidateAnalysisDto
        {
            AverageVotePercentage = performances.Average(p => p.VotePercentage),
            VotePercentageVariance = CalculateVariance(performances.Select(p => p.VotePercentage)),
            Clusters = new List<CandidateClusterDto>() // Placeholder
        };
    }

    private LocationAnalysisDto GenerateLocationAnalysis(DetailedStatisticsDto stats)
    {
        var locations = stats.LocationStatistics;
        return new LocationAnalysisDto
        {
            TurnoutVariance = CalculateVariance(locations.Select(l => l.TurnoutPercentage)),
            Clusters = new List<LocationClusterDto>() // Placeholder
        };
    }

    private decimal CalculateVariance(IEnumerable<decimal> values)
    {
        var avg = values.Average();
        return values.Sum(v => (v - avg) * (v - avg)) / values.Count();
    }

    private List<string> GenerateColors(int count)
    {
        var colors = new[] { "#409eff", "#67c23a", "#e6a23c", "#f56c6c", "#909399", "#c71585", "#daa520", "#32cd32", "#ff6347", "#4682b4" };
        var result = new List<string>();
        for (int i = 0; i < count; i++)
        {
            result.Add(colors[i % colors.Length]);
        }
        return result;
    }
}


