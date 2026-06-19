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
    /// Primary teller name at the time of the action.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// Second teller name at the time of the action, if any.
    /// </summary>
    public string? Teller2 { get; set; }

    /// <summary>
    /// Location name (if applicable).
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Envelope number (if applicable).
    /// </summary>
    public int? EnvNum { get; set; }

    /// <summary>
    /// Additional context for the action (e.g. unregister reason).
    /// </summary>
    public string? PerformedBy { get; set; }
}
