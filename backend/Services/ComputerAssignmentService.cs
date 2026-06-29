using System.Collections.Concurrent;
using Backend.DTOs.Computers;
using Backend.Helpers;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Services;

/// <summary>
/// In-memory computer code assignment and connection tracking per election.
/// </summary>
public class ComputerAssignmentService : IComputerAssignmentService, IDisposable
{
    public static readonly TimeSpan DefaultGuestCloseoutDelay = TimeSpan.FromMinutes(60);

    private readonly ConcurrentDictionary<Guid, ElectionAssignmentState> _elections = new();
    private readonly IHubContext<MainHub> _mainHubContext;
    private readonly ISignalRNotificationService? _signalRNotificationService;
    private readonly ILogger<ComputerAssignmentService> _logger;
    private readonly TimeSpan _guestCloseoutDelay;
    private readonly object _stateLock = new();

    public ComputerAssignmentService(
        IHubContext<MainHub> mainHubContext,
        ILogger<ComputerAssignmentService> logger,
        ISignalRNotificationService signalRNotificationService)
        : this(mainHubContext, logger, signalRNotificationService, DefaultGuestCloseoutDelay)
    {
    }

    internal ComputerAssignmentService(
        IHubContext<MainHub> mainHubContext,
        ILogger<ComputerAssignmentService> logger,
        TimeSpan guestCloseoutDelay)
        : this(mainHubContext, logger, null, guestCloseoutDelay)
    {
    }

    internal ComputerAssignmentService(
        IHubContext<MainHub> mainHubContext,
        ILogger<ComputerAssignmentService> logger,
        ISignalRNotificationService? signalRNotificationService,
        TimeSpan guestCloseoutDelay)
    {
        _mainHubContext = mainHubContext;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
        _guestCloseoutDelay = guestCloseoutDelay;
    }

    /// <inheritdoc />
    public string AssignCode(Guid electionGuid, string clientId, string connectionId, bool isMainTeller)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var normalizedClientId = clientId.Trim();
        ReleaseConnectionFromOtherElections(connectionId, electionGuid);

        var state = _elections.GetOrAdd(electionGuid, _ => new ElectionAssignmentState());
        var refreshGuestJoinList = false;
        string assignedCode;

        lock (_stateLock)
        {
            var previousMainCount = state.Connections.Values.Count(c => c.IsMainTeller);

            foreach (var existing in state.Connections.Values
                         .Where(c => c.ClientId == normalizedClientId)
                         .ToList())
            {
                state.Connections.TryRemove(existing.ConnectionId, out _);
            }

            var activeCodes = state.Connections.Values
                .Select(c => c.ComputerCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (state.ClientIdToLastCode.TryGetValue(normalizedClientId, out var previousCode)
                && ComputerCodeHelper.IsValidCode(previousCode)
                && !activeCodes.Contains(previousCode))
            {
                assignedCode = ComputerCodeHelper.NormalizeCode(previousCode);
            }
            else
            {
                state.HighestAssignedIndex++;
                assignedCode = ComputerCodeHelper.IndexToCode(state.HighestAssignedIndex);
            }

            state.Connections[connectionId] = new ActiveConnection
            {
                ConnectionId = connectionId,
                ClientId = normalizedClientId,
                ComputerCode = assignedCode,
                IsMainTeller = isMainTeller,
                ElectionGuid = electionGuid,
                ConnectedAt = DateTimeOffset.UtcNow,
            };

            state.ClientIdToLastCode[normalizedClientId] = assignedCode;

            var newMainCount = state.Connections.Values.Count(c => c.IsMainTeller);
            ApplyGuestCloseoutPolicy(state, electionGuid, previousMainCount, newMainCount);
            refreshGuestJoinList = GuestCloseoutPolicy.ShouldRefreshGuestJoinList(
                previousMainCount,
                newMainCount);

            _logger.LogInformation(
                "Assigned computer code {ComputerCode} to client {ClientId} (connection {ConnectionId}, main={IsMainTeller}) for election {ElectionGuid}",
                assignedCode,
                normalizedClientId,
                connectionId,
                isMainTeller,
                electionGuid);
        }

        if (refreshGuestJoinList)
        {
            NotifyGuestJoinListRefresh();
        }

        return assignedCode;
    }

