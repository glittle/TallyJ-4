using System;
using System.Collections.Generic;

namespace Backend.DTOs.Import;

/// <summary>
/// Data transfer object for import file information, mapping from ImportFile entity (excluding Contents).
/// </summary>
public class ImportFileDto
{
    /// <summary>
    /// The unique row identifier for the import file.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// The GUID of the election this file belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The time when the file was uploaded.
    /// </summary>
    public DateTime? UploadTime { get; set; }

    /// <summary>
    /// The time when the import was completed.
    /// </summary>
    public DateTime? ImportTime { get; set; }

    /// <summary>
    /// The size of the uploaded file in bytes.
    /// </summary>
    public int? FileSize { get; set; }

    /// <summary>
    /// Indicates whether the file has content stored.
    /// </summary>
    public bool? HasContent { get; set; }

    /// <summary>
    /// The row number where data starts (1-based).
    /// </summary>
    public int? FirstDataRow { get; set; }

    /// <summary>
    /// JSON string containing column mappings.
    /// </summary>
    public string? ColumnsToRead { get; set; }

    /// <summary>
    /// The original name of the uploaded file.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// The current processing status of the file.
    /// </summary>
    public string? ProcessingStatus { get; set; }

    /// <summary>
    /// The type of the file (e.g., csv, xlsx).
    /// </summary>
    public string? FileType { get; set; }

    /// <summary>
    /// The code page for text encoding.
    /// </summary>
    public int? CodePage { get; set; }

    /// <summary>
    /// Any messages or errors related to the file processing.
    /// </summary>
    public string? Messages { get; set; }
}

/// <summary>
/// Data transfer object for the response when parsing a file.
/// </summary>
public class ParseFileResponse
{
    /// <summary>
    /// List of column headers from the parsed file.
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Preview rows from the file (first few data rows).
    /// </summary>
    public List<List<string>> PreviewRows { get; set; } = new();

    /// <summary>
    /// Total number of data rows in the file.
    /// </summary>
    public int TotalDataRows { get; set; }

    /// <summary>
    /// Automatically detected column mappings.
    /// </summary>
    public List<ColumnMappingDto> AutoMappings { get; set; } = new();
}

/// <summary>
/// Data transfer object for column mapping configuration.
/// </summary>
public class ColumnMappingDto
{
    /// <summary>
    /// The name of the column in the source file.
    /// </summary>
    public string FileColumn { get; set; } = null!;

    /// <summary>
    /// The target field name in TallyJ, or null to ignore this column.
    /// </summary>
    public string? TargetField { get; set; }
}

/// <summary>
/// Data transfer object for updating file settings.
/// </summary>
public class UpdateFileSettingsDto
{
    /// <summary>
    /// The row number where data starts (1-based).
    /// </summary>
    public int? FirstDataRow { get; set; }

    /// <summary>
    /// The code page for text encoding.
    /// </summary>
    public int? CodePage { get; set; }
}

/// <summary>
/// Data transfer object containing the result of importing people.
/// </summary>
public class ImportPeopleResult
{
    /// <summary>
    /// Indicates whether the import operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of people successfully added.
    /// </summary>
    public int PeopleAdded { get; set; }

    /// <summary>
    /// Number of people skipped (duplicates or invalid).
    /// </summary>
    public int PeopleSkipped { get; set; }

    /// <summary>
    /// Total number of rows processed.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// List of warning messages generated during import.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// List of error messages encountered during import.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Time elapsed during the import operation in seconds.
    /// </summary>
    public double TimeElapsedSeconds { get; set; }
}

/// <summary>
/// Data transfer object containing the result of deleting all people.
/// </summary>
public class DeleteAllPeopleResult
{
    /// <summary>
    /// Number of people records deleted.
    /// </summary>
    public int DeletedCount { get; set; }
}