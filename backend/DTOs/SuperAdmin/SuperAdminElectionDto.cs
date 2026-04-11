using Backend.Domain.Enumerations;

namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Data transfer object containing summary information about an election for super admin dashboard.
/// </summary>
public class SuperAdminElectionDto
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
    public DateTimeOffset? DateOfElection { get; set; }

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
}



