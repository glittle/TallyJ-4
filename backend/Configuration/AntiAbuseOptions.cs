namespace Backend.Configuration;

/// <summary>
/// Limits and alert settings for paid verification sends and voter-list abuse checks.
/// Email login codes are not subject to the paid-send approval or the caps.
/// A freeze stops every channel, including email.
/// </summary>
public class AntiAbuseOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AntiAbuse";

    /// <summary>
    /// Default SMS, voice, and WhatsApp sends one election may make before a super admin raises the allowance.
    /// </summary>
    public const int DefaultElectionPaidSendAllowance = 25;

    /// <summary>
    /// Default SMS, voice, and WhatsApp sends one owner may make per UTC day across all of their elections.
    /// </summary>
    public const int DefaultOwnerDailyPaidSendCap = 100;

    /// <summary>
    /// Default hours before the same election or owner can email the super admin again for the same kind of alert.
    /// </summary>
    public const int DefaultAlertThrottleHours = 24;

    /// <summary>
    /// Default number of flagged voter-list entries that flags the whole election.
    /// The election is flagged when the count is greater than this number.
    /// </summary>
    public const int DefaultFlaggedEntryThreshold = 3;

    /// <summary>
    /// Default length of a consecutive phone run that is flagged.
    /// </summary>
    public const int DefaultConsecutivePhoneRunLength = 4;

    /// <summary>
    /// Default region when the election does not name expected phone countries.
    /// </summary>
    public const string DefaultPhoneRegion = "CA";

    /// <summary>
    /// Default seconds to wait for one MX lookup before treating the domain as unknown.
    /// </summary>
    public const int DefaultMxLookupTimeoutSeconds = 3;

    /// <summary>
    /// Default number of MX lookups in flight at once.
    /// </summary>
    public const int DefaultMxLookupParallelism = 8;

    /// <summary>
    /// Default SMS, voice, and WhatsApp sends allowed per destination prefix in one sliding hour,
    /// across every election. Email is not counted.
    /// </summary>
    public const int DefaultPhonePrefixSendLimit = 100;

    /// <summary>
    /// Default length of the sliding window for <see cref="DefaultPhonePrefixSendLimit"/>.
    /// </summary>
    public const int DefaultPhonePrefixWindowMinutes = 60;

    /// <summary>
    /// Default number of leading E.164 digits (after <c>+</c>) that form a destination prefix.
    /// Six digits is the country code plus the next digits.
    /// </summary>
    public const int DefaultPhonePrefixDigits = 6;

    /// <summary>
    /// Default minimum time for a <c>requestCode</c> reply, so a fast reject is not instant.
    /// </summary>
    public const int DefaultRequestCodeMinimumMilliseconds = 200;

    /// <summary>
    /// SMS, voice, and WhatsApp sends one election may make before a super admin raises the allowance.
    /// </summary>
    public int ElectionPaidSendAllowance { get; set; } = DefaultElectionPaidSendAllowance;

    /// <summary>
    /// SMS, voice, and WhatsApp sends one owner may make per UTC day across all of their elections.
    /// </summary>
    public int OwnerDailyPaidSendCap { get; set; } = DefaultOwnerDailyPaidSendCap;

    /// <summary>
    /// Hours before the same alert key can email the super admin again.
    /// </summary>
    public int AlertThrottleHours { get; set; } = DefaultAlertThrottleHours;

    /// <summary>
    /// Addresses that receive abuse alerts. When empty, <c>SuperAdmin:Emails</c> is used.
    /// </summary>
    public string[] AlertEmails { get; set; } = Array.Empty<string>();

    /// <summary>
    /// An upload or an election total with more flagged entries than this flags the election.
    /// </summary>
    public int FlaggedEntryThreshold { get; set; } = DefaultFlaggedEntryThreshold;

    /// <summary>
    /// A run of this many sequential phone numbers is flagged.
    /// </summary>
    public int ConsecutivePhoneRunLength { get; set; } = DefaultConsecutivePhoneRunLength;

    /// <summary>
    /// ISO region used when an election has no expected phone countries.
    /// </summary>
    public string DefaultPhoneRegionCode { get; set; } = DefaultPhoneRegion;

    /// <summary>
    /// Seconds before an MX lookup is unknown rather than flagged.
    /// </summary>
    public int MxLookupTimeoutSeconds { get; set; } = DefaultMxLookupTimeoutSeconds;

    /// <summary>
    /// How many distinct domains are looked up at once during an import.
    /// </summary>
    public int MxLookupParallelism { get; set; } = DefaultMxLookupParallelism;

    /// <summary>
    /// SMS, voice, and WhatsApp login codes allowed per destination prefix in the sliding window,
    /// across the whole site. Email codes are not limited by this number.
    /// </summary>
    public int PhonePrefixSendLimit { get; set; } = DefaultPhonePrefixSendLimit;

    /// <summary>
    /// Length of the sliding window for <see cref="PhonePrefixSendLimit"/>, in minutes.
    /// </summary>
    public int PhonePrefixWindowMinutes { get; set; } = DefaultPhonePrefixWindowMinutes;

    /// <summary>
    /// How many leading E.164 digits, after <c>+</c>, identify a destination prefix.
    /// </summary>
    public int PhonePrefixDigits { get; set; } = DefaultPhonePrefixDigits;

    /// <summary>
    /// Minimum milliseconds before <c>requestCode</c> returns.
    /// Zero returns as soon as the work is done. A negative value uses the default.
    /// </summary>
    public int RequestCodeMinimumMilliseconds { get; set; } = DefaultRequestCodeMinimumMilliseconds;

    /// <summary>
    /// Allowance after applying the default when the configured value is below zero.
    /// </summary>
    public int ResolvedElectionPaidSendAllowance =>
        ElectionPaidSendAllowance < 0 ? DefaultElectionPaidSendAllowance : ElectionPaidSendAllowance;

    /// <summary>
    /// Daily cap after applying the default when the configured value is below zero.
    /// </summary>
    public int ResolvedOwnerDailyPaidSendCap =>
        OwnerDailyPaidSendCap < 0 ? DefaultOwnerDailyPaidSendCap : OwnerDailyPaidSendCap;

    /// <summary>
    /// Throttle window after applying the default when the configured value is below 1.
    /// </summary>
    public int ResolvedAlertThrottleHours =>
        AlertThrottleHours < 1 ? DefaultAlertThrottleHours : AlertThrottleHours;

    /// <summary>
    /// Flag threshold after applying the default when the configured value is below zero.
    /// </summary>
    public int ResolvedFlaggedEntryThreshold =>
        FlaggedEntryThreshold < 0 ? DefaultFlaggedEntryThreshold : FlaggedEntryThreshold;

    /// <summary>
    /// Consecutive-run length after applying the default when the configured value is below 2.
    /// </summary>
    public int ResolvedConsecutivePhoneRunLength =>
        ConsecutivePhoneRunLength < 2 ? DefaultConsecutivePhoneRunLength : ConsecutivePhoneRunLength;

    /// <summary>
    /// MX timeout after applying the default when the configured value is below 1.
    /// </summary>
    public int ResolvedMxLookupTimeoutSeconds =>
        MxLookupTimeoutSeconds < 1 ? DefaultMxLookupTimeoutSeconds : MxLookupTimeoutSeconds;

    /// <summary>
    /// MX parallelism after applying the default when the configured value is below 1.
    /// </summary>
    public int ResolvedMxLookupParallelism =>
        MxLookupParallelism < 1 ? DefaultMxLookupParallelism : MxLookupParallelism;

    /// <summary>
    /// Prefix cap after applying the default when the configured value is below 1.
    /// </summary>
    public int ResolvedPhonePrefixSendLimit =>
        PhonePrefixSendLimit < 1 ? DefaultPhonePrefixSendLimit : PhonePrefixSendLimit;

    /// <summary>
    /// Prefix window after applying the default when the configured value is below 1.
    /// </summary>
    public int ResolvedPhonePrefixWindowMinutes =>
        PhonePrefixWindowMinutes < 1 ? DefaultPhonePrefixWindowMinutes : PhonePrefixWindowMinutes;

    /// <summary>
    /// Prefix length after applying the default when the configured value is outside 1–15.
    /// </summary>
    public int ResolvedPhonePrefixDigits =>
        PhonePrefixDigits is < 1 or > 15 ? DefaultPhonePrefixDigits : PhonePrefixDigits;

    /// <summary>
    /// Reply floor after applying the default when the configured value is below zero.
    /// Zero is kept, so tests can turn the wait off.
    /// </summary>
    public int ResolvedRequestCodeMinimumMilliseconds =>
        RequestCodeMinimumMilliseconds < 0
            ? DefaultRequestCodeMinimumMilliseconds
            : RequestCodeMinimumMilliseconds;
}
