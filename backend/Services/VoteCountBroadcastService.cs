using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.DTOs.SignalR;

namespace Backend.Services;

/// <summary>
/// Background service that batches vote count changes and broadcasts them periodically.
/// Reduces SignalR traffic by collecting changes and sending updates every 15 seconds.
/// </summary>
public class VoteCountBroadcastService : BackgroundService, IVoteCountBroadcastService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoteCountBroadcastService> _logger;
    private readonly ConcurrentDictionary<(Guid electionGuid, Guid personGuid), bool> _pendingUpdates = new();
    private readonly TimeSpan _broadcastInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Initializes a new instance of the VoteCountBroadcastService.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to access scoped services.</param>
    /// <param name="logger">Logger for recording broadcast operations.</param>
    public VoteCountBroadcastService(
        IServiceScopeFactory scopeFactory,
        ILogger<VoteCountBroadcastService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Queues a person's vote count for the next periodic broadcast.
    /// This method is thread-safe and can be called from multiple threads simultaneously.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person whose vote count changed.</param>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    public void QueueVoteCountUpdate(Guid personGuid, Guid electionGuid)
    {
        _pendingUpdates.TryAdd((electionGuid, personGuid), true);
        _logger.LogDebug("Queued vote count update for person {PersonGuid} in election {ElectionGuid}",
            personGuid, electionGuid);
    }

    /// <summary>
    /// Background task that runs continuously, broadcasting pending vote count updates every 15 seconds.
    /// </summary>
    /// <param name="stoppingToken">Token to signal when the service should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Vote count broadcast service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_broadcastInterval, stoppingToken);
                await BroadcastPendingUpdatesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in vote count broadcast service");
            }
        }

        _logger.LogInformation("Vote count broadcast service stopped");
    }

    /// <summary>
    /// Broadcasts all pending vote count updates to connected clients.
    /// Retrieves current vote counts from the database and sends them via SignalR.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task BroadcastPendingUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_pendingUpdates.IsEmpty)
        {
            return;
        }

        // Take snapshot of pending updates and clear the queue
        var updates = _pendingUpdates.Keys.ToList();
        _pendingUpdates.Clear();

        if (updates.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Broadcasting {Count} vote count updates", updates.Count);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

        foreach (var (electionGuid, personGuid) in updates)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                // Get the current vote count for this person in this election
                var voteCount = await context.Votes
                    .CountAsync(v => v.PersonGuid == personGuid &&
                                   v.Ballot.Location.ElectionGuid == electionGuid,
                        cancellationToken);

                await signalRService.SendPersonVoteCountUpdateAsync(new PersonVoteCountUpdateDto
                {
                    ElectionGuid = electionGuid,
                    PersonGuid = personGuid,
                    VoteCount = voteCount
                });

                _logger.LogDebug("Broadcast vote count {VoteCount} for person {PersonGuid} in election {ElectionGuid}",
                    voteCount, personGuid, electionGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting vote count for person {PersonGuid} in election {ElectionGuid}",
                    personGuid, electionGuid);
            }
        }
    }
}
