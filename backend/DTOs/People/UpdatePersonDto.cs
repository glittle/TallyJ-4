namespace TallyJ4.DTOs.People;

public class UpdatePersonDto
{
    public string LastName { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? OtherLastNames { get; set; }
    public string? OtherNames { get; set; }
    public string? OtherInfo { get; set; }
    public string? Area { get; set; }
    public string? BahaiId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? CanReceiveVotes { get; set; }
    public bool? CanVote { get; set; }
    public string? AgeGroup { get; set; }
    public Guid? IneligibleReasonGuid { get; set; }
}
