namespace Backend.DTOs.People;

/// <summary>
/// Manual teller set of <c>OnlineVoter.SmsStatus</c> for the person's stored
/// phone P row. <c>"OK"</c> unblocks; any other short value is the block reason.
/// Null (unchecked) is not set through this DTO.
/// </summary>
public class SetPersonPhoneSmsStatusDto
{
    /// <summary>
    /// <c>"OK"</c> (any case is stored as <c>OK</c>) or a short block reason
    /// (max 50 after trim).
    /// </summary>
    public string SmsStatus { get; set; } = null!;
}
