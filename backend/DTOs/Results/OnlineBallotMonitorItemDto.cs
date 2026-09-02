namespace Backend.DTOs.Results;

/// <summary>
/// One <c>OnlineVotingInfo</c> row for the monitor pending / accepted lists.
/// Person name and <c>WhenStatus</c> only — the vote payload is wiped on accept
/// and this row is not linked to the regular <c>Ballot</c>.
/// </summary>
public class OnlineBallotMonitorItemDto
{
    /// <summary>
    /// <c>OnlineVotingInfo</c> row id (list key). Not a voter contact id.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// Display name from the Person row (Last, First). Never email, phone, or kiosk.
    /// </summary>
    public string PersonName { get; set; } = string.Empty;

    /// <summary>
    /// Stored <c>OnlineVotingInfo.Status</c>: Submitted, Processing, or Processed.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When this status was last written (submit, claim, or accept).
    /// </summary>
    public DateTimeOffset? WhenStatus { get; set; }
}
