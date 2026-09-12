namespace Backend.Tests.V3AnalysisComparison;

/// <summary>
/// Stable, GUID-free snapshot of analysis output for v3 vs v4 diffs.
/// Person identity is last + first name (import remaps GUIDs).
/// </summary>
public sealed class AnalysisComparisonSnapshot
{
    public List<ResultSummarySnapshot> Summaries { get; set; } = [];
    public List<ResultTieSnapshot> Ties { get; set; } = [];
    public List<PersonCountSnapshot> Counts { get; set; } = [];
}

public sealed class ResultSummarySnapshot
{
    public string ResultType { get; set; } = "";
    public int? NumVoters { get; set; }
    public int? NumEligibleToVote { get; set; }
    public int? InPersonBallots { get; set; }
    public int? MailedInBallots { get; set; }
    public int? DroppedOffBallots { get; set; }
    public int? CalledInBallots { get; set; }
    public int? OnlineBallots { get; set; }
    public int? ImportedBallots { get; set; }
    public int? Custom1Ballots { get; set; }
    public int? Custom2Ballots { get; set; }
    public int? Custom3Ballots { get; set; }
    public int? SpoiledBallots { get; set; }
    public int? SpoiledVotes { get; set; }
    public int? SpoiledManualBallots { get; set; }
    public int? TotalVotes { get; set; }
    public int? BallotsReceived { get; set; }
    public int? BallotsNeedingReview { get; set; }
    public bool? UseOnReports { get; set; }
}

public sealed class ResultTieSnapshot
{
    public int TieBreakGroup { get; set; }
    public bool? TieBreakRequired { get; set; }
    public int NumToElect { get; set; }
    public int NumInTie { get; set; }
    public bool? IsResolved { get; set; }
}

public sealed class PersonCountSnapshot
{
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public int VoteCount { get; set; }
    public int Rank { get; set; }
    public string Section { get; set; } = "";
    public bool? IsTied { get; set; }
    public int? TieBreakGroup { get; set; }
    public bool? TieBreakRequired { get; set; }
    public int? RankInExtra { get; set; }
    public bool? ForceShowInOther { get; set; }
}
