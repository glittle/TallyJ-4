namespace Backend.DTOs.SignalR;

public class PersonVoteCountUpdateDto
{
    public Guid ElectionGuid { get; set; }
    public Guid PersonGuid { get; set; }
    public int VoteCount { get; set; }
}
