namespace TallyJ4.DTOs.SignalR;

public class ElectionUpdateDto
{
    public Guid ElectionGuid { get; set; }
    public string? Name { get; set; }
    public string? TallyStatus { get; set; }
    public string? ElectionStatus { get; set; }
    public DateTime UpdatedAt { get; set; }
}
