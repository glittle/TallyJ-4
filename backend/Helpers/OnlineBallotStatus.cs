namespace Backend.Helpers;

/// <summary>
/// Status values stored on <c>OnlineVotingInfo.Status</c> (varchar(10)).
/// This flow persists only Submitted and Processed — there is no stored Processing
/// or Draft value. Accept-all claims a row by compare-and-swap from Submitted to
/// Processed in the same transaction that creates (or unlinks) the regular ballot.
/// </summary>
public static class OnlineBallotStatus
{
    public const string Submitted = "Submitted";
    public const string Processed = "Processed";
}
