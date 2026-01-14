using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Domain.Entities;

[Index("ElectionGuid", "TieBreakGroup", Name = "IX_ResultTie", IsUnique = true)]
public partial class ResultTie
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public int TieBreakGroup { get; set; }

    public bool? TieBreakRequired { get; set; }

    public int NumToElect { get; set; }

    public int NumInTie { get; set; }

    public bool? IsResolved { get; set; }

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}
