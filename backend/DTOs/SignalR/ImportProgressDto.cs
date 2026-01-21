namespace TallyJ4.DTOs.SignalR;

public class ImportProgressDto
{
    public Guid ElectionGuid { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public bool IsComplete { get; set; }
    public List<string> Errors { get; set; } = new();
}
