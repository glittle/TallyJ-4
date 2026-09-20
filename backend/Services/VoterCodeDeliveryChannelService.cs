using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Backend.DTOs.SignalR;

namespace Backend.Services;

/// <summary>
/// In-memory voter-code delivery channels. Same-host only — two app servers do not share tokens.
/// </summary>
public sealed class VoterCodeDeliveryChannelService : IVoterCodeDeliveryChannelService
{
    /// <summary>
    /// Channel lifetime. Shorter than the 15-minute OTP so a leaked token dies before the code.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    internal const int TokenBytes = 32;
    private const int MaxBufferedStatuses = 16;

    private readonly TimeProvider _clock;
    private readonly ConcurrentDictionary<string, ChannelState> _byTokenHash = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _channelIdToHash = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _sidToHash = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _connectionToHash = new(StringComparer.Ordinal);

    public VoterCodeDeliveryChannelService(TimeProvider? timeProvider = null)
    {
        _clock = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public VoterCodeChannelIssue Issue()
    {
        SweepExpired();

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes)).ToLowerInvariant();
        var hash = HashToken(rawToken);
        var channelId = Guid.NewGuid().ToString("N");
        var now = _clock.GetUtcNow();
        var state = new ChannelState
        {
            ChannelId = channelId,
            ExpiresAt = now.Add(Lifetime)
        };

        _byTokenHash[hash] = state;
        _channelIdToHash[channelId] = hash;

        return new VoterCodeChannelIssue
        {
            RawToken = rawToken,
            ChannelId = channelId
        };
    }

    /// <inheritdoc />
    public bool TryJoin(string rawToken, string connectionId, out VoterCodeChannelJoin join)
    {
        join = null!;
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(connectionId))
        {
            return false;
        }

        SweepExpired();
        var hash = HashToken(rawToken.Trim());
        if (!_byTokenHash.TryGetValue(hash, out var state))
        {
            return false;
        }

        if (state.IsExpired(_clock.GetUtcNow()))
        {
            Remove(hash, state);
            return false;
        }

        lock (state.Sync)
        {
            if (state.JoinedConnectionId != null
                && !string.Equals(state.JoinedConnectionId, connectionId, StringComparison.Ordinal))
            {
                return false;
            }

            state.JoinedConnectionId = connectionId;
            _connectionToHash[connectionId] = hash;
            join = new VoterCodeChannelJoin
            {
                ChannelId = state.ChannelId,
                BufferedStatuses = state.Statuses.ToArray()
            };
            return true;
        }
    }

    /// <inheritdoc />
    public void Leave(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        if (!_connectionToHash.TryRemove(connectionId, out var hash))
        {
            return;
        }

        if (!_byTokenHash.TryGetValue(hash, out var state))
        {
            return;
        }

        lock (state.Sync)
        {
            if (string.Equals(state.JoinedConnectionId, connectionId, StringComparison.Ordinal))
            {
                state.JoinedConnectionId = null;
            }
        }
    }

    /// <inheritdoc />
    public void BindProviderSid(string channelId, string providerSid)
    {
        if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(providerSid))
        {
            return;
        }

        if (!_channelIdToHash.TryGetValue(channelId, out var hash))
        {
            return;
        }

        if (!_byTokenHash.TryGetValue(hash, out var state) || state.IsExpired(_clock.GetUtcNow()))
        {
            return;
        }

        lock (state.Sync)
        {
            state.ProviderSid = providerSid.Trim();
        }

        _sidToHash[providerSid.Trim()] = hash;
    }

    /// <inheritdoc />
    public bool TryGetChannelIdByProviderSid(string providerSid, out string channelId)
    {
        channelId = "";
        if (string.IsNullOrWhiteSpace(providerSid))
        {
            return false;
        }

        if (!_sidToHash.TryGetValue(providerSid.Trim(), out var hash))
        {
            return false;
        }

        if (!_byTokenHash.TryGetValue(hash, out var state) || state.IsExpired(_clock.GetUtcNow()))
        {
            return false;
        }

        channelId = state.ChannelId;
        return true;
    }

    /// <inheritdoc />
    public void RecordStatus(string channelId, VoterCodeDeliveryStatusDto status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (string.IsNullOrWhiteSpace(channelId))
        {
            return;
        }

        if (!_channelIdToHash.TryGetValue(channelId, out var hash))
        {
            return;
        }

        if (!_byTokenHash.TryGetValue(hash, out var state) || state.IsExpired(_clock.GetUtcNow()))
        {
            return;
        }

        lock (state.Sync)
        {
            if (state.Statuses.Count >= MaxBufferedStatuses)
            {
                state.Statuses.RemoveAt(0);
            }

            state.Statuses.Add(CloneStatus(status));
        }
    }

    internal static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void SweepExpired()
    {
        var now = _clock.GetUtcNow();
        foreach (var pair in _byTokenHash)
        {
            if (pair.Value.IsExpired(now))
            {
                Remove(pair.Key, pair.Value);
            }
        }
    }

    private void Remove(string hash, ChannelState state)
    {
        _byTokenHash.TryRemove(hash, out _);
        _channelIdToHash.TryRemove(state.ChannelId, out _);
        if (!string.IsNullOrEmpty(state.ProviderSid))
        {
            _sidToHash.TryRemove(state.ProviderSid, out _);
        }

        if (!string.IsNullOrEmpty(state.JoinedConnectionId))
        {
            _connectionToHash.TryRemove(state.JoinedConnectionId, out _);
        }
    }

    private static VoterCodeDeliveryStatusDto CloneStatus(VoterCodeDeliveryStatusDto status) =>
        new()
        {
            Status = status.Status,
            MessageKey = status.MessageKey,
            Okay = status.Okay,
            ProviderStatus = VoterCodeDeliveryStatuses.SanitizeProviderStatus(status.ProviderStatus)
        };

    private sealed class ChannelState
    {
        public required string ChannelId { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public string? JoinedConnectionId { get; set; }
        public string? ProviderSid { get; set; }
        public List<VoterCodeDeliveryStatusDto> Statuses { get; } = [];
        public object Sync { get; } = new();

        public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    }
}
