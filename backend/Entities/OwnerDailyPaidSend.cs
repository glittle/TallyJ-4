using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// SMS, voice, and WhatsApp sends one owner has made on one UTC calendar day.
/// The row is the counter. It resets by using a new date, not by deleting the previous day.
/// </summary>
[Table("OwnerDailyPaidSends")]
[PrimaryKey(nameof(UserId), nameof(UtcDate))]
public class OwnerDailyPaidSend
{
    /// <summary>
    /// Owner or admin the sends are charged to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// UTC calendar day the sends belong to.
    /// </summary>
    public DateOnly UtcDate { get; set; }

    /// <summary>
    /// Paid sends charged to this owner on <see cref="UtcDate"/>.
    /// </summary>
    public int SendCount { get; set; }
}
