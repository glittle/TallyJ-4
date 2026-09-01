using System.Collections.Concurrent;

namespace Backend.Helpers;

/// <summary>
/// In-process lock keyed by election. Combined with per-row Status == Submitted
/// inside the Accept-all transaction, this is what stops duplicate regular ballots
/// from overlapping Accept-all calls (the v3 incident: a second call started while
/// the first was still creating ballots).
/// </summary>
public sealed class OnlineBallotAcceptLock : IOnlineBallotAcceptLock
{
    private readonly ConcurrentDictionary<Guid, byte> _locks = new();

    public bool TryEnter(Guid electionGuid) => _locks.TryAdd(electionGuid, 0);

    public void Exit(Guid electionGuid) => _locks.TryRemove(electionGuid, out _);
}
