using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Results;
using TallyJ4.Services;

/// <summary>
/// Controller for managing election reports and advanced reporting features.
/// Provides endpoints for exporting reports, generating charts, comparing elections, and statistical analysis.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportExportService _reportExportService;
    private readonly IAdvancedReportingService _advancedReportingService;
    private readonly ILogger<ReportsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ReportsController.
    /// </summary>
    /// <param name="reportExportService">The report export service for generating file exports.</param>
    /// <param name="advancedReportingService">The advanced reporting service for complex reports and analysis.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public ReportsController(
        IReportExportService reportExportService,
        IAdvancedReportingService advancedReportingService,
        ILogger<ReportsController> logger)
    {
        _reportExportService = reportExportService;
        _advancedReportingService = advancedReportingService;
        _logger = logger;
    }

    /// <summary>
    /// Exports an election report in the specified format (PDF, Excel, or CSV).
    /// </summary>
    /// <param name="electionId">The GUID of the election to export.</param>
    /// <param name="request">The export request containing format and filter options.</param>
    /// <returns>A file download response with the exported report.</returns>
    [HttpPost("export/{electionId:guid}")]
    public async Task<IActionResult> ExportReport(Guid electionId, [FromBody] ExportRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting report for election {ElectionId} in format {Format}",
                electionId, request.Format);

            byte[] fileData;
            string contentType;
            string fileName;

            switch (request.Format.ToLower())
            {
                case "pdf":
                    fileData = await _reportExportService.GeneratePdfReportAsync(electionId, request.Filters);
                    contentType = "application/pdf";
                    fileName = $"election_report_{electionId}.pdf";
                    break;
                case "excel":
                case "xlsx":
                    fileData = await _reportExportService.GenerateExcelReportAsync(electionId, request.Filters);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"election_report_{electionId}.xlsx";
                    break;
                case "csv":
                    fileData = await _reportExportService.GenerateCsvReportAsync(electionId, request.Filters);
                    contentType = "text/csv";
                    fileName = $"election_report_{electionId}.csv";
                    break;
                default:
                    return BadRequest(new { message = "Unsupported format. Supported formats: pdf, excel, xlsx, csv" });
            }

            _logger.LogInformation("Report exported successfully for election {ElectionId}", electionId);
            return File(fileData, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionId} not found", electionId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report for election {ElectionId}", electionId);
            throw;
        }
    }

    /// <summary>
    /// Generates chart data for visualizing election results.
    /// </summary>
    /// <param name="electionId">The GUID of the election to generate chart data for.</param>
    /// <param name="chartType">The type of chart to generate.</param>
    /// <returns>The chart data for the specified election and chart type.</returns>
    [HttpGet("chart/{electionId:guid}/{chartType}")]
    public async Task<ActionResult<ChartDataDto>> GetChartData(Guid electionId, string chartType)
    {
        try
        {
            _logger.LogInformation("Generating chart data for election {ElectionId}, type {ChartType}", electionId, chartType);

            var chartData = await _advancedReportingService.GenerateChartDataAsync(electionId, chartType);
            return Ok(chartData);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Chart type {ChartType} not supported for election {ElectionId}", chartType, electionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart data for election {ElectionId}", electionId);
            throw;
        }
    }

    /// <summary>
    /// Compares multiple elections based on specified metrics.
    /// </summary>
    /// <param name="request">The comparison request containing election IDs and metrics to compare.</param>
    /// <returns>The comparison results for the specified elections.</returns>
    [HttpPost("compare")]
    public async Task<ActionResult<ElectionComparisonDto>> CompareElections([FromBody] ElectionComparisonRequestDto request)
    {
        try
        {
            _logger.LogInformation("Comparing {Count} elections", request.ElectionIds.Count);

            var comparison = await _advancedReportingService.CompareElectionsAsync(request.ElectionIds, request.Metrics);
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid election comparison request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing elections");
            throw;
        }
    }

    /// <summary>
    /// Generates a filtered report for an election based on advanced filter criteria.
    /// </summary>
    /// <param name="electionId">The GUID of the election to generate the report for.</param>
    /// <param name="filters">The advanced filter criteria to apply.</param>
    /// <returns>The filtered report data.</returns>
    [HttpPost("advanced-filter/{electionId:guid}")]
    public async Task<ActionResult<FilteredReportDto>> GetFilteredReport(Guid electionId, [FromBody] AdvancedFilterDto filters)
    {
        try
        {
            _logger.LogInformation("Generating filtered report for election {ElectionId}", electionId);

            var filteredReport = await _advancedReportingService.GenerateFilteredReportAsync(electionId, filters);
            return Ok(filteredReport);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid filter for election {ElectionId}: {Message}", electionId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating filtered report for election {ElectionId}", electionId);
            throw;
        }
    }

    /// <summary>
    /// Generates a custom report based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration for the custom report.</param>
    /// <returns>The generated custom report.</returns>
    [HttpPost("custom")]
    public async Task<ActionResult<CustomReportDto>> GenerateCustomReport([FromBody] CustomReportConfigDto config)
    {
        try
        {
            _logger.LogInformation("Generating custom report: {ReportName}", config.ReportName);

            var customReport = await _advancedReportingService.GenerateCustomReportAsync(config);
            return Ok(customReport);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid custom report configuration: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating custom report");
            throw;
        }
    }

    /// <summary>
    /// Generates statistical analysis for an election.
    /// </summary>
    /// <param name="electionId">The GUID of the election to analyze.</param>
    /// <returns>The statistical analysis results.</returns>
    [HttpGet("statistics/{electionId:guid}")]
    public async Task<ActionResult<StatisticalAnalysisDto>> GetStatisticalAnalysis(Guid electionId)
    {
        try
        {
            _logger.LogInformation("Generating statistical analysis for election {ElectionId}", electionId);

            var analysis = await _advancedReportingService.GenerateStatisticalAnalysisAsync(electionId);
            return Ok(analysis);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Election {ElectionId} not found", electionId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating statistical analysis for election {ElectionId}", electionId);
            throw;
        }
    }
}