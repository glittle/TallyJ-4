namespace TallyJ4.DTOs.Setup;

public class ElectionStep2Dto
{
    public Guid ElectionGuid { get; set; }
    public int NumberToElect { get; set; }
    public string ElectionType { get; set; } = string.Empty;
    public string ElectionMode { get; set; } = string.Empty;
}
