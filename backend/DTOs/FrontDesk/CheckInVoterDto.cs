using System.ComponentModel.DataAnnotations;

namespace TallyJ4.DTOs.FrontDesk;

/// <summary>
/// Data transfer object for checking in a voter at the front desk.
/// </summary>
public class CheckInVoterDto
{
    /// <summary>
    /// The unique identifier of the person being checked in.
    /// </summary>
    [Required]
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The voting method (e.g., 'P' for paper, 'O' for online).
    /// </summary>
    [Required]
    [StringLength(1)]
    public string VotingMethod { get; set; } = null!;

    /// <summary>
    /// The name of the teller checking in the voter.
    /// </summary>
    [StringLength(25)]
    public string? TellerName { get; set; }

    /// <summary>
    /// The unique identifier of the voting location.
    /// </summary>
    public Guid? VotingLocationGuid { get; set; }
}
