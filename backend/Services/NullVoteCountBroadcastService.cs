namespace Backend.Services;

internal sealed class NullVoteCountBroadcastService : IVoteCountBroadcastService
{
    public void QueueVoteCountUpdate(Guid personGuid, Guid electionGuid) { }
}