    /// <inheritdoc />
    public void ReleaseConnection(string connectionId)
    {
        Guid? electionGuid = null;
        ElectionAssignmentState? state = null;
        ActiveConnection? removed = null;
        var refreshGuestJoinList = false;

        lock (_stateLock)
        {
            foreach (var (guid, electionState) in _elections)
            {
                if (electionState.Connections.TryRemove(connectionId, out removed))
                {
                    electionGuid = guid;
                    state = electionState;
                    break;
                }
            }

            if (state == null || removed == null || electionGuid == null)
            {
                return;
            }

            _logger.LogInformation(
                "Released computer code {ComputerCode} from connection {ConnectionId} for election {ElectionGuid}",
                removed.ComputerCode,
                connectionId,
                electionGuid);

            if (state.Connections.IsEmpty)
            {
                CancelGuestCloseoutTimer(state, electionGuid.Value);
                refreshGuestJoinList = removed.IsMainTeller;
                state.ClientIdToLastCode.Clear();
                state.HighestAssignedIndex = -1;
                _elections.TryRemove(electionGuid.Value, out _);
                _logger.LogInformation(
                    "All workstations disconnected from election {ElectionGuid}; code assignment will restart at A",
                    electionGuid);
            }
            else
            {
                var newMainCount = state.Connections.Values.Count(c => c.IsMainTeller);
                var previousMainCount = removed.IsMainTeller ? newMainCount + 1 : newMainCount;
                ApplyGuestCloseoutPolicy(state, electionGuid.Value, previousMainCount, newMainCount);
                refreshGuestJoinList = GuestCloseoutPolicy.ShouldRefreshGuestJoinList(
                    previousMainCount,
                    newMainCount);
            }
        }

        if (refreshGuestJoinList)
        {
            NotifyGuestJoinListRefresh();
        }
    }

    /// <inheritdoc />
    public bool HasActiveMainTeller(Guid electionGuid)
    {
        if (!_elections.TryGetValue(electionGuid, out var state))
        {
            return false;
        }

        return state.Connections.Values.Any(c => c.IsMainTeller);
    }

    /// <inheritdoc />
    public bool IsGuestGracePeriodActive(Guid electionGuid)
    {
        if (!_elections.TryGetValue(electionGuid, out var state))
        {
            return false;
        }

        return state.GuestCloseoutTimerCts != null;
    }

    /// <inheritdoc />
    public bool CanGuestJoin(Guid electionGuid) =>
        HasActiveMainTeller(electionGuid) || IsGuestGracePeriodActive(electionGuid);

    /// <inheritdoc />
    public IReadOnlyList<ActiveComputerDto> GetActiveComputers(Guid electionGuid)
    {
        if (!_elections.TryGetValue(electionGuid, out var state))
        {
            return Array.Empty<ActiveComputerDto>();
        }

        return state.Connections.Values
            .OrderBy(c => ComputerCodeHelper.CodeToIndex(c.ComputerCode))
            .Select(c => new ActiveComputerDto
            {
                ComputerCode = c.ComputerCode,
                ClientId = c.ClientId,
                IsMainTeller = c.IsMainTeller,
                ConnectedAt = c.ConnectedAt,
            })
            .ToList();
    }

    public void Dispose()
    {
        foreach (var state in _elections.Values)
        {
            state.GuestCloseoutTimerCts?.Cancel();
            state.GuestCloseoutTimerCts?.Dispose();
        }
    }

    private void NotifyGuestJoinListRefresh()
    {
        if (_signalRNotificationService == null)
        {
            return;
        }

        _ = _signalRNotificationService.SendPublicElectionListUpdateAsync();
    }

