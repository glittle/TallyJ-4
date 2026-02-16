namespace Backend.DTOs.Results;

/// <summary>
/// Configuration for custom reports
/// </summary>
public class CustomReportConfigDto
{
    /// <summary>
    /// The name of the custom report.
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// A description of the custom report.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The sections that make up the custom report.
    /// </summary>
    public List<ReportSectionDto> Sections { get; set; } = new();

    /// <summary>
    /// Default filters to apply to the report data.
    /// </summary>
    public AdvancedFilterDto? DefaultFilters { get; set; }

    /// <summary>
    /// Supported export formats for the report.
    /// </summary>
    public List<string> ExportFormats { get; set; } = new() { "pdf", "excel", "csv" };

    /// <summary>
    /// Whether the report is publicly accessible.
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// The date and time when the report configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the report configuration was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Represents a section within a custom report.
/// </summary>
public class ReportSectionDto
{
    /// <summary>
    /// The type of section (summary, candidates, locations, chart, statistics).
    /// </summary>
    public string SectionType { get; set; } = string.Empty; // "summary", "candidates", "locations", "chart", "statistics"

    /// <summary>
    /// The title of the report section.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Parameters specific to this section type.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// The order of this section within the report.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Represents a generated custom report.
/// </summary>
public class CustomReportDto
{
    /// <summary>
    /// The unique identifier for this report instance.
    /// </summary>
    public Guid ReportGuid { get; set; }

    /// <summary>
    /// The configuration used to generate this report.
    /// </summary>
    public CustomReportConfigDto Config { get; set; } = new();

    /// <summary>
    /// The generated report data organized by section or data type.
    /// </summary>
    public Dictionary<string, object> GeneratedData { get; set; } = new();

    /// <summary>
    /// The date and time when this report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}


