using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Domain.Entities;

[Index("ElectionGuid", Name = "IX_Election", IsUnique = true)]
public partial class Election
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string? Convenor { get; set; }

    [Precision(0)]
    public DateTime? DateOfElection { get; set; }

    [StringLength(5)]
    [Unicode(false)]
    public string? ElectionType { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string? ElectionMode { get; set; }

    public int? NumberToElect { get; set; }

    public int? NumberExtra { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string? CanVote { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string? CanReceive { get; set; }

    public int? LastEnvNum { get; set; }

    [StringLength(15)]
    [Unicode(false)]
    public string? TallyStatus { get; set; }

    public bool? ShowFullReport { get; set; }

    public Guid? LinkedElectionGuid { get; set; }

    [StringLength(2)]
    [Unicode(false)]
    public string? LinkedElectionKind { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? OwnerLoginId { get; set; }

    [StringLength(50)]
    public string? ElectionPasscode { get; set; }

    public DateTime? ListedForPublicAsOf { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    public bool? ListForPublic { get; set; }

    public bool? ShowAsTest { get; set; }

    public bool? UseCallInButton { get; set; }

    public bool? HidePreBallotPages { get; set; }

    public bool? MaskVotingMethod { get; set; }

    [Precision(0)]
    public DateTime? OnlineWhenOpen { get; set; }

    [Precision(0)]
    public DateTime? OnlineWhenClose { get; set; }

    public bool OnlineCloseIsEstimate { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string? OnlineSelectionProcess { get; set; }

    [Precision(0)]
    public DateTime? OnlineAnnounced { get; set; }

    [StringLength(250)]
    public string? EmailFromAddress { get; set; }

    [StringLength(100)]
    public string? EmailFromName { get; set; }

    public string? EmailText { get; set; }

    [StringLength(500)]
    public string? SmsText { get; set; }

    [StringLength(250)]
    public string? EmailSubject { get; set; }

    [StringLength(50)]
    public string? CustomMethods { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? VotingMethods { get; set; }

    public string? Flags { get; set; }

    public virtual ICollection<ImportFile> ImportFiles { get; set; } = new List<ImportFile>();

    public virtual ICollection<JoinElectionUser> JoinElectionUsers { get; set; } = new List<JoinElectionUser>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Person> People { get; set; } = new List<Person>();

    public virtual ICollection<ResultSummary> ResultSummaries { get; set; } = new List<ResultSummary>();

    public virtual ICollection<ResultTie> ResultTies { get; set; } = new List<ResultTie>();

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual ICollection<Teller> Tellers { get; set; } = new List<Teller>();
}
