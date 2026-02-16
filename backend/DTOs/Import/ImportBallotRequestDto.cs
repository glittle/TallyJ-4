namespace Backend.DTOs.Import;

/// <summary>
/// Data transfer object for requesting a ballot import operation.
/// </summary>
public class ImportBallotRequestDto
{
    /// <summary>
    /// The CSV content to import.
    /// </summary>
    public string CsvContent { get; set; } = null!;

    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The unique identifier of the location (optional).
    /// </summary>
    public Guid? LocationGuid { get; set; }

    /// <summary>
    /// Configuration settings for the import operation.
    /// </summary>
    public ImportConfigurationDto Configuration { get; set; } = new();
}



