namespace TallyJ4.DTOs.SignalR;

public class TallyProgressDto
{
    public Guid ElectionGuid { get; set; }
    public int TotalBallots { get; set; }
    public int ProcessedBallots { get; set; }
    public int TotalVotes { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public bool IsComplete { get; set; }
}
