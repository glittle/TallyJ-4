namespace Backend.DTOs.FrontDesk;

/// <summary>
/// Data transfer object representing a voter at the front desk.
/// </summary>
public class FrontDeskVoterDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The full name of the voter.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// The BahÃ¡'Ã­ ID of the voter.
    /// </summary>
    public string? BahaiId { get; set; }

    /// <summary>
    /// The area where the voter is registered.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Indicates whether the voter is eligible to vote.
    /// </summary>
    public bool? CanVote { get; set; }

    /// <summary>
    /// The voting method (e.g., 'P' for paper, 'O' for online).
    /// </summary>
    public string? VotingMethod { get; set; }

    /// <summary>
    /// The envelope number assigned to the voter.
    /// </summary>
    public int? EnvNum { get; set; }

    /// <summary>
    /// The timestamp when the voter was registered.
    /// </summary>
    public DateTime? RegistrationTime { get; set; }

    /// <summary>
    /// The unique identifier of the voting location.
    /// </summary>
    public Guid? VotingLocationGuid { get; set; }

    /// <summary>
    /// The name of the first teller.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller.
    /// </summary>
    public string? Teller2 { get; set; }

    /// <summary>
    /// Indicates whether the voter has checked in.
    /// </summary>
    public bool IsCheckedIn => RegistrationTime.HasValue;

    /// <summary>
    /// Flags/labels assigned to this voter (comma-separated).
    /// </summary>
    public string? Flags { get; set; }

    /// <summary>
    /// Registration history entries for this voter.
    /// </summary>
    public List<RegistrationHistoryEntryDto>? RegistrationHistory { get; set; }
}



