using Backend.Domain.Enumerations;

namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Data transfer object containing detailed information about an election for super admin dashboard.
/// </summary>
public class SuperAdminElectionDetailDto
{
    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The convenor of the election.
    /// </summary>
    public string? Convenor { get; set; }

    /// <summary>
    /// The date when the election is scheduled to occur.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The current tally status of the election.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// The type of election.
    /// </summary>
    public ElectionTypeCode? ElectionType { get; set; }

    /// <summary>
    /// The total number of voters in the election.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// The total number of ballots in the election.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// The total number of locations for the election.
    /// </summary>
    public int LocationCount { get; set; }

    /// <summary>
    /// The email address of the election owner.
    /// </summary>
    public string? OwnerEmail { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int? NumberToElect { get; set; }

    /// <summary>
    /// The mode of the election (online, in-person, etc.).
    /// </summary>
    public ElectionModeCode? ElectionMode { get; set; }

    /// <summary>
    /// The percentage of completion for the election tally process.
    /// </summary>
    public double PercentComplete { get; set; }

    /// <summary>
    /// List of owners and their roles for this election.
    /// </summary>
    public List<SuperAdminElectionOwnerDto> Owners { get; set; } = new();
}

/// <summary>
/// Data transfer object representing an election owner with their role information.
/// </summary>
public class SuperAdminElectionOwnerDto
{
    /// <summary>
    /// The email address of the election owner.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The display name of the election owner.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The role of the owner in the election (e.g., Owner, Teller).
    /// </summary>
    public string? Role { get; set; }
}



