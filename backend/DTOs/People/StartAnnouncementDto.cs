namespace Backend.DTOs.People;

/// <summary>
/// Teller request to queue an email or SMS announcement for selected people in one election.
/// Bound so one start cannot queue an entire imported roll.
/// </summary>
public class StartAnnouncementDto
{
    /// <summary>
    /// Maximum person GUIDs accepted on one announcement start.
    /// </summary>
    public const int MaxSelectedPeople = 100;

    /// <summary>
    /// <c>email</c> or <c>sms</c>. WhatsApp uses the notify endpoint.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// People to consider. Election-scoped on the URL. Duplicates are ignored.
    /// </summary>
    public List<Guid> PersonGuids { get; set; } = [];
}
