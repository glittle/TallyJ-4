namespace Backend.DTOs.SignalR;

/// <summary>
/// Thin SignalR payload for a connected online voter's personal channel
/// (VoterPersonalHub <c>updateVoter</c>). Clients re-fetch status / election list;
/// do not push tallies or rich private election detail.
/// </summary>
public class VoterPersonalUpdateDto
{
    /// <summary>
    /// When true, front desk (or processing) changed this voter's registration / voting method.
    /// Client should re-fetch available elections and vote status for the election.
    /// </summary>
    public bool UpdateRegistration { get; set; }

    /// <summary>
    /// Election whose registration changed (when <see cref="UpdateRegistration"/> is true).
    /// </summary>
    public Guid? ElectionGuid { get; set; }

    /// <summary>
    /// Current voting method code after the change (optional thin hint; client may ignore and re-fetch).
    /// </summary>
    public string? VotingMethod { get; set; }

    /// <summary>
    /// Registration timestamp after the change (null when unregistered).
    /// </summary>
    public DateTimeOffset? RegistrationTime { get; set; }

    /// <summary>
    /// When true, the same voter identity authenticated on another browser/device (v3 <c>login: true</c>).
    /// </summary>
    public bool Login { get; set; }
}
