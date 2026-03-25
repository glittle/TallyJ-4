using System.Security.Claims;
using Backend.DTOs.Import;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for handling ballot import operations, including CSV parsing and ballot imports.
/// </summary>
[ApiController]
[Route("api/Import")]
[Authorize]
public class BallotImportController : ControllerBase
{
    private const long MaxFileSize = 50 * 1024 * 1024;
    private readonly ImportService _importService;
    private readonly ElectionExportImportService _electionExportImportService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BallotImportController"/> class.
    /// </summary>
    /// <param name="importService">The import service.</param>
    /// <param name="electionExportImportService">The election export/import service.</param>
    public BallotImportController(ImportService importService, ElectionExportImportService electionExportImportService)
    {
        _importService = importService;
        _electionExportImportService = electionExportImportService;
    }

    /// <summary>
    /// Parses CSV content and extracts column headers.
    /// </summary>
    /// <param name="request">The CSV parsing request.</param>
    /// <returns>The parsed headers.</returns>
    [HttpPost("parse-csv-headers")]
    public async Task<IActionResult> ParseCsvHeaders([FromBody] ParseCsvHeadersRequest request)
    {
        if (string.IsNullOrEmpty(request.CsvContent))
        {
            return BadRequest(new { error = "CSV content is required" });
        }

        var result = await _importService.ParseCsvHeadersAsync(request.CsvContent, request.Delimiter ?? ",");
        return Ok(result);
    }

    /// <summary>
    /// Imports ballot data from CSV content.
    /// </summary>
    /// <param name="request">The ballot import request.</param>
    /// <returns>The import result including created ballots and votes.</returns>
    [HttpPost("importBallots")]
    public async Task<IActionResult> ImportBallots([FromBody] ImportBallotRequestDto request)
    {
        if (string.IsNullOrEmpty(request.CsvContent))
        {
            return BadRequest(new { error = "CSV content is required" });
        }

        if (request.ElectionGuid == Guid.Empty)
        {
            return BadRequest(new { error = "Election GUID is required" });
        }

        var result = await _importService.ImportBallotDataAsync(request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                errors = result.Errors,
                warnings = result.Warnings,
                ballotsCreated = result.BallotsCreated,
                votesCreated = result.VotesCreated,
                skippedRows = result.SkippedRows
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Imports Canadian ballot data from XML file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to import ballots into.</param>
    /// <param name="file">The XML file to import.</param>
    /// <returns>The import result.</returns>
    [HttpPost("importCdnBallots/{electionGuid}")]
    [Authorize(Policy = "ElectionAccess")]
    public async Task<IActionResult> ImportCdnBallots(Guid electionGuid, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { error = "File too large" });
            }

            using var stream = file.OpenReadStream();
            var result = await _electionExportImportService.ImportCdnBallotsAsync(electionGuid, stream);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    errors = result.Errors,
                    warnings = result.Warnings,
                    ballotsCreated = result.BallotsCreated,
                    votesCreated = result.VotesCreated
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Import failed: {ex.Message}" });
        }
    }
}

/// <summary>
/// Request model for parsing CSV headers.
/// </summary>
public class ParseCsvHeadersRequest
{
    /// <summary>
    /// Gets or sets the CSV content to parse.
    /// </summary>
    public string CsvContent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delimiter character (default: comma).
    /// </summary>
    public string? Delimiter { get; set; }
}