namespace Backend.Helpers;

/// <summary>
/// Process-wide election-scoped lock. A second Accept-all on the same host is
/// refused (HTTP 409) while one is running. This does not make duplicates
/// impossible across hosts or against Submit; that is the DB compare-and-swap
/// on <c>OnlineVotingInfo.Status</c>.
/// </summary>
public interface IOnlineBallotAcceptLock
{
    /// <summary>
    /// Tries to enter the lock for this election. False if another Accept-all is already running on this process.
    /// </summary>
    bool TryEnter(Guid electionGuid);

    /// <summary>
    /// Releases the lock for this election. Safe if the lock was not held.
    /// </summary>
    void Exit(Guid electionGuid);
}
