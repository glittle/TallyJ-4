namespace Backend.DTOs.Elections;

/// <summary>
/// Updates only the online voting window settings for an election
/// (open/close times and whether the close time is an estimate).
/// </summary>
public class UpdateOnlineVotingWindowDto
{
    /// <summary>
    /// When online voting opens (null clears the open time).
    /// </summary>
    public DateTimeOffset? OnlineWhenOpen { get; set; }

    /// <summary>
    /// When online voting closes (null clears the close time).
    /// </summary>
    public DateTimeOffset? OnlineWhenClose { get; set; }

    /// <summary>
    /// Whether the online close time is an estimate rather than a firm deadline.
    /// </summary>
    public bool OnlineCloseIsEstimate { get; set; }
}
