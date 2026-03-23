using Backend.DTOs.Import;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing people import operations including file upload, parsing, mapping, and import execution.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeopleImportController : ControllerBase
{
    private readonly IPeopleImportService _peopleImportService;
    private readonly ILogger<PeopleImportController> _logger;

    /// <summary>
    /// Initializes a new instance of the PeopleImportController.
    /// </summary>
    /// <param name="peopleImportService">The people import service for import operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public PeopleImportController(IPeopleImportService peopleImportService, ILogger<PeopleImportController> logger)
    {
        _peopleImportService = peopleImportService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file for people import and stores it in the database.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="file">The uploaded file.</param>
    /// <returns>The created import file information.</returns>
    [HttpPost("{electionGuid}/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<ImportFileDto>> UploadFile(Guid electionGuid, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var result = await _peopleImportService.UploadFileAsync(electionGuid, file);
            _logger.LogInformation("File uploaded for election {ElectionGuid}: {FileName}", electionGuid, file.FileName);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("File upload validation failed for election {ElectionGuid}: {Message}", electionGuid, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for election {ElectionGuid}", electionGuid);
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Retrieves all import files for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>List of import files for the election.</returns>
    [HttpGet("{electionGuid}/files")]
    public async Task<ActionResult<List<ImportFileDto>>> GetFiles(Guid electionGuid)
    {
        try
        {
            var files = await _peopleImportService.GetFilesAsync(electionGuid);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files for election {ElectionGuid}", electionGuid);
            return StatusCode(500, new { message = "An error occurred while retrieving files" });
        }
    }

    /// <summary>
    /// Parses an uploaded file and returns headers, preview data, and auto-mappings.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="codePage">Optional code page for text encoding.</param>
    /// <param name="firstDataRow">Optional first data row number (1-based).</param>
    /// <returns>Parsed file information including headers and preview.</returns>
    [HttpGet("{electionGuid}/files/{rowId}/parse")]
    public async Task<ActionResult<ParseFileResponse>> ParseFile(Guid electionGuid, int rowId,
        [FromQuery] int? codePage = null, [FromQuery] int? firstDataRow = null)
    {
        try
        {
            var result = await _peopleImportService.ParseFileAsync(electionGuid, rowId, codePage, firstDataRow);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("File parsing failed for election {ElectionGuid}, file {RowId}: {Message}", electionGuid, rowId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return StatusCode(500, new { message = "An error occurred while parsing the file" });
        }
    }

    /// <summary>
    /// Saves column mappings for an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="mappings">List of column mappings.</param>
    /// <returns>Task representing the operation.</returns>
    [HttpPut("{electionGuid}/files/{rowId}/mapping")]
    public async Task<IActionResult> SaveColumnMappings(Guid electionGuid, int rowId, [FromBody] List<ColumnMappingDto> mappings)
    {
        try
        {
            await _peopleImportService.SaveColumnMappingsAsync(electionGuid, rowId, mappings);
            _logger.LogInformation("Column mappings saved for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Save column mappings failed for election {ElectionGuid}, file {RowId}: {Message}", electionGuid, rowId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving column mappings for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return StatusCode(500, new { message = "An error occurred while saving column mappings" });
        }
    }

    /// <summary>
    /// Gets saved column mappings for an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>List of column mappings, or null if none are saved.</returns>
    [HttpGet("{electionGuid}/files/{rowId}/mapping")]
    public async Task<ActionResult<List<ColumnMappingDto>>> GetColumnMappings(Guid electionGuid, int rowId)
    {
        try
        {
            var mappings = await _peopleImportService.GetColumnMappingsAsync(electionGuid, rowId);
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving column mappings for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return StatusCode(500, new { message = "An error occurred while retrieving column mappings" });
        }
    }

    /// <summary>
    /// Updates file settings like first data row and code page.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="settings">The settings to update.</param>
    /// <returns>Task representing the operation.</returns>
    [HttpPut("{electionGuid}/files/{rowId}/settings")]
    public async Task<IActionResult> UpdateFileSettings(Guid electionGuid, int rowId, [FromBody] UpdateFileSettingsDto settings)
    {
        try
        {
            await _peopleImportService.UpdateFileSettingsAsync(electionGuid, rowId, settings);
            _logger.LogInformation("File settings updated for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Update file settings failed for election {ElectionGuid}, file {RowId}: {Message}", electionGuid, rowId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file settings for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return StatusCode(500, new { message = "An error occurred while updating file settings" });
        }
    }

    /// <summary>
    /// Executes the import of people from the configured file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>The result of the import operation.</returns>
    [HttpPost("{electionGuid}/files/{rowId}/import")]
    public async Task<ActionResult<ImportPeopleResult>> ImportPeople(Guid electionGuid, int rowId)
    {
        try
        {
            var result = await _peopleImportService.ImportPeopleAsync(electionGuid, rowId);
            _logger.LogInformation("People import completed for election {ElectionGuid}, file {RowId}: {PeopleAdded} added, {PeopleSkipped} skipped",
                electionGuid, rowId, result.PeopleAdded, result.PeopleSkipped);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("People import failed for election {ElectionGuid}, file {RowId}: {Message}", electionGuid, rowId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing people for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return StatusCode(500, new { message = "An error occurred while importing people" });
        }
    }

    /// <summary>
    /// Deletes an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>True if the file was deleted successfully.</returns>
    [HttpDelete("{electionGuid}/files/{rowId}")]
    public async Task<IActionResult> DeleteFile(Guid electionGuid, int rowId)
    {
        try
        {
            var deleted = await _peopleImportService.DeleteFileAsync(electionGuid, rowId);
            if (!deleted)
            {
                return NotFound(new { message = "Import file not found" });
            }

            _logger.LogInformation("Import file deleted for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file for election {ElectionGuid}, file {RowId}", electionGuid, rowId);
            return StatusCode(500, new { message = "An error occurred while deleting the file" });
        }
    }

    /// <summary>
    /// Deletes all people for an election (with safety guards).
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>The result of the delete operation.</returns>
    [HttpDelete("{electionGuid}/people")]
    public async Task<ActionResult<DeleteAllPeopleResult>> DeleteAllPeople(Guid electionGuid)
    {
        try
        {
            var result = await _peopleImportService.DeleteAllPeopleAsync(electionGuid);
            _logger.LogInformation("All people deleted for election {ElectionGuid}: {DeletedCount} records", electionGuid, result.DeletedCount);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Delete all people blocked for election {ElectionGuid}: {Message}", electionGuid, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all people for election {ElectionGuid}", electionGuid);
            return StatusCode(500, new { message = "An error occurred while deleting people" });
        }
    }

    /// <summary>
    /// Gets the count of people for an election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>The number of people in the election.</returns>
    [HttpGet("{electionGuid}/people-count")]
    public async Task<ActionResult<int>> GetPeopleCount(Guid electionGuid)
    {
        try
        {
            var count = await _peopleImportService.GetPeopleCountAsync(electionGuid);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting people count for election {ElectionGuid}", electionGuid);
            return StatusCode(500, new { message = "An error occurred while getting people count" });
        }
    }
}