namespace TallyJ4.DTOs.FrontDesk;

public class RollCallDto
{
    public List<FrontDeskVoterDto> Voters { get; set; } = new();
    public FrontDeskStatsDto Stats { get; set; } = new();
}
