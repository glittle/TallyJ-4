using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.Application.Services;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for importing data into elections, such as ballot information from CSV files.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly ImportService _importService;

    /// <summary>
    /// Initializes a new instance of the ImportController.
    /// </summary>
    /// <param name="importService">The import service for data import operations.</param>
    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// Imports ballot data from CSV content for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to import data into.</param>
    /// <param name="request">The import request containing CSV data.</param>
    /// <returns>Import results including counts of created ballots and votes.</returns>
    [HttpPost("ballots/{electionGuid}")]
    public async Task<IActionResult> ImportBallots(Guid electionGuid, [FromBody] ImportBallotsRequest request)
    {
        if (string.IsNullOrEmpty(request.CsvContent))
        {
            return BadRequest(new { error = "CSV content is required" });
        }

        var result = await _importService.ImportBallotDataAsync(electionGuid, request.CsvContent);

        if (!result.Success)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new
        {
            message = $"Import completed. Created {result.BallotsCreated} ballots with {result.VotesCreated} votes",
            ballotsCreated = result.BallotsCreated,
            votesCreated = result.VotesCreated
        });
    }
}

/// <summary>
/// Request model for importing ballot data.
/// </summary>
public class ImportBallotsRequest
{
    /// <summary>
    /// The CSV content containing ballot data to import.
    /// </summary>
    public string CsvContent { get; set; } = null!;
}