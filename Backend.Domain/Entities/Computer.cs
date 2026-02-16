using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("ComputerGuid", Name = "IX_Computer", IsUnique = true)]
[Index("LocationGuid", Name = "IX_Computer_Location")]
[Index("ElectionGuid", "ComputerCode", Name = "IX_Computer_Code", IsUnique = true)]
public partial class Computer
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public Guid LocationGuid { get; set; }

    public Guid ComputerGuid { get; set; }

    [StringLength(2)]
    [Unicode(false)]
    public string ComputerCode { get; set; } = null!;

    [StringLength(250)]
    public string? BrowserInfo { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime? LastActivity { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public bool? IsActive { get; set; }

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;

    [ForeignKey("LocationGuid")]
    public virtual Location Location { get; set; } = null!;
}


