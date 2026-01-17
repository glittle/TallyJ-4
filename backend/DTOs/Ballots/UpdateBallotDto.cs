namespace TallyJ4.DTOs.Ballots;

public class UpdateBallotDto
{
    public string StatusCode { get; set; } = null!;
    public string? Teller1 { get; set; }
    public string? Teller2 { get; set; }
}
