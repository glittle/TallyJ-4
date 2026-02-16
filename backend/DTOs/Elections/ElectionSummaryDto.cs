using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enumerations;

namespace Backend.DTOs.Elections;

/// <summary>
/// Data transfer object representing a summary of election information.
/// </summary>
public class ElectionSummaryDto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    [Required]
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The type of the election.
    /// </summary>
    public ElectionTypeCode? ElectionType { get; set; }

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The current tally status of the election.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// The total number of registered voters.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// The total number of ballots cast.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// The mode of the election (N=Normal, T=Tie-Break, B=By-election).
    /// </summary>
    public ElectionModeCode? ElectionMode { get; set; }

}



