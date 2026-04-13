using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("BallotGuid", "PositionOnBallot", Name = "IX_VoteBallot")]
[Index("PersonGuid", Name = "IX_VotePerson")]
public partial class Vote
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid BallotGuid { get; set; }

    public int PositionOnBallot { get; set; }

    /// <summary>
    /// The GUID of the person this vote is for.
    /// </summary>
    public Guid? PersonGuid { get; set; }

    public VoteStatus VoteStatus { get; set; }

    /// <summary>
    /// The <see cref="IneligibleReasonEnum"/> code (e.g., "X01") of the person linked to this vote, or why there is no person
    /// </summary>
    public string? IneligibleReasonCode { get; set; }

    public int? SingleNameElectionCount { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    public string? PersonCombinedInfo { get; set; }

    public string? OnlineVoteRaw { get; set; }

    [ForeignKey("BallotGuid")]
    public virtual Ballot Ballot { get; set; } = null!;

    [ForeignKey("PersonGuid")]
    public virtual Person? Person { get; set; }
}


