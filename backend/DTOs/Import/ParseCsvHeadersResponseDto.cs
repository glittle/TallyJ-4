namespace TallyJ4.DTOs.Import;

public class ParseCsvHeadersResponseDto
{
    public List<string> Headers { get; set; } = new();
    
    public List<string[]> PreviewRows { get; set; } = new();
    
    public int TotalRows { get; set; }
}
