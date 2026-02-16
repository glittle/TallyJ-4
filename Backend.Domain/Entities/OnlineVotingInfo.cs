using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("ElectionGuid", "PersonGuid", Name = "IX_OnlineVotingInfo_ElectionPerson")]
[Index("PersonGuid", Name = "IX_OnlineVotingInfo_Person")]
public partial class OnlineVotingInfo
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public Guid PersonGuid { get; set; }

    [Precision(0)]
    public DateTime? WhenBallotCreated { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Precision(0)]
    public DateTime? WhenStatus { get; set; }

    public string? ListPool { get; set; }

    public bool? PoolLocked { get; set; }

    [Unicode(false)]
    public string? HistoryStatus { get; set; }

    public bool? NotifiedAboutOpening { get; set; }
}


