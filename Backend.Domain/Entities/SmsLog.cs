using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("SmsSid", Name = "IX_SmsLog")]
[Index("ElectionGuid", "LastDate", Name = "IX_SmsLog_Election_Date", IsDescending = new[] { false, true })]
public partial class SmsLog
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    [StringLength(40)]
    [Unicode(false)]
    public string SmsSid { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string Phone { get; set; } = null!;

    [Precision(0)]
    public DateTimeOffset SentDate { get; set; }

    public Guid? ElectionGuid { get; set; }

    public Guid? PersonGuid { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? LastStatus { get; set; }

    [Precision(0)]
    public DateTimeOffset? LastDate { get; set; }

    public int? ErrorCode { get; set; }
}


