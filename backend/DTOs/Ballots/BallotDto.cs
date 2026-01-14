using TallyJ4.DTOs.Votes;

namespace TallyJ4.DTOs.Ballots;

public class BallotDto
{
    public Guid BallotGuid { get; set; }
    public string BallotCode { get; set; } = null!;
    public Guid LocationGuid { get; set; }
    public string LocationName { get; set; } = null!;
    public int BallotNumAtComputer { get; set; }
    public string ComputerCode { get; set; } = null!;
    public string StatusCode { get; set; } = null!;
    public string? Teller1 { get; set; }
    public string? Teller2 { get; set; }
    public int VoteCount { get; set; }
    public List<VoteDto> Votes { get; set; } = new();
}
