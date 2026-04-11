using Backend.Domain.Enumerations;
using Backend.DTOs.Votes;

namespace Backend.DTOs.Ballots;

/// <summary>
/// Data transfer object representing a ballot with its associated votes.
/// </summary>
public class BallotDto
{
    /// <summary>
    /// The unique identifier for the ballot.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// The code identifying this ballot.
    /// </summary>
    public string BallotCode { get; set; } = null!;

    /// <summary>
    /// The GUID of the location where this ballot was cast.
    /// </summary>
    public Guid LocationGuid { get; set; }

    /// <summary>
    /// The name of the location where this ballot was cast.
    /// </summary>
    public string LocationName { get; set; } = null!;

    /// <summary>
    /// The ballot number at the specific computer.
    /// </summary>
    public int BallotNumAtComputer { get; set; }

    /// <summary>
    /// The code of the computer that processed this ballot.
    /// </summary>
    public string ComputerCode { get; set; } = null!;

    /// <summary>
    /// The status of the ballot.
    /// </summary>
    public BallotStatus StatusCode { get; set; }

    /// <summary>
    /// The name of the first teller who processed the ballot.
    /// </summary>
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller who processed the ballot.
    /// </summary>
    public string? Teller2 { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public DateTimeOffset? DateUpdated { get; set; }

    /// <summary>
    /// The number of votes on this ballot.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The list of votes associated with this ballot.
    /// </summary>
    public List<VoteDto> Votes { get; set; } = new();
}



