using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("ElectionGuid", Name = "IX_JoinElectionUser_ElectionGuid")]
[Index("UserId", Name = "IX_JoinElectionUser_UserId")]
public partial class JoinElectionUser
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public Guid UserId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? Role { get; set; }

    [StringLength(150)]
    public string? InviteEmail { get; set; }

    [Precision(0)]
    public DateTimeOffset? InviteWhen { get; set; }

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}