    /// <summary>
    /// A single workstation may only be tracked for one election at a time.
    /// </summary>
    private void ReleaseConnectionFromOtherElections(string connectionId, Guid targetElectionGuid)
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var hasOtherElection = false;
            lock (_stateLock)
            {
                hasOtherElection = _elections.Any(kvp =>
                    kvp.Key != targetElectionGuid &&
                    kvp.Value.Connections.ContainsKey(connectionId));
            }

            if (!hasOtherElection)
            {
                return;
            }

            ReleaseConnection(connectionId);
        }

        _logger.LogWarning(
            "Connection {ConnectionId} still registered in another election after repeated release attempts (target {ElectionGuid})",
            connectionId,
            targetElectionGuid);
    }

    private void ApplyGuestCloseoutPolicy(
        ElectionAssignmentState state,
        Guid electionGuid,
        int previousMainCount,
        int newMainCount)
    {
        switch (GuestCloseoutPolicy.Decide(previousMainCount, newMainCount))
        {
            case GuestCloseoutAction.StartTimer:
                StartGuestCloseoutTimer(state, electionGuid);
                break;
            case GuestCloseoutAction.CancelTimer:
                CancelGuestCloseoutTimer(state, electionGuid);
                break;
        }
    }

    private void StartGuestCloseoutTimer(ElectionAssignmentState state, Guid electionGuid)
    {
        if (state.GuestCloseoutTimerCts != null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        state.GuestCloseoutTimerCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_guestCloseoutDelay, cts.Token);
                await CloseOutGuestTellersAsync(electionGuid);
            }
            catch (OperationCanceledException)
            {
                // Timer cancelled because a main teller reconnected.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guest teller close-out timer failed for election {ElectionGuid}", electionGuid);
            }
            finally
            {
                lock (_stateLock)
                {
                    if (state.GuestCloseoutTimerCts == cts)
                    {
                        cts.Dispose();
                        state.GuestCloseoutTimerCts = null;
                    }
                }
            }
        }, CancellationToken.None);

        _logger.LogInformation(
            "Started {Minutes}-minute guest teller close-out timer for election {ElectionGuid}",
            _guestCloseoutDelay.TotalMinutes,
            electionGuid);
    }

    private void CancelGuestCloseoutTimer(ElectionAssignmentState state, Guid electionGuid)
    {
        if (state.GuestCloseoutTimerCts == null)
        {
            return;
        }

        state.GuestCloseoutTimerCts.Cancel();
        state.GuestCloseoutTimerCts.Dispose();
        state.GuestCloseoutTimerCts = null;

        _logger.LogInformation(
            "Cancelled guest teller close-out timer for election {ElectionGuid}",
            electionGuid);
    }

    private async Task CloseOutGuestTellersAsync(Guid electionGuid)
    {
        lock (_stateLock)
        {
            if (!_elections.TryGetValue(electionGuid, out var state))
            {
                return;
            }

            if (state.Connections.Values.Any(c => c.IsMainTeller))
            {
                return;
            }
        }

        var guestGroup = $"Main{electionGuid}Guest";
        await _mainHubContext.Clients.Group(guestGroup).SendAsync("electionClosed");

        _logger.LogInformation(
            "Guest tellers closed out for election {ElectionGuid} after main teller absence timeout",
            electionGuid);
    }

    private sealed class ElectionAssignmentState
    {
        public ConcurrentDictionary<string, ActiveConnection> Connections { get; } = new();

        public Dictionary<string, string> ClientIdToLastCode { get; } = new(StringComparer.Ordinal);

        public int HighestAssignedIndex { get; set; } = -1;

        public CancellationTokenSource? GuestCloseoutTimerCts { get; set; }
    }

    private sealed class ActiveConnection
    {
        public required string ConnectionId { get; init; }

        public required string ClientId { get; init; }

        public required string ComputerCode { get; init; }

        public required bool IsMainTeller { get; init; }

        public required Guid ElectionGuid { get; init; }

        public required DateTimeOffset ConnectedAt { get; init; }
    }
}