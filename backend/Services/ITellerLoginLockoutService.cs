namespace Backend.Services;

/// <summary>
/// Result of recording one wrong teller passcode for an election.
/// </summary>
public readonly record struct TellerPasscodeFailureResult(
    bool IsLocked,
    bool LockoutStarted,
    DateTimeOffset? LockedUntil,
    int ConsecutiveFailures);

/// <summary>
/// Persists per-election guest teller passcode failures and the lockout window.
/// </summary>
public interface ITellerLoginLockoutService
{
    /// <summary>
    /// True when shared-passcode login for this election is inside its lockout window.
    /// </summary>
    Task<bool> IsLockedAsync(Guid electionGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds one consecutive failure. At the configured threshold, starts the cooldown
    /// and reports <see cref="TellerPasscodeFailureResult.LockoutStarted"/>.
    /// A failure during an active lock does not add another count or start another lock.
    /// </summary>
    Task<TellerPasscodeFailureResult> RecordPasscodeFailureAsync(
        Guid electionGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the failure count and any lock.
    /// Returns true when a row had failures or a lock and this call cleared them.
    /// </summary>
    Task<bool> ResetAsync(Guid electionGuid, CancellationToken cancellationToken = default);
}
