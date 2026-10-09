using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// System-wide SMS, voice, and WhatsApp send count for one destination prefix.
/// One row per prefix. The two counts are the current hour-aligned bucket and the
/// previous bucket, so the limit is a sliding hour rather than a counter that
/// resets in full at the boundary.
/// </summary>
[Table("PhonePrefixSendCounters")]
public class PhonePrefixSendCounter
{
    /// <summary>
    /// First digits of the E.164 number, without <c>+</c>.
    /// </summary>
    [Key]
    [StringLength(16)]
    [Unicode(false)]
    public string Prefix { get; set; } = null!;

    /// <summary>
    /// Unix seconds when the current bucket started, aligned to the window.
    /// </summary>
    public long BucketStartedUnix { get; set; }

    /// <summary>
    /// Sends charged in the current bucket.
    /// </summary>
    public int SendCount { get; set; }

    /// <summary>
    /// Sends charged in the previous bucket. Older buckets are discarded.
    /// </summary>
    public int PreviousCount { get; set; }
}
