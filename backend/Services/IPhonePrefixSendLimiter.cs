namespace Backend.Services;

/// <summary>
/// System-wide cap on SMS, voice, and WhatsApp login codes per destination prefix.
/// Email is not counted and is not blocked.
/// </summary>
public interface IPhonePrefixSendLimiter
{
    /// <summary>
    /// Takes one slot for a paid send when the sliding window is still under the cap.
    /// Returns <see cref="PhonePrefixSendResult.Allowed"/> false when the send must not happen.
    /// </summary>
    Task<PhonePrefixSendResult> TryConsumeAsync(
        string channel,
        string destination,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a prefix check. <see cref="Prefix"/> is set when the number parsed.
/// <see cref="BlockReason"/> is null when the send may proceed.
/// </summary>
public sealed record PhonePrefixSendResult(bool Allowed, string? Prefix, string? BlockReason);
