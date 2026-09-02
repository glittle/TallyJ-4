namespace Backend.Helpers;

/// <summary>
/// Status values stored on <c>OnlineVotingInfo.Status</c> (varchar(10)).
/// Submitted = pending. Processing = claimed by an Accept-all run (persisted so
/// another server can see the claim). Processed = regular ballot created (or a
/// legacy row unlinked) and the online payload wiped. There is no Draft value.
/// "Processing" is 10 characters and fits the column.
/// </summary>
public static class OnlineBallotStatus
{
    public const string Submitted = "Submitted";
    public const string Processing = "Processing";
    public const string Processed = "Processed";

    public static bool IsSubmitted(string? status) =>
        string.Equals(status, Submitted, StringComparison.OrdinalIgnoreCase);

    public static bool IsProcessing(string? status) =>
        string.Equals(status, Processing, StringComparison.OrdinalIgnoreCase);

    public static bool IsProcessed(string? status) =>
        string.Equals(status, Processed, StringComparison.OrdinalIgnoreCase);
}
