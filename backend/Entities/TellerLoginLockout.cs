using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities;

/// <summary>
/// Consecutive failed shared-passcode attempts for one election.
/// One row per election. The row is the lockout that survives process restarts
/// and is visible to every instance.
/// </summary>
[Table("TellerLoginLockouts")]
public class TellerLoginLockout
{
    /// <summary>
    /// Election whose guest teller passcode attempts are counted.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// Wrong passcodes in a row since the last success or the last expired lock.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// When set and still in the future, shared-passcode guest teller login is locked.
    /// </summary>
    [Precision(0)]
    public DateTimeOffset? LockedUntil { get; set; }
}
