namespace TallyJ4.DTOs.Results;

/// <summary>
/// Configuration for custom reports
/// </summary>
public class CustomReportConfigDto
{
    public string ReportName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ReportSectionDto> Sections { get; set; } = new();
    public AdvancedFilterDto? DefaultFilters { get; set; }
    public List<string> ExportFormats { get; set; } = new() { "pdf", "excel", "csv" };
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class ReportSectionDto
{
    public string SectionType { get; set; } = string.Empty; // "summary", "candidates", "locations", "chart", "statistics"
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int Order { get; set; }
}

public class CustomReportDto
{
    public Guid ReportGuid { get; set; }
    public CustomReportConfigDto Config { get; set; } = new();
    public Dictionary<string, object> GeneratedData { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}