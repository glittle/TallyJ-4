namespace Backend.DTOs.People;

/// <summary>
/// Abort the active WhatsApp notify queue for this election.
/// When <see cref="QueueToken"/> is set, only that run is aborted.
/// </summary>
public class AbortWhatsAppNotifyDto
{
    /// <summary>
    /// Optional token from start. When omitted, abort the election's active run.
    /// </summary>
    public string? QueueToken { get; set; }
}
