namespace TallyJ4.DTOs.Ballots;

public class CreateBallotDto
{
    public Guid ElectionGuid { get; set; }
    public string ComputerCode { get; set; } = null!;
}
