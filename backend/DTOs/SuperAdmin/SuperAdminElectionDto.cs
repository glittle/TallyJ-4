namespace TallyJ4.DTOs.SuperAdmin;

public class SuperAdminElectionDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = null!;
    public string? Convenor { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string? TallyStatus { get; set; }
    public string? ElectionType { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
    public int LocationCount { get; set; }
    public string? OwnerEmail { get; set; }
}
