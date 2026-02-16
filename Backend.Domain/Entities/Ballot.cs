using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

/// <summary>
/// Entity representing a ballot in an election.
/// Contains ballot identification, status, and associated votes.
/// </summary>
[Index("BallotGuid", Name = "IX_Ballot", IsUnique = true)]
[Index("ComputerCode", Name = "IX_Ballot_Code")]
[Index("LocationGuid", Name = "IX_Ballot_Location")]
public partial class Ballot
{
    /// <summary>
    /// The internal row identifier for the ballot.
    /// </summary>
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    /// <summary>
    /// The unique identifier of the location where this ballot was cast.
    /// </summary>
    public Guid LocationGuid { get; set; }

    /// <summary>
    /// The unique identifier for this ballot.
    /// </summary>
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// The status code of the ballot (e.g., "Ok", "Spoiled").
    /// </summary>
    [StringLength(10)]
    [Unicode(false)]
    public string StatusCode { get; set; } = null!;

    /// <summary>
    /// The computer code where this ballot was entered.
    /// </summary>
    [StringLength(2)]
    [Unicode(false)]
    public string ComputerCode { get; set; } = null!;

    /// <summary>
    /// The ballot number at the specific computer.
    /// </summary>
    public int BallotNumAtComputer { get; set; }

    /// <summary>
    /// The encrypted or hashed ballot code for verification.
    /// </summary>
    [Column("_BallotCode")]
    [StringLength(32)]
    [Unicode(false)]
    public string? BallotCode { get; set; }

    /// <summary>
    /// The name of the first teller who processed this ballot.
    /// </summary>
    [StringLength(25)]
    public string? Teller1 { get; set; }

    /// <summary>
    /// The name of the second teller who processed this ballot.
    /// </summary>
    [StringLength(25)]
    public string? Teller2 { get; set; }

    /// <summary>
    /// The row version for optimistic concurrency control.
    /// </summary>
    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    /// <summary>
    /// The location where this ballot was cast.
    /// </summary>
    [ForeignKey("LocationGuid")]
    public virtual Location Location { get; set; } = null!;

    /// <summary>
    /// The collection of votes associated with this ballot.
    /// </summary>
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}


