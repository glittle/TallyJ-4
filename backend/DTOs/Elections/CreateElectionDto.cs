namespace TallyJ4.DTOs.Elections;

public class CreateElectionDto
{
    public string Name { get; set; } = null!;
    public DateTime? DateOfElection { get; set; }
    public string? ElectionType { get; set; }
    public int? NumberToElect { get; set; }
    public string? Convenor { get; set; }
    public string? ElectionMode { get; set; }
    public int? NumberExtra { get; set; }
    public bool? ShowFullReport { get; set; }
    public bool? ListForPublic { get; set; }
    public bool? ShowAsTest { get; set; }
}
