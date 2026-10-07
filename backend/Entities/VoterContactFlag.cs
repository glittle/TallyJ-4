using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// One phone or email on an election that failed a voter-list check.
/// Inactive rows are kept after a super admin clears the election flag.
/// </summary>
[Table("VoterContactFlags")]
[Index(nameof(ElectionGuid), nameof(Active), Name = "IX_VoterContactFlag_Election_Active")]
public class VoterContactFlag
{
    /// <summary>
    /// Database identity.
    /// </summary>
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    /// <summary>
    /// Election this contact belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// Person who has this phone or email, when the row is still in the election.
    /// </summary>
    public Guid? PersonGuid { get; set; }

    /// <summary>
    /// File row number when the value came from an upload. Null for a manual edit.
    /// </summary>
    public int? SourceRowNumber { get; set; }

    /// <summary>
    /// Masked phone or email shown to the super admin.
    /// </summary>
    [StringLength(80)]
    public string MaskedValue { get; set; } = null!;

    /// <summary>
    /// invalid-phone, unexpected-country, consecutive-run, disposable-domain, or no-mx.
    /// </summary>
    [StringLength(40)]
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Normalized phone or email so the same contact is counted once.
    /// </summary>
    [StringLength(250)]
    public string ContactKey { get; set; } = null!;

    /// <summary>
    /// False after the flag rows are replaced or a super admin clears the election.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// When this row was written.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset FlaggedAt { get; set; }
}
