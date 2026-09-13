using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

[Index("VoterId", Name = "IX_OnlineVoter_Id", IsUnique = true)]
public partial class OnlineVoter
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    [StringLength(250)]
    public string VoterId { get; set; } = null!;

    [StringLength(1)]
    [Unicode(false)]
    public string VoterIdType { get; set; } = null!;

    [Precision(0)]
    public DateTimeOffset? WhenRegistered { get; set; }

    [Precision(0)]
    public DateTimeOffset? WhenLastLogin { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string? EmailCodes { get; set; }

    [StringLength(50)]
    public string? Country { get; set; }

    [StringLength(200)]
    public string? OtherInfo { get; set; }

    /// <summary>
    /// SMS/voice paid-channel eligibility when <see cref="VoterIdType"/> is phone.
    /// null = not yet checked (paid send still allowed);
    /// "OK" = checked and valid;
    /// any other short value = blocked (reason code).
    /// Also gates WhatsApp verify-code send (same rule as SMS).
    /// Email / kiosk / Telegram rows leave this null.
    /// Separate from <see cref="WhatsAppStatus"/>.
    /// </summary>
    [StringLength(50)]
    [Unicode(false)]
    public string? SmsStatus { get; set; }

    /// <summary>
    /// WhatsApp presence when <see cref="VoterIdType"/> is phone (GreenAPI checkWhatsapp).
    /// null = not yet checked (WhatsApp send still allowed);
    /// "OK" = has WhatsApp;
    /// any other short value = no / blocked / error (e.g. "no-wa", "check-failed").
    /// Global by phone. Email / kiosk / Telegram rows leave this null.
    /// Separate from <see cref="SmsStatus"/>.
    /// </summary>
    [StringLength(50)]
    [Unicode(false)]
    public string? WhatsAppStatus { get; set; }

    [StringLength(15)]
    [Unicode(false)]
    public string? VerifyCode { get; set; }

    [Precision(0)]
    public DateTimeOffset? VerifyCodeDate { get; set; }

    public int? VerifyAttempts { get; set; }

    [Precision(0)]
    public DateTimeOffset? VerifyAttemptsStart { get; set; }
}


