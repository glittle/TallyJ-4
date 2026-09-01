namespace Backend.Helpers;

/// <summary>
/// Status values on <c>OnlineVotingInfo.Status</c> (varchar(10)).
/// Submitted is pending teller Accept-all. Processed means a regular ballot was created
/// and the online payload was wiped; the voter cannot change that vote.
/// </summary>
public static class OnlineBallotStatus
{
    public const string Submitted = "Submitted";
    public const string Processed = "Processed";
}
