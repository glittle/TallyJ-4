using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Domain.Entities;

[Index("BallotGuid", Name = "IX_Ballot", IsUnique = true)]
[Index("ComputerCode", Name = "IX_Ballot_Code")]
[Index("LocationGuid", Name = "IX_Ballot_Location")]
public partial class Ballot
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid LocationGuid { get; set; }

    public Guid BallotGuid { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string StatusCode { get; set; } = null!;

    [StringLength(2)]
    [Unicode(false)]
    public string ComputerCode { get; set; } = null!;

    public int BallotNumAtComputer { get; set; }

    [Column("_BallotCode")]
    [StringLength(32)]
    [Unicode(false)]
    public string? BallotCode { get; set; }

    [StringLength(25)]
    public string? Teller1 { get; set; }

    [StringLength(25)]
    public string? Teller2 { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    [ForeignKey("LocationGuid")]
    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
