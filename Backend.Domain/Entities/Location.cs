using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("LocationGuid", Name = "IX_Location", IsUnique = true)]
[Index("ElectionGuid", Name = "IX_Location_Election")]
public partial class Location
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public Guid LocationGuid { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(250)]
    public string? ContactInfo { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Long { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Lat { get; set; }

    [StringLength(15)]
    [Unicode(false)]
    public string? TallyStatus { get; set; }

    public int? SortOrder { get; set; }

    public int? BallotsCollected { get; set; }

    public virtual ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}


