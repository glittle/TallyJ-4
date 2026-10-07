using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// Last time one alert key emailed the super admin.
/// One election cannot send the same alert again until the throttle window passes.
/// </summary>
[Table("AbuseAlertStates")]
public class AbuseAlertState
{
    /// <summary>
    /// Stable key such as cap:election:{guid} or flag:{guid}.
    /// </summary>
    [Key]
    [StringLength(160)]
    [Unicode(false)]
    public string AlertKey { get; set; } = null!;

    /// <summary>
    /// When this key last sent an email and Sentry warning.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset LastSentAt { get; set; }
}
