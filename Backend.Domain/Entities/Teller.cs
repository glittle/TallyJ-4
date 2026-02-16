using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("ElectionGuid", "Name", Name = "IX_Teller", IsUnique = true)]
public partial class Teller
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(2)]
    [Unicode(false)]
    public string? UsingComputerCode { get; set; }

    public bool? IsHeadTeller { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}


