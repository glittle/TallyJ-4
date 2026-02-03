namespace TallyJ4.DTOs.FrontDesk;

public class FrontDeskStatsDto
{
    public int TotalEligible { get; set; }
    public int CheckedIn { get; set; }
    public int NotYetCheckedIn { get; set; }
    public decimal CheckInPercentage => TotalEligible > 0 
        ? Math.Round((decimal)CheckedIn / TotalEligible * 100, 1) 
        : 0;
}
