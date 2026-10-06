namespace Backend.Configuration;

/// <summary>
/// Limits for shared-passcode guest teller login.
/// Account sign-in for owners and admins is not covered here.
/// </summary>
public class TellerLoginProtectionOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "TellerLoginProtection";

    /// <summary>
    /// Default number of consecutive wrong passcodes before that election's guest teller login locks.
    /// </summary>
    public const int DefaultMaxConsecutiveFailures = 10;

    /// <summary>
    /// Default minutes guest teller login stays locked after the failure threshold.
    /// </summary>
    public const int DefaultCooldownMinutes = 15;

    /// <summary>
    /// Default minimum length when an owner sets a new teller passcode.
    /// </summary>
    public const int DefaultMinimumPasscodeLength = 6;

    /// <summary>
    /// Consecutive wrong passcodes for one election before guest teller login locks.
    /// Values below 1 use <see cref="DefaultMaxConsecutiveFailures"/>.
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = DefaultMaxConsecutiveFailures;

    /// <summary>
    /// Minutes guest teller login stays locked after <see cref="MaxConsecutiveFailures"/>.
    /// Values below 1 use <see cref="DefaultCooldownMinutes"/>.
    /// </summary>
    public int CooldownMinutes { get; set; } = DefaultCooldownMinutes;

    /// <summary>
    /// Minimum length for a passcode an owner creates or changes.
    /// A stored passcode shorter than this still works at login.
    /// Values below 1 use <see cref="DefaultMinimumPasscodeLength"/>.
    /// </summary>
    public int MinimumPasscodeLength { get; set; } = DefaultMinimumPasscodeLength;

    /// <summary>
    /// Failure count that starts a lockout. A configured value below 1 uses the default.
    /// </summary>
    public int ResolvedMaxConsecutiveFailures =>
        MaxConsecutiveFailures < 1 ? DefaultMaxConsecutiveFailures : MaxConsecutiveFailures;

    /// <summary>
    /// Lockout length in minutes. A configured value below 1 uses the default.
    /// </summary>
    public int ResolvedCooldownMinutes =>
        CooldownMinutes < 1 ? DefaultCooldownMinutes : CooldownMinutes;

    /// <summary>
    /// Minimum length applied when a passcode is created or changed.
    /// A configured value below 1 uses the default.
    /// </summary>
    public int ResolvedMinimumPasscodeLength =>
        MinimumPasscodeLength < 1 ? DefaultMinimumPasscodeLength : MinimumPasscodeLength;
}
