namespace Backend.DTOs.People;

/// <summary>
/// Teller request to queue a WhatsApp notify for selected people in one election.
/// Bound so one start cannot queue an entire imported roll.
/// </summary>
public class StartWhatsAppNotifyDto
{
    /// <summary>
    /// Maximum person GUIDs accepted on one notify start.
    /// </summary>
    public const int MaxSelectedPeople = 100;

    /// <summary>
    /// People to consider. Election-scoped on the URL. Duplicates are ignored.
    /// Only phone P rows with <c>WhatsAppStatus == "OK"</c> are queued to send.
    /// </summary>
    public List<Guid> PersonGuids { get; set; } = [];
}
