using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// Paid-send allowance, freeze, and abuse flag for one election.
/// </summary>
[Table("ElectionSendControls")]
public class ElectionSendControl
{
    /// <summary>
    /// Election these send controls apply to.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// SMS, voice, and WhatsApp sends already counted against the allowance.
    /// </summary>
    public int PaidSendsUsed { get; set; }

    /// <summary>
    /// SMS, voice, and WhatsApp sends allowed before a super admin raises the allowance.
    /// Null uses the configured default.
    /// </summary>
    public int? AllowanceOverride { get; set; }

    /// <summary>
    /// When true, no login code is sent for this election on any channel, including email.
    /// </summary>
    public bool SendsFrozen { get; set; }

    /// <summary>
    /// When true, SMS and WhatsApp are stopped and online voting is suspended
    /// until a super admin clears the flag.
    /// </summary>
    public bool Flagged { get; set; }

    /// <summary>
    /// When the election was flagged.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? FlaggedAt { get; set; }

    /// <summary>
    /// Active flagged voter-list entries at the time the election was flagged.
    /// </summary>
    public int FlaggedEntryCount { get; set; }

    /// <summary>
    /// When a super admin cleared the flag.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? ClearedAt { get; set; }

    /// <summary>
    /// Super admin account id that cleared the flag.
    /// </summary>
    [StringLength(450)]
    public string? ClearedByUserId { get; set; }
}
