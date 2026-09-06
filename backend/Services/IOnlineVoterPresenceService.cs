namespace Backend.Services;

/// <summary>
/// In-memory count of authenticated online-voter SignalR sessions that have
/// joined a specific election's ballot page. Stores connection ids only —
/// no voter id, person name, or contact.
/// </summary>
public interface IOnlineVoterPresenceService
{
    /// <summary>
    /// Records that this AllVotersHub connection is on the given election's
    /// ballot page. Moving the same connection to another election replaces
    /// the previous session.
    /// </summary>
    void AddSession(Guid electionGuid, string connectionId);

    /// <summary>
    /// Drops this connection from whatever election it was counted in.
    /// Unknown connection ids are ignored.
    /// </summary>
    void RemoveSession(string connectionId);

    /// <summary>
    /// Anonymous session count for this election's ballot page. One person
    /// with two tabs counts as two. Not a "building a ballot" count.
    /// </summary>
    int CountSessions(Guid electionGuid);
}
