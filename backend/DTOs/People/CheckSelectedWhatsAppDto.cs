namespace Backend.DTOs.People;

/// <summary>
/// Teller request to GreenAPI-check WhatsApp for selected people in one election.
/// Bound so one request cannot check an entire imported roll.
/// </summary>
public class CheckSelectedWhatsAppDto
{
    /// <summary>
    /// Maximum person GUIDs accepted on one check-selected request.
    /// </summary>
    public const int MaxSelectedPeople = 100;

    /// <summary>
    /// People to check. Election-scoped on the URL. Duplicates are ignored.
    /// </summary>
    public List<Guid> PersonGuids { get; set; } = [];
}
