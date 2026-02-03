namespace TallyJ4.DTOs.Import;

public class ImportResultDto
{
    public bool Success { get; set; }
    
    public int TotalRows { get; set; }
    
    public int BallotsCreated { get; set; }
    
    public int VotesCreated { get; set; }
    
    public int SkippedRows { get; set; }
    
    public List<string> Errors { get; set; } = new();
    
    public List<string> Warnings { get; set; } = new();
}
