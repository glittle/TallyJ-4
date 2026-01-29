using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Results;
using TallyJ4.Services;

namespace TallyJ4.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportExportService _reportExportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportExportService reportExportService,
        ILogger<ReportsController> logger)
    {
        _reportExportService = reportExportService;
        _logger = logger;
    }

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
                default:
                    return BadRequest(new { message = "Unsupported format. Supported formats: pdf, excel, xlsx" });
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
}