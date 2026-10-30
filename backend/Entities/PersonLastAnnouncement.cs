using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// Last bulk announcement attempt for one person (email, SMS, or WhatsApp).
/// One row per person. Retries update this row; they do not add another.
/// </summary>
[Table("PersonLastAnnouncements")]
[Index(nameof(PersonGuid), Name = "IX_PersonLastAnnouncement_Person", IsUnique = true)]
[Index(nameof(ElectionGuid), nameof(Channel), nameof(Outcome), Name = "IX_PersonLastAnnouncement_Election_Channel_Outcome")]
public class PersonLastAnnouncement
{
    /// <summary>
    /// Database identity.
    /// </summary>
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    /// <summary>
    /// Person this attempt belongs to.
    /// </summary>
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// Election the attempt was made for.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// <c>email</c>, <c>sms</c>, or <c>whatsapp</c>.
    /// </summary>
    [StringLength(20)]
    [Unicode(false)]
    public string Channel { get; set; } = null!;

    /// <summary>
    /// <c>sent</c>, <c>failed</c>, <c>cancelled</c>, or a <c>skipped-*</c> reason.
    /// </summary>
    [StringLength(40)]
    [Unicode(false)]
    public string Outcome { get; set; } = null!;

    /// <summary>
    /// Provider message or block reason. No raw phone or email.
    /// </summary>
    [StringLength(200)]
    public string? Error { get; set; }

    /// <summary>
    /// When this attempt finished.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset AttemptedAt { get; set; }
}
