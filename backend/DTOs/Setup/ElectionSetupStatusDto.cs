namespace TallyJ4.DTOs.Setup;

public class ElectionSetupStatusDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TallyStatus { get; set; } = string.Empty;
    public bool Step1Complete { get; set; }
    public bool Step2Complete { get; set; }
    public int ProgressPercent { get; set; }
}
