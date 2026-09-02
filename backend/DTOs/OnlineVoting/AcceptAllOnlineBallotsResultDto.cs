namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Outcome of one Accept-all run. Only pending (Submitted) online ballots at the
/// start of the run are accepted. The online voting window may stay open.
/// </summary>
public class AcceptAllOnlineBallotsResultDto
{
    public bool Success { get; set; }

    /// <summary>
    /// True when another Accept-all for this election is already running.
    /// </summary>
    public bool AlreadyInProgress { get; set; }

    /// <summary>
    /// Regular ballots created (or legacy submitted ballots marked processed) in this run.
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Expected rows skipped because they were no longer Processing when taken
    /// (another server already processed them).
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Submitted rows still pending after this run (usually 0 unless new votes arrived).
    /// </summary>
    public int PendingRemaining { get; set; }

    /// <summary>
    /// i18n key for the teller-facing message.
    /// </summary>
    public string? MessageKey { get; set; }
}
