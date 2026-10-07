using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// Paid-send approval, daily cap, and freeze for one election owner or admin.
/// One row per login account. The account is the <see cref="JoinElectionUser.UserId"/>.
/// </summary>
[Table("OwnerSendControls")]
public class OwnerSendControl
{
    /// <summary>
    /// Login account that owns or administers elections.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid UserId { get; set; }

    /// <summary>
    /// When true, this account is approved for SMS, voice, and WhatsApp codes.
    /// An election may send those codes only when every owner and admin on it is approved.
    /// Email codes do not read this flag.
    /// </summary>
    public bool PaidSendsApproved { get; set; }

    /// <summary>
    /// When a super admin approved paid sends. Null until the first approval.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>
    /// Super admin account id that approved paid sends.
    /// </summary>
    [StringLength(450)]
    public string? ApprovedByUserId { get; set; }

    /// <summary>
    /// SMS, voice, and WhatsApp sends allowed per UTC day. Null uses the configured default.
    /// </summary>
    public int? DailyCapOverride { get; set; }

    /// <summary>
    /// When true, no login code (email, SMS, voice, or WhatsApp) is sent for any election
    /// where this account is an owner or admin. One frozen owner or admin stops that election
    /// even when another owner or admin on it is not frozen.
    /// </summary>
    public bool SendsFrozen { get; set; }

    /// <summary>
    /// When the account's first paid send was recorded. Used so the first-send alert fires once.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? FirstPaidSendAt { get; set; }
}
