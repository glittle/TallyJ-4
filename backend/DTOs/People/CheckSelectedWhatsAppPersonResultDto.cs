namespace Backend.DTOs.People;

/// <summary>
/// One selected person's WhatsApp check outcome.
/// </summary>
public class CheckSelectedWhatsAppPersonResultDto
{
    /// <summary>
    /// The person that was requested.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// <c>OK</c>, <c>no-wa</c>, <c>check-failed</c>, <c>skipped-no-phone</c>,
    /// <c>skipped-non-p</c>, <c>skipped-other-election</c>, or <c>cancelled</c>.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;
}
