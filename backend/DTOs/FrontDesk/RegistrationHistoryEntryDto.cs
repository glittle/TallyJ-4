namespace Backend.DTOs.FrontDesk;

/// <summary>
/// Represents a single registration history entry for a person.
/// </summary>
public class RegistrationHistoryEntryDto
{
    /// <summary>
    /// Timestamp of the action (stored in UTC).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Action performed (e.g., "CheckedIn", "Unregistered").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Voting method used (if applicable).
    /// </summary>
    public string? VotingMethod { get; set; }

    /// <summary>
    /// Teller name (if applicable).
    /// </summary>
    public string? TellerName { get; set; }

    /// <summary>
    /// Location name (if applicable).
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Envelope number (if applicable).
    /// </summary>
    public int? EnvNum { get; set; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    public string? PerformedBy { get; set; }
}
