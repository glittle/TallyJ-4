using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Results;
using TallyJ4.Services;

namespace TallyJ4.Controllers
{

    /// <summary>
    /// Controller for exporting election reports in various formats (PDF, Excel, CSV).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/report-exports")]
    public class ReportExportsController : ControllerBase
    {
        private readonly IReportExportService _reportExportService;
        private readonly ILogger<ReportExportsController> _logger;

        /// <summary>
        /// Initializes a new instance of the ReportExportsController.
        /// </summary>
        /// <param name="reportExportService">The report export service for generating file exports.</param>
        /// <param name="logger">The logger for recording operations.</param>
        public ReportExportsController(
            IReportExportService reportExportService,
            ILogger<ReportExportsController> logger)
        {
            _reportExportService = reportExportService;
            _logger = logger;
        }

        /// <summary>
        /// Exports an election report in the specified format (PDF, Excel, or CSV).
        /// </summary>
        /// <param name="electionId">The GUID of the election to export.</param>
        /// <param name="request">The export request containing format and filter options.</param>
        /// <returns>A file download response with the exported report.</returns>
        [HttpPost("{electionId:guid}")]
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
    }
}
