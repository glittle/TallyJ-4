namespace Backend.Helpers;

/// <summary>
/// Process-wide election-scoped lock so two Accept-all runs cannot create ballots
/// for the same pending online votes at the same time.
/// </summary>
public interface IOnlineBallotAcceptLock
{
    /// <summary>
    /// Tries to enter the lock for this election. False if another Accept-all is already running.
    /// </summary>
    bool TryEnter(Guid electionGuid);

    /// <summary>
    /// Releases the lock for this election. Safe if the lock was not held.
    /// </summary>
    void Exit(Guid electionGuid);
}
