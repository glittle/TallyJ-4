namespace Backend.DTOs.Results;

/// <summary>
/// v3 Analyze count table: calculated, optional manual override, and final.
/// Null on a manual field means “use calculated” (no override).
/// </summary>
public class AnalyzeCountSummariesDto
{
    public AnalyzeCountRowDto Calculated { get; set; } = new();
    public AnalyzeCountRowDto Manual { get; set; } = new();
    public AnalyzeCountRowDto Final { get; set; } = new();
}

/// <summary>
/// Overrideable Analyze counts. Same columns v3 SaveManual persisted
/// (Online and Imported stay calculated-only).
/// </summary>
public class AnalyzeCountRowDto
{
    public int? NumEligibleToVote { get; set; }
    public int? InPersonBallots { get; set; }
    public int? DroppedOffBallots { get; set; }
    public int? MailedInBallots { get; set; }
    public int? CalledInBallots { get; set; }
    public int? Custom1Ballots { get; set; }
    public int? Custom2Ballots { get; set; }
    public int? Custom3Ballots { get; set; }
    public int? SpoiledManualBallots { get; set; }
}
