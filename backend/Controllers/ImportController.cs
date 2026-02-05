using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.Backend.Services;
using TallyJ4.DTOs.Import;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for handling data import operations, including CSV parsing and ballot imports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly ImportService _importService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportController"/> class.
    /// </summary>
    /// <param name="importService">The import service.</param>
    public ImportController(ImportService importService)
    {
        _importService = importService;
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
    [HttpPost("ballots")]
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