using System.Collections.Concurrent;

namespace Backend.Helpers;

/// <summary>
/// In-process lock keyed by election. Serializes Accept-all on one host so a
/// second overlapping call gets 409 instead of starting another run (the v3
/// incident: a second call started while the first was still creating ballots).
/// Process-wide only — another instance, or a voter Submit that loaded Submitted
/// before Accept committed, is not covered. Duplicate prevention is the DB
/// compare-and-swap from Submitted to Processed.
/// </summary>
public sealed class OnlineBallotAcceptLock : IOnlineBallotAcceptLock
{
    private readonly ConcurrentDictionary<Guid, byte> _locks = new();

    public bool TryEnter(Guid electionGuid) => _locks.TryAdd(electionGuid, 0);

    public void Exit(Guid electionGuid) => _locks.TryRemove(electionGuid, out _);
}
