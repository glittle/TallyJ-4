namespace TallyJ4.DTOs.SignalR;

public class PersonUpdateDto
{
    public Guid ElectionGuid { get; set; }
    public Guid PersonGuid { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime UpdatedAt { get; set; }
}
