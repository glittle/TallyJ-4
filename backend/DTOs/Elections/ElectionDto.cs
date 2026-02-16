using Backend.Domain.Enumerations;

namespace Backend.DTOs.Elections;

/// <summary>
/// Data transfer object representing an election with its configuration and statistics.
/// </summary>
public class ElectionDto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The type of election (LSA, LSA1, LSA2, NSA, Con, Reg, Oth).
    /// </summary>
    public ElectionTypeCode? ElectionType { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int? NumberToElect { get; set; }

    /// <summary>
    /// The current tally status of the election.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// The name of the election convenor.
    /// </summary>
    public string? Convenor { get; set; }

    /// <summary>
    /// The mode of the election (N=Normal, T=Tie-Break, B=By-election).
    /// </summary>
    public ElectionModeCode? ElectionMode { get; set; }

    /// <summary>
    /// The number of extra positions beyond the required number.
    /// </summary>
    public int? NumberExtra { get; set; }

    /// <summary>
    /// Whether to show the full election report.
    /// </summary>
    public bool? ShowFullReport { get; set; }

    /// <summary>
    /// Whether to list this election for public access.
    /// </summary>
    public bool? ListForPublic { get; set; }

    /// <summary>
    /// Whether to mark this election as a test election.
    /// </summary>
    public bool? ShowAsTest { get; set; }

    /// <summary>
    /// The date and time when online voting opens.
    /// </summary>
    public DateTime? OnlineWhenOpen { get; set; }

    /// <summary>
    /// The date and time when online voting closes.
    /// </summary>
    public DateTime? OnlineWhenClose { get; set; }

    /// <summary>
    /// The total number of registered voters.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// The total number of ballots cast.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// The number of voting locations.
    /// </summary>
    public int LocationCount { get; set; }

    /// <summary>
    /// Default eligibility to vote (Y/N/?).
    /// </summary>
    public string? CanVote { get; set; }

    /// <summary>
    /// Default eligibility to receive votes (Y/N/?).
    /// </summary>
    public string? CanReceive { get; set; }

    /// <summary>
    /// The passcode required for teller access.
    /// </summary>
    public string? ElectionPasscode { get; set; }

    /// <summary>
    /// Linked election GUID for tie-breaks.
    /// </summary>
    public Guid? LinkedElectionGuid { get; set; }

    /// <summary>
    /// Type of linked election (e.g., "TB" for tie-break).
    /// </summary>
    public string? LinkedElectionKind { get; set; }

    /// <summary>
    /// Whether to use the call-in button feature.
    /// </summary>
    public bool? UseCallInButton { get; set; }

    /// <summary>
    /// Whether to hide pre-ballot pages.
    /// </summary>
    public bool? HidePreBallotPages { get; set; }

    /// <summary>
    /// Whether to mask the voting method in displays.
    /// </summary>
    public bool? MaskVotingMethod { get; set; }

    /// <summary>
    /// Whether the online close time is an estimate.
    /// </summary>
    public bool? OnlineCloseIsEstimate { get; set; }

    /// <summary>
    /// Online voting selection process (e.g., "S" for simultaneous).
    /// </summary>
    public string? OnlineSelectionProcess { get; set; }

    /// <summary>
    /// The date and time when online voting was announced.
    /// </summary>
    public DateTime? OnlineAnnounced { get; set; }

    /// <summary>
    /// Email from address for voter communications.
    /// </summary>
    public string? EmailFromAddress { get; set; }

    /// <summary>
    /// Email from name for voter communications.
    /// </summary>
    public string? EmailFromName { get; set; }

    /// <summary>
    /// Email template text for voter invitations.
    /// </summary>
    public string? EmailText { get; set; }

    /// <summary>
    /// Email subject line for voter communications.
    /// </summary>
    public string? EmailSubject { get; set; }

    /// <summary>
    /// SMS template text for voter invitations.
    /// </summary>
    public string? SmsText { get; set; }

    /// <summary>
    /// Custom voting methods (comma-separated).
    /// </summary>
    public string? CustomMethods { get; set; }

    /// <summary>
    /// Voting methods available (encoded string).
    /// </summary>
    public string? VotingMethods { get; set; }

    /// <summary>
    /// Additional flags and settings (JSON).
    /// </summary>
    public string? Flags { get; set; }
}



