using TallyJ4.Domain.Enumerations;

namespace TallyJ4.DTOs.SuperAdmin;

public class SuperAdminElectionDetailDto
{
    public Guid ElectionGuid { get; set; }
    public string Name { get; set; } = null!;
    public string? Convenor { get; set; }
    public DateTime? DateOfElection { get; set; }
    public string? TallyStatus { get; set; }
    public ElectionTypeCode? ElectionType { get; set; }
    public int VoterCount { get; set; }
    public int BallotCount { get; set; }
    public int LocationCount { get; set; }
    public string? OwnerEmail { get; set; }
    public int? NumberToElect { get; set; }
    public ElectionModeCode? ElectionMode { get; set; }
    public double PercentComplete { get; set; }
    public List<SuperAdminElectionOwnerDto> Owners { get; set; } = new();
}

public class SuperAdminElectionOwnerDto
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
}
