namespace TallyJ4.DTOs.Import;

/// <summary>
/// Data transfer object for mapping a source column to a target field during import.
/// </summary>
public class FieldMappingDto
{
    /// <summary>
    /// The name of the source column in the CSV file.
    /// </summary>
    public string SourceColumn { get; set; } = null!;
    
    /// <summary>
    /// The name of the target field in the database.
    /// </summary>
    public string TargetField { get; set; } = null!;
}
