namespace TallyJ4.DTOs.Public;

public class PublicHomeDto
{
    public string ApplicationName { get; set; } = "TallyJ 4";
    public string Version { get; set; } = "4.0.0";
    public string Description { get; set; } = "Election management and online voting system";
    public int AvailableElectionsCount { get; set; }
    public DateTime ServerTime { get; set; }
}
