namespace TallyJ4.DTOs.SignalR;

public class BallotUpdateDto
{
    public Guid ElectionGuid { get; set; }
    public Guid BallotGuid { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? BallotCode { get; set; }
    public string? StatusCode { get; set; }
    public int? VoteCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}