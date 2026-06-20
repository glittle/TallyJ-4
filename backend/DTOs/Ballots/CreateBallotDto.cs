using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Ballots;

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

    /// <summary>
    /// The name of the primary teller entering this ballot.
    /// </summary>
    [StringLength(25)]
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller, if any.
    /// </summary>
    [StringLength(25)]
    public string? Teller2 { get; set; }
}