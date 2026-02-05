namespace TallyJ4.DTOs.Import;

/// <summary>
/// Data transfer object containing the parsed CSV headers and preview data.
/// </summary>
public class ParseCsvHeadersResponseDto
{
    /// <summary>
    /// List of column headers from the CSV file.
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Preview rows from the CSV file.
    /// </summary>
    public List<string[]> PreviewRows { get; set; } = new();

    /// <summary>
    /// Total number of data rows in the CSV file.
    /// </summary>
    public int TotalRows { get; set; }
}
