using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Domain.Entities;

[Index("VoterId", Name = "IX_OnlineVoter_Id", IsUnique = true)]
public partial class OnlineVoter
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    [StringLength(250)]
    public string VoterId { get; set; } = null!;

    [StringLength(1)]
    [Unicode(false)]
    public string VoterIdType { get; set; } = null!;

    [Precision(0)]
    public DateTime? WhenRegistered { get; set; }

    [Precision(0)]
    public DateTime? WhenLastLogin { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string? EmailCodes { get; set; }

    [StringLength(50)]
    public string? Country { get; set; }

    [StringLength(200)]
    public string? OtherInfo { get; set; }

    [StringLength(15)]
    [Unicode(false)]
    public string? VerifyCode { get; set; }

    [Precision(0)]
    public DateTime? VerifyCodeDate { get; set; }

    public int? VerifyAttempts { get; set; }

    [Precision(0)]
    public DateTime? VerifyAttemptsStart { get; set; }
}
