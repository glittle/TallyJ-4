namespace Backend.DTOs.FrontDesk;

/// <summary>
/// Data transfer object for unregistering a voter.
/// </summary>
public class UnregisterVoterDto
{
    /// <summary>
    /// The unique identifier of the person to unregister.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// Reason for unregistering (optional).
    /// </summary>
    public string? Reason { get; set; }
}
