using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.EF.Models;

[Table("Result")]
[Index("ElectionGuid", Name = "IX_Result_Election")]
public partial class Result
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public Guid PersonGuid { get; set; }

    public int? VoteCount { get; set; }

    public int Rank { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string Section { get; set; } = null!;

    public bool? CloseToPrev { get; set; }

    public bool? CloseToNext { get; set; }

    public bool? IsTied { get; set; }

    public int? TieBreakGroup { get; set; }

    public bool? TieBreakRequired { get; set; }

    public int? TieBreakCount { get; set; }

    public bool? IsTieResolved { get; set; }

    public int? RankInExtra { get; set; }

    public bool? ForceShowInOther { get; set; }

    [ForeignKey("ElectionGuid")]
    [InverseProperty("Results")]
    public virtual Election Election { get; set; } = null!;

    [ForeignKey("PersonGuid")]
    [InverseProperty("Results")]
    public virtual Person Person { get; set; } = null!;
}
