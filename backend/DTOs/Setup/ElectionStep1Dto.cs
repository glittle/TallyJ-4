namespace TallyJ4.DTOs.Setup;

/// <summary>
/// Data transfer object for election setup step 1.
/// Contains basic election information including name, date, and reason.
/// </summary>
public class ElectionStep1Dto
{
    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTime? DateOfElection { get; set; }

    /// <summary>
    /// The reason or purpose for holding this election.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
