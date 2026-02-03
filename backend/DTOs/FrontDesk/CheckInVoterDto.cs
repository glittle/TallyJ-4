using System.ComponentModel.DataAnnotations;

namespace TallyJ4.DTOs.FrontDesk;

public class CheckInVoterDto
{
    [Required]
    public Guid PersonGuid { get; set; }

    [Required]
    [StringLength(1)]
    public string VotingMethod { get; set; } = null!;

    [StringLength(25)]
    public string? TellerName { get; set; }

    public Guid? VotingLocationGuid { get; set; }
}
