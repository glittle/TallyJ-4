using Backend.Domain.Enumerations;

namespace Backend.DTOs.Public;

/// <summary>
/// Data transfer object representing an election available for public access.
/// </summary>
public class AvailableElectionDto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTimeOffset? DateOfElection { get; set; }

    /// <summary>
    /// The type of election (LSA, LSA1, LSA2, NSA, Con, Reg, Oth).
    /// </summary>
    public ElectionTypeCode? ElectionType { get; set; }
}



