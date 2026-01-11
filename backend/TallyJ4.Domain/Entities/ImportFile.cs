using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Domain.Entities;

public partial class ImportFile
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    public Guid ElectionGuid { get; set; }

    [Precision(2)]
    public DateTime? UploadTime { get; set; }

    [Precision(2)]
    public DateTime? ImportTime { get; set; }

    public int? FileSize { get; set; }

    public bool? HasContent { get; set; }

    public int? FirstDataRow { get; set; }

    public string? ColumnsToRead { get; set; }

    [StringLength(50)]
    public string? OriginalFileName { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? ProcessingStatus { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? FileType { get; set; }

    public int? CodePage { get; set; }

    [Unicode(false)]
    public string? Messages { get; set; }

    [Column(TypeName = "image")]
    public byte[]? Contents { get; set; }

    [ForeignKey("ElectionGuid")]
    public virtual Election Election { get; set; } = null!;
}
