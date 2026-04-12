using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Domain.Helpers;
using Backend.DTOs.Import;
using Backend.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing people import operations including file upload, parsing, mapping, and import execution.
/// </summary>
public class PeopleImportService : IPeopleImportService
{
    private readonly MainDbContext _context;
    private readonly IHubContext<PeopleImportHub> _hubContext;

    // Scoring weights for header detection
    private const int TextCellScore = 2;
    private const int KnownFieldScore = 10;
    private const int HeaderKeywordScore = 5;

    // Auto-mapping aliases for field matching (case-insensitive, spaces/underscores/hyphens ignored)
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> FieldAliases = new Dictionary<string, IReadOnlyList<string>>
    {
        ["FirstName"] = new[] { "first name", "firstname", "first_name", "given name", "givenname" },
        ["LastName"] = new[] { "last name", "lastname", "last_name", "surname", "family name", "familyname" },
        ["BahaiId"] = new[] { "bahai id", "bahaiid", "bahai_id", "baha'i id", "id" },
        ["IneligibleReasonDescription"] = new[] { "eligibility", "eligibility status", "status", "ineligible reason" },
        ["Area"] = new[] { "area", "region", "locality", "community" },
        ["Email"] = new[] { "email", "email address", "e-mail" },
        ["Phone"] = new[] { "phone", "phone number", "telephone", "tel", "mobile" },
        ["OtherNames"] = new[] { "other names", "othernames", "other_names", "middle name", "middlename" },
        ["OtherLastNames"] = new[] { "other last names", "otherlastnames", "maiden name", "former name", "formername" },
        ["OtherInfo"] = new[] { "other info", "otherinfo", "other_info", "notes", "comments" }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleImportService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="hubContext">The SignalR hub context for people import notifications.</param>
    public PeopleImportService(MainDbContext context, IHubContext<PeopleImportHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

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

        // Map to DTO and return
        var dto = new ImportFileDto
        {
            RowId = importFile.RowId,
            ElectionGuid = importFile.ElectionGuid,
            UploadTime = importFile.UploadTime,
            ImportTime = importFile.ImportTime,
            FileSize = importFile.FileSize,
            HasContent = importFile.HasContent,
            FirstDataRow = importFile.FirstDataRow,
            ColumnsToRead = importFile.ColumnsToRead,
            OriginalFileName = importFile.OriginalFileName,
            ProcessingStatus = importFile.ProcessingStatus,
            FileType = importFile.FileType,
            CodePage = importFile.CodePage,
            Messages = importFile.Messages
        };

        return dto;
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

        return files.Select(f => new ImportFileDto
        {
            RowId = f.RowId,
            ElectionGuid = f.ElectionGuid,
            UploadTime = f.UploadTime,
            ImportTime = f.ImportTime,
            FileSize = f.FileSize,
            HasContent = f.HasContent,
            FirstDataRow = f.FirstDataRow,
            ColumnsToRead = f.ColumnsToRead,
            OriginalFileName = f.OriginalFileName,
            ProcessingStatus = f.ProcessingStatus,
            FileType = f.FileType,
            CodePage = f.CodePage,
            Messages = f.Messages
        }).ToList();
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

        // Parse the file
        var (headers, previewRows, totalDataRows) = await ParseFileContentAsync(importFile);

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
            PreviewRows = previewRows,
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
    public async Task UpdateFileSettingsAsync(Guid electionGuid, int rowId, UpdateFileSettingsDto settings)
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
    }

    /// <summary>
    /// Executes the import of people from the configured file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>The result of the import operation.</returns>
    public async Task<ImportPeopleResult> ImportPeopleAsync(Guid electionGuid, int rowId)
    {
        var result = new ImportPeopleResult();
        var groupName = GetGroupName(electionGuid);
        var startTime = DateTimeOffset.UtcNow;

        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null || importFile.HasContent != true || importFile.Contents == null)
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.fileNotFound",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        // Deserialize column mappings
        if (string.IsNullOrEmpty(importFile.ColumnsToRead))
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.noMappings",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        var mappings = JsonSerializer.Deserialize<List<ColumnMappingDto>>(importFile.ColumnsToRead);
        if (mappings == null || mappings.Count == 0)
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.invalidMappings",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        // Validate required mappings
        var firstNameMapping = mappings.FirstOrDefault(m => m.TargetField == "FirstName");
        var lastNameMapping = mappings.FirstOrDefault(m => m.TargetField == "LastName");

        if (firstNameMapping == null || lastNameMapping == null)
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.missingRequiredMappings",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        // Load existing people for deduplication
        var existingPeople = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .ToListAsync();

        var bahaiIdLookup = existingPeople
            .Where(p => !string.IsNullOrEmpty(p.BahaiId))
            .Select(p => p.BahaiId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // for voter login, the email and phone must be unique
        var emailLookup = existingPeople
        .Where(p => !string.IsNullOrEmpty(p.Email))
        .Select(p => p.Email!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var phoneLookup = existingPeople
        .Where(p => !string.IsNullOrEmpty(p.Phone))
        .Select(p => p.Phone!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nameLookup = existingPeople
            .Select(p => $"{p.FirstName ?? ""} {p.LastName}".Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        using var transaction = await _context.Database.BeginTransactionAsync();

        var rowNumber = 0; // Initialize row number for error reporting

        try
        {
            // Parse file content
            var (headers, allRows, _) = await ParseFileContentAsync(importFile);
            var firstRowNum = importFile!.FirstDataRow ?? 0;
            var dataRows = allRows.Skip(firstRowNum - 1).ToList();

            result.TotalRows = dataRows.Count;

            // Process in batches
            const int batchSize = 100;
            var peopleToAdd = new List<Person>();
            var errorsFound = false;

            for (int i = 0; i < dataRows.Count; i++)
            {
                var row = dataRows[i];
                rowNumber = i + firstRowNum + 1; // Calculate actual row number in the file for error reporting

                await ReportProgress(groupName, i + 1, dataRows.Count, $"Processing row {rowNumber}");

                try
                {
                    var skippedBefore = result.PeopleSkipped;
                    var person = CreatePersonFromRow(row, headers, mappings, electionGuid, bahaiIdLookup, emailLookup, phoneLookup, nameLookup, rowNumber, result);
                    if (person != null)
                    {
                        peopleToAdd.Add(person);
                    }
                    else if (result.PeopleSkipped == skippedBefore)
                    {
                        errorsFound = true;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportErrorDto
                    {
                        Key = "import.errors.lineError",
                        Parameters = new Dictionary<string, string>
                        {
                            ["rowNumber"] = rowNumber.ToString(),
                            ["message"] = ex.Message
                        }
                    });
                    result.PeopleSkipped++;
                    errorsFound = true;
                    continue;
                }

                // Save batch
                if (!errorsFound && (peopleToAdd.Count >= batchSize || i == dataRows.Count - 1))
                {
                    _context.People.AddRange(peopleToAdd);
                    try
                    {
                        await _context.SaveChangesAsync();
                        result.PeopleAdded += peopleToAdd.Count;
                        peopleToAdd.Clear();
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ImportErrorDto
                        {
                            Key = "import.errors.batchSaveFailed",
                            Parameters = new Dictionary<string, string>
                            {
                                ["message"] = ex.InnerException?.Message ?? ex.Message
                            }
                        });
                        errorsFound = true;
                    }
                }
            }

            if (!errorsFound)
            {
                // If no errors, update status to Imported
                importFile.ProcessingStatus = "Imported";
                importFile.ImportTime = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                result.Success = true;
                result.TimeElapsedSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

                await _hubContext.Clients.Group(groupName).SendAsync("importComplete", result);
            }
            else
            {
                // If errors found, rollback the transaction
                await transaction.RollbackAsync();
            }
        }
        catch (Exception ex)
        {
            // Rollback transaction on any exception
            await transaction.RollbackAsync();

            result.Success = false;
            if (rowNumber > 0)
            {
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.importFailedAtLine",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["message"] = ex.Message
                    }
                });
            }
            else
            {
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.importFailed",
                    Parameters = new Dictionary<string, string>
                    {
                        ["message"] = ex.Message
                    }
                });
            }
            await _hubContext.Clients.Group(groupName).SendAsync("importError", ex.Message);
        }

        return result;
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

    private async Task<(List<string> headers, List<List<string>> rows, int totalDataRows)> ParseFileContentAsync(ImportFile importFile)
    {
        if (importFile.FileType == "xlsx")
        {
            return await ParseXlsxFileAsync(importFile.Contents!, importFile.FirstDataRow);
        }
        else
        {
            return ParseTextFile(importFile.Contents!, importFile.FileType!, importFile.CodePage ?? 65001);
        }
    }

    private async Task<(List<string> headers, List<List<string>> rows, int totalDataRows)> ParseXlsxFileAsync(byte[] content, int? firstDataRow = null)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = new List<string>();
        var rows = new List<List<string>>();

        // Determine header row: use provided firstDataRow or auto-detect
        int headerRowNumber;
        if (firstDataRow.HasValue && firstDataRow.Value > 0)
        {
            headerRowNumber = firstDataRow.Value;
        }
        else
        {
            headerRowNumber = DetectHeaderRow(worksheet);
        }

        // Read headers from detected row
        var headerRow = worksheet.Row(headerRowNumber);
        var columnCount = headerRow.CellsUsed().Count();
        foreach (var cell in headerRow.CellsUsed())
        {
            headers.Add(cell.GetValue<string>() ?? "");
        }

        // Read all data rows after the header row
        var allRows = worksheet.RowsUsed().ToList();
        var dataRowsStartIndex = allRows.FindIndex(r => r.RowNumber() == headerRowNumber) + 1;

        for (int i = dataRowsStartIndex; i < allRows.Count; i++)
        {
            var row = allRows[i];
            var rowData = new List<string>();
            for (int colNum = 1; colNum <= columnCount; colNum++)
            {
                var cell = row.Cell(colNum);
                rowData.Add(cell.GetValue<string>() ?? "");
            }
            rows.Add(rowData);
        }

        return (headers, rows, rows.Count);
    }

    /// <summary>
    /// Detects the row number (1-based) where column headers are likely located.
    /// Scans the first 10 rows looking for text-based headers that match known field names.
    /// The Take() method safely handles worksheets with fewer than 10 rows.
    /// </summary>
    private int DetectHeaderRow(ClosedXML.Excel.IXLWorksheet worksheet)
    {
        const int maxRowsToScan = 10;
        var allRows = worksheet.RowsUsed().Take(maxRowsToScan).ToList();

        if (!allRows.Any())
            return 1; // Default to first row if no rows found

        int bestRowNumber = 1;
        int bestScore = 0;

        foreach (var row in allRows)
        {
            int score = ScoreHeaderRow(row);
            if (score > bestScore)
            {
                bestScore = score;
                bestRowNumber = row.RowNumber();
            }
        }

        return bestRowNumber;
    }

    /// <summary>
    /// Scores a row based on how likely it is to be a header row.
    /// Higher scores indicate more header-like characteristics.
    /// Uses scoring weights: Text cells (+2), Known fields (+10), Header keywords (+5).
    /// </summary>
    private int ScoreHeaderRow(ClosedXML.Excel.IXLRow row)
    {
        int score = 0;
        var cells = row.CellsUsed().ToList();

        if (!cells.Any())
            return 0;

        foreach (var cell in cells)
        {
            var value = cell.GetValue<string>()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(value))
                continue;

            // Bonus: Cell contains text (not just numbers)
            if (!double.TryParse(value, out _))
            {
                score += TextCellScore;
            }

            // Bonus: Matches known field aliases
            var normalizedValue = NormalizeHeader(value);
            foreach (var fieldAliases in FieldAliases.Values)
            {
                if (fieldAliases.Any(alias => NormalizeHeader(alias) == normalizedValue))
                {
                    score += KnownFieldScore; // Strong indicator of a header
                    break;
                }
            }

            // Bonus: Contains common header keywords
            var lowerValue = value.ToLower();
            if (lowerValue.Contains("name") || lowerValue.Contains("id") ||
                lowerValue.Contains("email") || lowerValue.Contains("phone") ||
                lowerValue.Contains("area") || lowerValue.Contains("status") ||
                lowerValue.Contains("eligibility"))
            {
                score += HeaderKeywordScore;
            }
        }

        return score;
    }

    private (List<string> headers, List<List<string>> rows, int totalDataRows) ParseTextFile(byte[] content, string fileType, int codePage)
    {
        var encoding = Encoding.GetEncoding(codePage);
        var text = encoding.GetString(content);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim('\r', '\n'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (lines.Length == 0)
        {
            return (new List<string>(), new List<List<string>>(), 0);
        }

        var delimiter = fileType == "tab" ? '\t' : ',';
        var headers = ParseCsvLine(lines[0], delimiter).ToList();
        var rows = new List<List<string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            rows.Add(ParseCsvLine(lines[i], delimiter));
        }

        return (headers, rows, rows.Count);
    }

    private List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }

    private List<ColumnMappingDto> GenerateAutoMappings(List<string> headers)
    {
        var mappings = new List<ColumnMappingDto>();

        foreach (var header in headers)
        {
            var normalizedHeader = NormalizeHeader(header);
            var targetField = FindMatchingField(normalizedHeader);

            mappings.Add(new ColumnMappingDto
            {
                FileColumn = header,
                TargetField = targetField
            });
        }

        return mappings;
    }

    private string NormalizeHeader(string header)
    {
        return header
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }

    private string? FindMatchingField(string normalizedHeader)
    {
        foreach (var (field, aliases) in FieldAliases)
        {
            if (aliases.Contains(normalizedHeader))
            {
                return field;
            }
        }
        return null;
    }

    private Person? CreatePersonFromRow(List<string> cellsInRow, List<string> headers, List<ColumnMappingDto> mappings,
        Guid electionGuid, HashSet<string> bahaiIdLookup, HashSet<string> emailLookup, HashSet<string> phoneLookup, HashSet<string> nameLookup,
        int rowNumber, ImportPeopleResult result)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            CanVote = true, // Default to eligible
            CanReceiveVotes = true // Default to eligible
        };

        // Apply mappings
        foreach (var mapping in mappings.Where(m => !string.IsNullOrEmpty(m.TargetField)))
        {
            var columnIndex = headers.IndexOf(mapping.FileColumn);
            if (columnIndex >= 0 && columnIndex < cellsInRow.Count)
            {
                var value = cellsInRow[columnIndex]?.Trim();
                ApplyFieldMapping(person, mapping.TargetField!, value);
            }
        }

        var foundErrors = false;

        // Validate required fields

        if (string.IsNullOrEmpty(person.LastName))
        {
            result.PeopleSkipped++;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.missingLastName",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString()
                }
            });
            foundErrors = true;
        }

        if (string.IsNullOrEmpty(person.FirstName))
        {
            result.PeopleSkipped++;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.missingFirstName",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString()
                }
            });
            foundErrors = true;
        }

        if (!string.IsNullOrEmpty(person.BahaiId))
        {
            if (bahaiIdLookup.Contains(person.BahaiId))
            {
                result.PeopleSkipped++;
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.duplicateBahaiId",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["bahaiId"] = person.BahaiId
                    }
                });
                foundErrors = true;
            }
            else
            {
                // add it
                bahaiIdLookup.Add(person.BahaiId);
            }
        }


        if (!string.IsNullOrEmpty(person.Email))
        {
            if (emailLookup.Contains(person.Email))
            {
                result.PeopleSkipped++;
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.duplicateEmail",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["email"] = person.Email
                    }
                });
                foundErrors = true;
            }
            else
            {
                // add it
                emailLookup.Add(person.Email);
            }
        }


        if (!string.IsNullOrEmpty(person.Phone))
        {
            if (phoneLookup.Contains(person.Phone))
            {
                result.PeopleSkipped++;
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.duplicatePhone",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["phone"] = person.Phone
                    }
                });
                foundErrors = true;
            }
            else
            {
                // add it
                phoneLookup.Add(person.Phone);
            }
        }


        var nameKey = $"{person.FirstName ?? ""} {person.LastName}".Trim().ToLowerInvariant();
        if (nameLookup.Contains(nameKey))
        {
            result.PeopleSkipped++;
            result.Warnings.Add(new ImportWarningDto
            {
                Key = "import.errors.duplicateName",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString(),
                    ["firstName"] = person.FirstName ?? "",
                    ["lastName"] = person.LastName ?? ""
                }
            });
            foundErrors = true;
        }
        else
        {
            // add it
            nameLookup.Add(nameKey);
        }


        // Set eligibility
        var ineligibleReasonMapping = mappings.FirstOrDefault(m => m.TargetField == "IneligibleReasonDescription");
        if (ineligibleReasonMapping != null)
        {
            var columnIndex = headers.IndexOf(ineligibleReasonMapping.FileColumn);
            if (columnIndex >= 0 && columnIndex < cellsInRow.Count)
            {
                var eligibilityValue = cellsInRow[columnIndex]?.Trim();
                SetEligibility(person, eligibilityValue, result, rowNumber);
            }
        }

        if (foundErrors)
        {
            return null; // Skip this person due to validation errors
        }

        // Set computed fields
        person.FullName = PersonNameHelper.ComputeFullName(person);
        person.FullNameFl = PersonNameHelper.ComputeFullNameFl(person);

        return person;
    }

    private void ApplyFieldMapping(Person person, string targetField, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        switch (targetField)
        {
            case "FirstName":
                person.FirstName = value;
                break;
            case "LastName":
                person.LastName = value;
                break;
            case "BahaiId":
                person.BahaiId = value;
                break;
            case "Area":
                person.Area = value;
                break;
            case "Email":
                person.Email = value;
                break;
            case "Phone":
                person.Phone = value;
                break;
            case "OtherNames":
                person.OtherNames = value;
                break;
            case "OtherLastNames":
                person.OtherLastNames = value;
                break;
            case "OtherInfo":
                person.OtherInfo = value;
                break;
        }
    }

    private void SetEligibility(Person person, string? eligibilityValue, ImportPeopleResult result, int rowNumber)
    {
        if (string.IsNullOrEmpty(eligibilityValue))
        {
            person.CanVote = true;
            person.CanReceiveVotes = true;
            person.IneligibleReasonGuid = null;
            return;
        }

        var reason = IneligibleReasonEnum.GetByDescription(eligibilityValue) ??
                    IneligibleReasonEnum.GetByCode(eligibilityValue);

        if (reason != null)
        {
            person.CanVote = reason.CanVote;
            person.CanReceiveVotes = reason.CanReceiveVotes;
            person.IneligibleReasonGuid = reason.ReasonGuid;
        }
        else
        {
            // Unrecognized eligibility value - treat as eligible but warn
            person.CanVote = true;
            person.CanReceiveVotes = true;
            person.IneligibleReasonGuid = null;
            result.Warnings.Add(new ImportWarningDto
            {
                Key = "import.warnings.unrecognizedEligibility",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString(),
                    ["eligibilityValue"] = eligibilityValue
                }
            });
        }
    }

    private async Task ReportProgress(string groupName, int processed, int total, string status)
    {
        await _hubContext.Clients.Group(groupName).SendAsync("importProgress", processed, total, status);
    }

    private static string GetGroupName(Guid electionGuid) => $"PeopleImport{electionGuid}";
}