using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Domain.Entities;

[Index("ElectionGuid", Name = "Ix_ResultSummary_Election")]
public partial class ResultSummary
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string ResultType { get; set; } = null!;

    public bool? UseOnReports { get; set; }

    public int? NumVoters { get; set; }

    public int? NumEligibleToVote { get; set; }

    public int? MailedInBallots { get; set; }

    public int? DroppedOffBallots { get; set; }

    public int? InPersonBallots { get; set; }

    public int? SpoiledBallots { get; set; }

    public int? SpoiledVotes { get; set; }

    public int? TotalVotes { get; set; }

    public int? BallotsReceived { get; set; }

    public int? BallotsNeedingReview { get; set; }

    public int? CalledInBallots { get; set; }

    public int? OnlineBallots { get; set; }

    public int? SpoiledManualBallots { get; set; }

    public int? Custom1Ballots { get; set; }

    public int? Custom2Ballots { get; set; }

    public int? Custom3Ballots { get; set; }

    public int? ImportedBallots { get; set; }

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}
