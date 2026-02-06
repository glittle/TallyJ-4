namespace TallyJ4.DTOs.Import;

/// <summary>
/// Data transfer object for configuring an import operation.
/// </summary>
public class ImportConfigurationDto
{
    /// <summary>
    /// The row number where data starts (1-based index).
    /// </summary>
    public int FirstDataRow { get; set; } = 2;

    /// <summary>
    /// Indicates whether the CSV file has a header row.
    /// </summary>
    public bool HasHeaderRow { get; set; } = true;

    /// <summary>
    /// The delimiter character used in the CSV file.
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// List of field mappings from source columns to target fields.
    /// </summary>
    public List<FieldMappingDto> FieldMappings { get; set; } = new();

    /// <summary>
    /// Indicates whether invalid rows should be skipped during import.
    /// </summary>
    public bool SkipInvalidRows { get; set; } = true;
}
