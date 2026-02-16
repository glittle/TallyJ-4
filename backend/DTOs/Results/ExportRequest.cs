namespace Backend.DTOs.Results;

/// <summary>
/// Request DTO for exporting election reports in different formats.
/// </summary>
public class ExportRequest
{
    /// <summary>
    /// The format of the exported report (PDF, Excel, or CSV).
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the election to export.
    /// </summary>
    public Guid ElectionId { get; set; }

    /// <summary>
    /// Optional filters to apply to the export (e.g., date ranges, locations).
    /// </summary>
    public Dictionary<string, string>? Filters { get; set; }
}


