namespace Backend.DTOs.Results;

/// <summary>
/// Kinds of count-reconciliation mismatches the teller-facing report can list.
/// Names match what the evaluator actually emits.
/// </summary>
public static class CountReconciliationMismatchKinds
{
    /// <summary>
    /// Residual Front Desk registrations vs entered ballots (including spoiled).
    /// Paper and accepted-online ballots are anonymous, so this is a count row.
    /// </summary>
    public const string FrontDeskVsBallots = "FrontDeskVsBallots";

    /// <summary>
    /// An <c>OnlineVotingInfo</c> row still <c>Submitted</c> or <c>Processing</c>.
    /// </summary>
    public const string PendingOnline = "PendingOnline";

    /// <summary>
    /// Two or more people share the same envelope number.
    /// </summary>
    public const string DuplicateEnvelope = "DuplicateEnvelope";

    /// <summary>
    /// A person has a paper/imported Front Desk method and also an online vote
    /// (pending or Processed).
    /// </summary>
    public const string DuplicateVotingPath = "DuplicateVotingPath";
}

/// <summary>
/// One teller-visible mismatch row. May name a voter or a ballot.
/// </summary>
public class CountReconciliationMismatchDto
{
    public string Kind { get; set; } = string.Empty;

    public Guid? PersonGuid { get; set; }

    public string? PersonName { get; set; }

    public string? VotingMethod { get; set; }

    public int? EnvNum { get; set; }

    public string? OnlineStatus { get; set; }

    public Guid? BallotGuid { get; set; }

    public string? BallotCode { get; set; }

    public int? FrontDeskCount { get; set; }

    public int? BallotCount { get; set; }
}

/// <summary>
/// Live count-reconciliation report used before Analyze and Finalize.
/// </summary>
public class CountReconciliationReportDto
{
    public bool IsReconciled { get; set; }

    public int FrontDeskCount { get; set; }

    public int BallotCount { get; set; }

    public int PendingOnlineCount { get; set; }

    public int SpoiledBallotCount { get; set; }

    public IReadOnlyList<CountReconciliationMismatchDto> Mismatches { get; set; } = [];
}
