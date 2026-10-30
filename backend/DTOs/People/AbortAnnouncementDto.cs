namespace Backend.DTOs.People;

/// <summary>
/// Abort the active email or SMS announcement for this election.
/// When <see cref="QueueToken"/> is set, only that run is aborted.
/// </summary>
public class AbortAnnouncementDto
{
    /// <summary>
    /// <c>email</c> or <c>sms</c>.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Optional token from start. When omitted, abort the channel's active run.
    /// </summary>
    public string? QueueToken { get; set; }
}
