using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.Backend.Services;
using TallyJ4.DTOs.Import;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly ImportService _importService;

    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

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

public class ParseCsvHeadersRequest
{
    public string CsvContent { get; set; } = null!;
    
    public string? Delimiter { get; set; }
}