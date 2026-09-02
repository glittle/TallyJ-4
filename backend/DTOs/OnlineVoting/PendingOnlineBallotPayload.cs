namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Vote list and optional name pool stored on <c>OnlineVotingInfo.ListPool</c>
/// while the online ballot is pending Accept-all. Wiped when a regular ballot is created.
/// </summary>
public class PendingOnlineBallotPayload
{
    public List<OnlineVoteDto> Votes { get; set; } = new();

    public List<OnlinePoolEntryDto> Pool { get; set; } = new();
}
