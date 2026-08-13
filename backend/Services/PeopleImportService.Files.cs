using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Entities;
using Backend.DTOs.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class PeopleImportService
{
    /// <summary>
    /// Uploads a file for people import and stores it in the database.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="file">The uploaded file.</param>
    /// <returns>The created import file information.</returns>
    public async Task<ImportFileDto> UploadFileAsync(Guid electionGuid, IFormFile file)
    {
        // Validate file extension
        var allowedExtensions = new[] { ".csv", ".tsv", ".tab", ".txt", ".xlsx" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException($"File type '{fileExtension}' is not supported. Supported types: {string.Join(", ", allowedExtensions)}");
        }

        // Validate file size (10MB limit)
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            throw new ArgumentException($"File size {file.Length} bytes exceeds the maximum allowed size of {maxFileSize} bytes (10MB)");
        }

        // Read file content
        byte[] fileContent;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            fileContent = memoryStream.ToArray();
        }

        // Determine file type
        var fileType = fileExtension switch
        {
            ".xlsx" => "xlsx",
            ".csv" => "csv",
            ".tsv" => "tab",
            ".tab" => "tab",
            ".txt" => "csv", // Default to CSV for .txt files
            _ => "csv"
        };

        // Auto-detect header row for XLSX files
        int detectedHeaderRow = 1;
        if (fileType == "xlsx")
        {
            try
            {
                using var stream = new MemoryStream(fileContent);
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                detectedHeaderRow = DetectHeaderRow(worksheet);
            }
            catch (Exception)
            {
                // If detection fails for any reason (corrupt file, empty worksheet, etc.), 
                // default to row 1 and let later validation handle the error
                // In production, consider logging this: _logger.LogWarning(ex, "Header detection failed")
                detectedHeaderRow = 1;
            }
        }

        // Create import file record
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            UploadTime = DateTimeOffset.UtcNow,
            FileSize = (int)file.Length,
            HasContent = true,
            FirstDataRow = detectedHeaderRow, // Use auto-detected row for XLSX, 1 for others
            ColumnsToRead = null, // No mapping yet
            OriginalFileName = file.FileName,
            ProcessingStatus = "Uploaded",
            FileType = fileType,
            CodePage = fileType == "xlsx" ? null : 65001, // UTF-8 for text files, null for XLSX
            Messages = null,
            Contents = fileContent
        };

        _context.ImportFiles.Add(importFile);
        await _context.SaveChangesAsync();

        return ToImportFileDto(importFile);
    }

    /// <summary>
    /// Retrieves all import files for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>List of import files for the election.</returns>
    public async Task<List<ImportFileDto>> GetFilesAsync(Guid electionGuid)
    {
        var files = await _context.ImportFiles
            .Where(f => f.ElectionGuid == electionGuid)
            .OrderByDescending(f => f.UploadTime)
            .ToListAsync();

        return files.Select(ToImportFileDto).ToList();
    }

    private static ImportFileDto ToImportFileDto(ImportFile file)
    {
        return new ImportFileDto
        {
            RowId = file.RowId,
            ElectionGuid = file.ElectionGuid,
            UploadTime = file.UploadTime,
            ImportTime = file.ImportTime,
            FileSize = file.FileSize,
            HasContent = file.HasContent,
            FirstDataRow = file.FirstDataRow,
            ColumnsToRead = file.ColumnsToRead,
            OriginalFileName = file.OriginalFileName,
            ProcessingStatus = file.ProcessingStatus,
            FileType = file.FileType,
            CodePage = file.CodePage,
            Messages = file.Messages
        };
    }

    /// <summary>
    /// Parses an uploaded file and returns headers, preview data, and auto-mappings.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="codePage">Optional code page for text encoding.</param>
    /// <param name="firstDataRow">Optional first data row number (1-based).</param>
    /// <returns>Parsed file information including headers and preview.</returns>
    public async Task<ParseFileResponse> ParseFileAsync(Guid electionGuid, int rowId, int? codePage = null, int? firstDataRow = null)
    {
        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null || importFile.HasContent != true || importFile.Contents == null)
        {
            throw new ArgumentException("Import file not found or has no content");
        }

        // Update settings if provided
        if (codePage.HasValue)
            importFile.CodePage = codePage.Value;
        if (firstDataRow.HasValue)
            importFile.FirstDataRow = firstDataRow.Value;

        await _context.SaveChangesAsync();

        // Headers + up to 3 non-empty samples per column (not full file rows)
        var (headers, previewRows, totalDataRows) = await ParseFileContentAsync(
            importFile,
            previewSamplesOnly: true);

        // Check for saved mappings first, otherwise generate auto-mappings
        List<ColumnMappingDto> mappings;
        if (!string.IsNullOrEmpty(importFile.ColumnsToRead))
        {
            try
            {
                mappings = JsonSerializer.Deserialize<List<ColumnMappingDto>>(importFile.ColumnsToRead) ?? new List<ColumnMappingDto>();
            }
            catch (JsonException)
            {
                // If deserialization fails, fall back to auto-mappings
                mappings = GenerateAutoMappings(headers);
            }
        }
        else
        {
            mappings = GenerateAutoMappings(headers);
        }

        return new ParseFileResponse
        {
            Headers = headers,
            PreviewRows = previewRows.Select(row => row.Cells).ToList(),
            TotalDataRows = totalDataRows,
            AutoMappings = mappings
        };
    }

    /// <summary>
    /// Saves column mappings for an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="mappings">List of column mappings.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task SaveColumnMappingsAsync(Guid electionGuid, int rowId, List<ColumnMappingDto> mappings)
    {
        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null)
        {
            throw new ArgumentException("Import file not found");
        }

        // check that there are no duplicate target fields in the mappings
        var duplicateTargets = mappings
        .Where(m => !string.IsNullOrEmpty(m.TargetField))
        .GroupBy(m => m.TargetField)
        .Where(g => g.Count() > 1)
        .Select(g => string.Join(", ", g.Select(t => $"{t.FileColumn} → {g.Key}")))
        .ToList();

        if (duplicateTargets.Any())
        {
            throw new ArgumentException($"Duplicate target fields in mappings: {string.Join("; ", duplicateTargets)}");
        }

        // Serialize mappings to JSON
        importFile.ColumnsToRead = JsonSerializer.Serialize(mappings);
        importFile.ProcessingStatus = "Mapped";

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets saved column mappings for an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>List of column mappings, or null if none are saved.</returns>
    public async Task<List<ColumnMappingDto>?> GetColumnMappingsAsync(Guid electionGuid, int rowId)
    {
        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null || string.IsNullOrEmpty(importFile.ColumnsToRead))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<ColumnMappingDto>>(importFile.ColumnsToRead);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates file settings like first data row and code page.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <param name="settings">The settings to update.</param>
    /// <returns>Task representing the operation.</returns>
    public async Task<ImportFileDto> UpdateFileSettingsAsync(Guid electionGuid, int rowId, UpdateFileSettingsDto settings)
    {
        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null)
        {
            throw new ArgumentException("Import file not found");
        }

        if (settings.FirstDataRow.HasValue)
            importFile.FirstDataRow = settings.FirstDataRow.Value;
        if (settings.CodePage.HasValue)
            importFile.CodePage = settings.CodePage.Value;

        await _context.SaveChangesAsync();
        return ToImportFileDto(importFile);
    }

    /// <summary>
    /// Deletes an import file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>True if the file was deleted successfully.</returns>
    public async Task<bool> DeleteFileAsync(Guid electionGuid, int rowId)
    {
        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null)
        {
            return false;
        }

        _context.ImportFiles.Remove(importFile);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes all people for an election (with safety guards).
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>The result of the delete operation.</returns>
    public async Task<DeleteAllPeopleResult> DeleteAllPeopleAsync(Guid electionGuid)
    {
        var result = new DeleteAllPeopleResult();

        // Check for existing ballots
        var ballotCount = await _context.Ballots
            .Include(b => b.Location)
            .CountAsync(b => b.Location.ElectionGuid == electionGuid);
        if (ballotCount > 0)
        {
            throw new InvalidOperationException($"Cannot delete all people: {ballotCount} ballots exist for this election");
        }

        // Check for people with registration time set
        var registeredPeopleCount = await _context.People.CountAsync(p => p.ElectionGuid == electionGuid && p.RegistrationTime.HasValue);
        if (registeredPeopleCount > 0)
        {
            throw new InvalidOperationException($"Cannot delete all people: {registeredPeopleCount} people have voting status set");
        }

        // Delete all people
        var peopleToDelete = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .ToListAsync();

        result.DeletedCount = peopleToDelete.Count;

        _context.People.RemoveRange(peopleToDelete);
        await _context.SaveChangesAsync();

        if (result.DeletedCount > 0)
        {
            await _signalRNotificationService.RequestFrontDeskReloadAsync(electionGuid);
        }

        return result;
    }

    /// <summary>
    /// Gets the count of people for an election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>The number of people in the election.</returns>
    public async Task<int> GetPeopleCountAsync(Guid electionGuid)
    {
        return await _context.People.CountAsync(p => p.ElectionGuid == electionGuid);
    }
}
