namespace TallyJ4.DTOs.Elections;

public class ElectionDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = null!;
    public DateTime? DateOfElection { get; set; }
    public string? ElectionType { get; set; }
    public int? NumberToElect { get; set; }
    public string? TallyStatus { get; set; }
    public string? Convenor { get; set; }
    public string? ElectionMode { get; set; }
    public int? NumberExtra { get; set; }
    public bool? ShowFullReport { get; set; }
    public bool? ListForPublic { get; set; }
    public bool? ShowAsTest { get; set; }
    public DateTime? OnlineWhenOpen { get; set; }
    public DateTime? OnlineWhenClose { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
    public int LocationCount { get; set; }
}
