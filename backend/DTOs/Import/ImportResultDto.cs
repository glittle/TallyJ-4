namespace TallyJ4.DTOs.Import;

/// <summary>
/// Data transfer object containing the result of an import operation.
/// </summary>
public class ImportResultDto
{
    /// <summary>
    /// Indicates whether the import operation was successful.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Total number of rows processed.
    /// </summary>
    public int TotalRows { get; set; }
    
    /// <summary>
    /// Number of ballots created during import.
    /// </summary>
    public int BallotsCreated { get; set; }
    
    /// <summary>
    /// Number of votes created during import.
    /// </summary>
    public int VotesCreated { get; set; }
    
    /// <summary>
    /// Number of rows skipped during import.
    /// </summary>
    public int SkippedRows { get; set; }
    
    /// <summary>
    /// List of error messages encountered during import.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// List of warning messages generated during import.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
