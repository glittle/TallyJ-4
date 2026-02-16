namespace Backend.DTOs.SignalR;

/// <summary>
/// Data transfer object for import progress notifications via SignalR.
/// </summary>
public class ImportProgressDto
{
    /// <summary>
    /// The GUID of the election being imported into.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The total number of rows to be imported.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// The number of rows that have been processed so far.
    /// </summary>
    public int ProcessedRows { get; set; }

    /// <summary>
    /// The number of rows that were successfully imported.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// The number of rows that failed to import.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// The current status message of the import process.
    /// </summary>
    public string CurrentStatus { get; set; } = string.Empty;

    /// <summary>
    /// The percentage of completion (0-100).
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Whether the import process has completed.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// List of error messages encountered during import.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}



