using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("ElectionGuid", Name = "IX_Person")]
[Index("ElectionGuid", "FullName", Name = "IX_PersonElection")]
[Index("PersonGuid", Name = "IX_Person_1", IsUnique = true)]
[Index("CanVote", "IneligibleReasonGuid", Name = "IX_Person_CanVote")]
[Index("ElectionGuid", Name = "nci_msft_Person_22A77D9DC21D83B4582C43E94A27236D")]
public partial class Person
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    public Guid PersonGuid { get; set; }

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? OtherLastNames { get; set; }

    [StringLength(100)]
    public string? OtherNames { get; set; }

    [StringLength(150)]
    public string? OtherInfo { get; set; }

    [StringLength(50)]
    public string? Area { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? BahaiId { get; set; }

    public string? CombinedInfo { get; set; }

    [Unicode(false)]
    public string? CombinedSoundCodes { get; set; }

    public string? CombinedInfoAtStart { get; set; }

    [StringLength(2)]
    [Unicode(false)]
    public string? AgeGroup { get; set; }

    public bool? CanVote { get; set; }

    public bool? CanReceiveVotes { get; set; }

    public Guid? IneligibleReasonGuid { get; set; }

    [Precision(0)]
    public DateTime? RegistrationTime { get; set; }

    public Guid? VotingLocationGuid { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string? VotingMethod { get; set; }

    public int? EnvNum { get; set; }

    [Column("_RowVersion")]
    public byte[] RowVersion { get; set; } = null!;

    [Column("_FullName")]
    [StringLength(461)]
    public string? FullName { get; set; }

    [Column("_RowVersionInt")]
    public long? RowVersionInt { get; set; }

    [Column("_FullNameFL")]
    [StringLength(460)]
    public string? FullNameFl { get; set; }

    [StringLength(25)]
    public string? Teller1 { get; set; }

    [StringLength(25)]
    public string? Teller2 { get; set; }

    [StringLength(250)]
    public string? Email { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string? Phone { get; set; }

    public bool? HasOnlineBallot { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Flags { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? UnitName { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? KioskCode { get; set; }

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}


