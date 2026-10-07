using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// One login-code send or blocked send. The destination is stored masked.
/// </summary>
[Table("CodeSendLogs")]
[Index(nameof(ElectionGuid), nameof(SentAt), Name = "IX_CodeSendLog_Election_SentAt")]
[Index(nameof(OwnerUserId), nameof(SentAt), Name = "IX_CodeSendLog_Owner_SentAt")]
public class CodeSendLog
{
    /// <summary>
    /// Database identity.
    /// </summary>
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    /// <summary>
    /// Election the send was charged to, or the election that blocked it.
    /// </summary>
    public Guid? ElectionGuid { get; set; }

    /// <summary>
    /// Owner or admin the send was charged to.
    /// </summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// Delivery channel: email, sms, voice, or whatsapp.
    /// </summary>
    [StringLength(20)]
    [Unicode(false)]
    public string Channel { get; set; } = null!;

    /// <summary>
    /// Destination with the local part or middle digits hidden.
    /// </summary>
    [StringLength(80)]
    [Unicode(false)]
    public string MaskedDestination { get; set; } = null!;

    /// <summary>
    /// sent, send-failed, or blocked-* reason.
    /// </summary>
    [StringLength(40)]
    [Unicode(false)]
    public string Outcome { get; set; } = null!;

    /// <summary>
    /// When the send was attempted.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset SentAt { get; set; }
}
