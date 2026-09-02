namespace Backend.DTOs.Results;

/// <summary>
/// One successful Accept-all run: who accepted, when, and pending / accepted
/// counts before and after that run. Failed or overlapping (409) runs are not stored.
/// </summary>
public class AcceptAllOnlineBallotsRunDto
{
    public DateTimeOffset When { get; set; }

    /// <summary>
    /// Logged-in teller user id (JWT sub). Not a voter id.
    /// </summary>
    public string? AcceptedByUserId { get; set; }

    /// <summary>
    /// Teller display name when the account has one. Never voter contact details.
    /// </summary>
    public string? AcceptedBy { get; set; }

    public int PendingBefore { get; set; }

    public int AcceptedBefore { get; set; }

    public int PendingAfter { get; set; }

    public int AcceptedAfter { get; set; }
}
