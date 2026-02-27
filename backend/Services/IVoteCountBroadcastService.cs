namespace Backend.Services;

/// <summary>
/// Service interface for batching and broadcasting vote count updates.
/// Collects vote count changes and broadcasts them periodically to reduce SignalR traffic.
/// </summary>
public interface IVoteCountBroadcastService
{
    /// <summary>
    /// Queues a person's vote count for periodic broadcast.
    /// The actual broadcast will happen on the next scheduled interval (approximately every 15 seconds).
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person whose vote count changed.</param>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    void QueueVoteCountUpdate(Guid personGuid, Guid electionGuid);
}
