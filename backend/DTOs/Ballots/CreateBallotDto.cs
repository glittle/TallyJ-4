namespace TallyJ4.DTOs.Ballots;

/// <summary>
/// Data transfer object for creating a new ballot.
/// </summary>
public class CreateBallotDto
{
    /// <summary>
    /// The GUID of the election this ballot belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The code of the computer creating this ballot.
    /// </summary>
    public string ComputerCode { get; set; } = null!;
}
