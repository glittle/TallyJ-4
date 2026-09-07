using System.Collections.Concurrent;

namespace Backend.Services;

/// <summary>
/// Process-wide ballot-page session tracker for online voters.
/// Same-host only — two app servers do not share this count.
/// </summary>
public sealed class OnlineVoterPresenceService : IOnlineVoterPresenceService
{
    private readonly ConcurrentDictionary<string, Guid> _sessions =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void AddSession(Guid electionGuid, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        if (electionGuid == Guid.Empty)
        {
            throw new ArgumentException("Election is required.", nameof(electionGuid));
        }

        _sessions[connectionId] = electionGuid;
    }

    /// <inheritdoc />
    public void RemoveSession(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        _sessions.TryRemove(connectionId, out _);
    }

    /// <inheritdoc />
    public int CountSessions(Guid electionGuid)
    {
        if (electionGuid == Guid.Empty)
        {
            return 0;
        }

        var count = 0;
        foreach (var pair in _sessions)
        {
            if (pair.Value == electionGuid)
            {
                count++;
            }
        }

        return count;
    }
}
