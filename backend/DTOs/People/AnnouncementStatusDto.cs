namespace Backend.DTOs.People;

/// <summary>
/// Per-run summary of a bulk email or SMS announcement.
/// Same shape as the WhatsApp notify summary, including the provider or block error.
/// </summary>
public class AnnouncementStatusDto
{
    /// <summary>
    /// Token returned by start. Abort may use this or the election's active run for the channel.
    /// </summary>
    public string QueueToken { get; set; } = string.Empty;

    /// <summary>
    /// <c>email</c> or <c>sms</c>.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// True while background sends are still in progress.
    /// </summary>
    public bool Running { get; set; }

    /// <summary>
    /// True when abort stopped remaining sends.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// People queued for the provider.
    /// </summary>
    public int Queued { get; set; }

    /// <summary>
    /// Provider accepted the message.
    /// </summary>
    public int Sent { get; set; }

    /// <summary>
    /// No address, invalid address, or SMS status blocked. Provider was not called.
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Provider failed, or the paid-send guard blocked the attempt.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Per-person outcomes in request order (duplicates removed).
    /// </summary>
    public List<AnnouncementPersonResultDto> Results { get; set; } = [];
}

/// <summary>
/// One selected person's announcement outcome.
/// </summary>
public class AnnouncementPersonResultDto
{
    /// <summary>
    /// The person that was requested.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// <c>sent</c>, <c>failed</c>, <c>cancelled</c>, or a <c>skipped-*</c> reason.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Provider text or block reason. Empty when the attempt did not fail.
    /// No raw phone or email.
    /// </summary>
    public string? Error { get; set; }
}
