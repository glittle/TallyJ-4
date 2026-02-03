namespace TallyJ4.DTOs.Import;

public class ImportConfigurationDto
{
    public int FirstDataRow { get; set; } = 2;
    
    public bool HasHeaderRow { get; set; } = true;
    
    public string Delimiter { get; set; } = ",";
    
    public List<FieldMappingDto> FieldMappings { get; set; } = new();
    
    public bool SkipInvalidRows { get; set; } = true;
}
