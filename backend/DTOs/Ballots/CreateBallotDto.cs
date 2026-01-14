namespace TallyJ4.DTOs.Ballots;

public class CreateBallotDto
{
    public Guid LocationGuid { get; set; }
    public string ComputerCode { get; set; } = null!;
}
