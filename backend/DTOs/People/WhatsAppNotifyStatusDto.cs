namespace Backend.DTOs.People;

/// <summary>
/// Per-run summary of a head-teller WhatsApp notify queue (v3 SendHeadTellerMessage shape).
/// No live SignalR progress.
/// </summary>
public class WhatsAppNotifyStatusDto
{
    /// <summary>
    /// Token returned by start. Abort may use this or the election's active run.
    /// </summary>
    public string QueueToken { get; set; } = string.Empty;

    /// <summary>
    /// True while background sends are still in progress.
    /// </summary>
    public bool Running { get; set; }

    /// <summary>
    /// True when abort stopped remaining sends.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// People queued for GreenAPI <c>sendMessage</c> (known-OK P-row phones).
    /// </summary>
    public int Queued { get; set; }

    /// <summary>
    /// Successful GreenAPI sends. Already-sent stay sent after abort.
    /// </summary>
    public int Sent { get; set; }

    /// <summary>
    /// Unchecked / no-wa / check-failed / no-phone / non-P / other-election.
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Provider was called and the send failed, or the paid-send guard blocked the attempt.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// This retry included only the first 100 failed people. Another retry sends the rest.
    /// </summary>
    public bool MoreFailedRemain { get; set; }

    /// <summary>
    /// Per-person outcomes in request order (duplicates removed).
    /// </summary>
    public List<WhatsAppNotifyPersonResultDto> Results { get; set; } = [];
}
