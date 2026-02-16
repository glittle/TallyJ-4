namespace Backend.DTOs.SuperAdmin;

public class SuperAdminSummaryDto
{
    public int TotalElections { get; set; }
    public int OpenElections { get; set; }
    public int UpcomingElections { get; set; }
    public int CompletedElections { get; set; }
    public int ArchivedElections { get; set; }
}



