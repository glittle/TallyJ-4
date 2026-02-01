namespace TallyJ4.DTOs.Import;

public class ImportBallotRequestDto
{
    public string CsvContent { get; set; } = null!;
    
    public Guid ElectionGuid { get; set; }
    
    public Guid? LocationGuid { get; set; }
    
    public ImportConfigurationDto Configuration { get; set; } = new();
}
