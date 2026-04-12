using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

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

    /// <summary>
    /// Section code indicating the election section for this result (e.g. "E" for Elected, "X" for Extra, "O" for Other).
    /// </summary>
    [StringLength(1)]
    [Column("Section")]
    [Unicode(false)]
    public ResultSection SectionCode { get; set; }

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
    public virtual Election Election { get; set; } = null!;

    [ForeignKey("PersonGuid")]
    public virtual Person Person { get; set; } = null!;
}


