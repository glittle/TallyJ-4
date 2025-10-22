using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.EF.Models;

[Table("Message")]
public partial class Message
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    [StringLength(150)]
    public string Title { get; set; } = null!;

    public string? Details { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    [Precision(0)]
    public DateTime AsOf { get; set; }

    [ForeignKey("ElectionGuid")]
    [InverseProperty("Messages")]
    public virtual Election Election { get; set; } = null!;
}
