using System.Security.Claims;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for handling election import and export operations.
/// </summary>
[ApiController]
[Route("api/Import")]
[Authorize]
public class ElectionImportController : ControllerBase
{
    private const long MaxFileSize = 50 * 1024 * 1024;
    private readonly ElectionExportImportService _electionExportImportService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElectionImportController"/> class.
    /// </summary>
    /// <param name="electionExportImportService">The election export/import service.</param>
    public ElectionImportController(ElectionExportImportService electionExportImportService)
    {
        _electionExportImportService = electionExportImportService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
        return !string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId) ? userId : null;
    }



    /// <summary>
    /// Imports an entire election from TallyJ v3 XML format.
    /// </summary>
    /// <param name="file">The XML file to import.</param>
    /// <returns>The created election information.</returns>
    [HttpPost("importTallyJv3Election")]
    public async Task<IActionResult> ImportTallyJv3Election(IFormFile file)
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
            var election = await _electionExportImportService.ImportTallyJv2ElectionAsync(stream, GetCurrentUserId());

            return CreatedAtAction(
                "GetElection",
                "Elections",
                new { guid = election.ElectionGuid },
                new { message = "Election imported successfully", election });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Import failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Imports an entire election from TallyJ v4 JSON format.
    /// </summary>
    /// <param name="file">The JSON file to import.</param>
    /// <returns>The created election information.</returns>
    [HttpPost("importElectionFromJson")]
    public async Task<IActionResult> ImportElectionFromJson(IFormFile file)
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
            var election = await _electionExportImportService.ImportElectionFromJsonAsync(stream, GetCurrentUserId());

            return CreatedAtAction(
                "GetElection",
                "Elections",
                new { guid = election.ElectionGuid },
                new { message = "Election imported successfully", election });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Import failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Exports an election to JSON format.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to export.</param>
    /// <returns>The JSON export data.</returns>
    [HttpGet("exportElectionToJson/{electionGuid}")]
    [Authorize(Policy = "ElectionAccess")]
    public async Task<IActionResult> ExportElectionToJson(Guid electionGuid)
    {
        try
        {
            var jsonContent = await _electionExportImportService.ExportElectionToJsonAsync(electionGuid);

            return File(
                System.Text.Encoding.UTF8.GetBytes(jsonContent),
                "application/json",
                $"Election_{electionGuid}.json");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Export failed: {ex.Message}" });
        }
    }
}