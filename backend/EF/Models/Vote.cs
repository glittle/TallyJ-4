using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.EF.Models;

[Table("Vote")]
[Index("BallotGuid", "PositionOnBallot", Name = "IX_VoteBallot")]
[Index("PersonGuid", Name = "IX_VotePerson")]
public partial class Vote
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid BallotGuid { get; set; }

    public int PositionOnBallot { get; set; }

    public Guid? PersonGuid { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string StatusCode { get; set; } = null!;

    public Guid? InvalidReasonGuid { get; set; }

    public int? SingleNameElectionCount { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    public string? PersonCombinedInfo { get; set; }

    public string? OnlineVoteRaw { get; set; }

    [ForeignKey("BallotGuid")]
    [InverseProperty("Votes")]
    public virtual Ballot Ballot { get; set; } = null!;

    [ForeignKey("PersonGuid")]
    [InverseProperty("Votes")]
    public virtual Person? Person { get; set; }
}
