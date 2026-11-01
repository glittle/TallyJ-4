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
    /// <see cref="OwnerRestrictionStatus"/>. Empty on rows that predate owner review
    /// and were not backfilled. Approved means this account is trusted for voter codes,
    /// online voting, and paid sends. <see cref="PaidSendsApproved"/> is kept in step
    /// with Approved so the paid-send counters still read one flag.
    /// </summary>
    [StringLength(32)]
    public string RestrictionStatus { get; set; } = "";

    /// <summary>
    /// When the owner last asked for a review. Also the start of the 24-hour re-request throttle.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? ReviewRequestedAt { get; set; }

    /// <summary>
    /// When a super admin last approved, rejected, or held this account.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// Super admin account id for the last review action.
    /// </summary>
    [StringLength(450)]
    public string? ReviewedByUserId { get; set; }

    /// <summary>
    /// Note stored with Hold or Reject and shown to the owner.
    /// </summary>
    [StringLength(2000)]
    public string? ReviewNote { get; set; }

    /// <summary>
    /// When the account was rejected. The cleanup job deletes that owner's elections
    /// <see cref="OwnerRestrictionStatus.RejectionRetentionDays"/> after this time.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? RejectedAt { get; set; }

    /// <summary>
    /// When <see cref="RestrictionStatus"/> last changed.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? StatusChangedAt { get; set; }

    /// <summary>
    /// When true, this account is approved for SMS, voice, and WhatsApp codes.
    /// Set together with <see cref="RestrictionStatus"/> Approved. An election may send
    /// those codes only when every owner and admin on it is approved.
    /// </summary>
    public bool PaidSendsApproved { get; set; }

    /// <summary>
    /// When a super admin approved this account. Null until the first approval.
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
