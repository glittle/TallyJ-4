using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

[Index("ElectionGuid", "Name", Name = "IX_Teller", IsUnique = true)]
public partial class Teller
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}