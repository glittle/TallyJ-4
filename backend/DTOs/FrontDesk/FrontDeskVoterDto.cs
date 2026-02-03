namespace TallyJ4.DTOs.FrontDesk;

public class FrontDeskVoterDto
{
    public Guid PersonGuid { get; set; }
    public string FullName { get; set; } = null!;
    public string? BahaiId { get; set; }
    public string? Area { get; set; }
    public bool? CanVote { get; set; }
    public string? VotingMethod { get; set; }
    public int? EnvNum { get; set; }
    public DateTime? RegistrationTime { get; set; }
    public Guid? VotingLocationGuid { get; set; }
    public string? Teller1 { get; set; }
    public string? Teller2 { get; set; }
    public bool IsCheckedIn => RegistrationTime.HasValue;
}
