namespace Backend.DTOs.SignalR;

/// <summary>
/// DTO for updating the vote count of a person in real-time via SignalR.
/// </summary>
public class PersonVoteCountUpdateDto
{
    /// <summary>
    /// The GUID of the election to which the person belongs. This is used to identify the correct election context for the vote count update.
    /// </summary>
    public Guid ElectionGuid { get; set; }
    /// <summary>
    /// The GUID of the person whose vote count is being updated. This identifies the specific person within the election for whom the vote count change applies.
    /// </summary>
    public Guid PersonGuid { get; set; }
    /// <summary>
    /// The new vote count for the person. This value represents the updated number of votes that the person has received, and it will be broadcast to all connected clients to reflect the change in real-time.
    /// </summary>
    public int VoteCount { get; set; }
}
