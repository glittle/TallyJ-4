namespace Backend.DTOs.People;

/// <summary>
/// Bulk WhatsApp check result for selected people. Not a notify send queue.
/// When <see cref="Cancelled"/> is true, <see cref="Results"/> still lists
/// what finished plus remaining people as <c>cancelled</c>.
/// </summary>
public class CheckSelectedWhatsAppResultDto
{
    /// <summary>
    /// True when the request abort token stopped remaining provider calls.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// People for whom GreenAPI was called and a status was persisted.
    /// </summary>
    public int Checked { get; set; }

    /// <summary>
    /// Persisted <c>OK</c>.
    /// </summary>
    public int Ok { get; set; }

    /// <summary>
    /// Persisted <c>no-wa</c>.
    /// </summary>
    public int NoWa { get; set; }

    /// <summary>
    /// Persisted <c>check-failed</c>.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// No-phone, non-P occupant, or other-election. Provider not called.
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Per-person outcomes in request order (duplicates removed).
    /// </summary>
    public List<CheckSelectedWhatsAppPersonResultDto> Results { get; set; } = [];
}
