namespace Backend.DTOs.SignalR;

/// <summary>
/// Thin SignalR payload for operator-facing online voting window / process changes
/// (FrontDeskHub <c>updateOnlineElection</c>). Clients re-apply these fields or re-fetch.
/// </summary>
public class OnlineElectionUpdateDto
{
    /// <summary>
    /// The election whose online settings changed.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// When online voting opens.
    /// </summary>
    public DateTimeOffset? OnlineWhenOpen { get; set; }

    /// <summary>
    /// When online voting closes.
    /// </summary>
    public DateTimeOffset? OnlineWhenClose { get; set; }

    /// <summary>
    /// Whether the close time is an estimate.
    /// </summary>
    public bool OnlineCloseIsEstimate { get; set; }

    /// <summary>
    /// Online ballot selection process code (e.g. simultaneous vs ranked).
    /// </summary>
    public string? OnlineSelectionProcess { get; set; }
}
