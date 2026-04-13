using Backend.DTOs.Import;

namespace Backend.Services;

/// <summary>
/// Service interface for managing people import operations including file upload, parsing, mapping, and import execution.
/// </summary>
public interface IPeopleImportService
{
    /// <summary>
    /// Uploads a file for people import and stores it in the database.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="file">The uploaded file.</param>
    /// <returns>The created import file information.</returns>
    Task<ImportFileDto> UploadFileAsync(Guid electionGuid, IFormFile file);

    /// <summary>
    /// Retrieves all import files for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>List of import files for the election.</returns>
    Task<List<ImportFileDto>> GetFilesAsync(Guid electionGuid);

    /// <summary>
    /// Parses an uploaded file and returns headers, preview data, and auto-mappings.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="codePage">Optional code page for text encoding.</param>
    /// <param name="firstDataRow">Optional first data row number (1-based).</param>
    /// <returns>Parsed file information including headers and preview.</returns>
    Task<ParseFileResponse> ParseFileAsync(Guid electionGuid, int rowId, int? codePage = null, int? firstDataRow = null);

    /// <summary>
    /// Saves column mappings for an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="mappings">List of column mappings.</param>
    /// <returns>Task representing the operation.</returns>
    Task SaveColumnMappingsAsync(Guid electionGuid, int rowId, List<ColumnMappingDto> mappings);

    /// <summary>
    /// Gets saved column mappings for an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>List of column mappings, or null if none are saved.</returns>
    Task<List<ColumnMappingDto>?> GetColumnMappingsAsync(Guid electionGuid, int rowId);

    /// <summary>
    /// Updates file settings like first data row and code page.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="settings">The settings to update.</param>
    /// <returns>Task representing the operation.</returns>
    Task UpdateFileSettingsAsync(Guid electionGuid, int rowId, UpdateFileSettingsDto settings);

    /// <summary>
    /// Executes the import of people from the configured file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>The result of the import operation.</returns>
    Task<ImportPeopleResult> ImportPeopleAsync(Guid electionGuid, int rowId);

    /// <summary>
    /// Deletes an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>True if the file was deleted successfully.</returns>
    Task<bool> DeleteFileAsync(Guid electionGuid, int rowId);

    /// <summary>
    /// Deletes all people for an election (with safety guards).
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteAllPeopleResult> DeleteAllPeopleAsync(Guid electionGuid);

    /// <summary>
    /// Gets the count of people for an election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>The number of people in the election.</returns>
    Task<int> GetPeopleCountAsync(Guid electionGuid);
}