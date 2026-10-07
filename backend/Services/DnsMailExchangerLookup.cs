using System.Collections.Concurrent;
using Backend.Configuration;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// MX lookup with a timeout and an in-memory cache. A timeout is unknown, not a missing MX.
/// </summary>
public sealed class DnsMailExchangerLookup : IMailExchangerLookup
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);

    private readonly LookupClient _client;
    private readonly TimeSpan _timeout;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the lookup from <see cref="AntiAbuseOptions.MxLookupTimeoutSeconds"/>.
    /// </summary>
    public DnsMailExchangerLookup(IOptions<AntiAbuseOptions> options)
    {
        var seconds = options.Value.ResolvedMxLookupTimeoutSeconds;
        _timeout = TimeSpan.FromSeconds(seconds);
        _client = new LookupClient(new LookupClientOptions
        {
            Timeout = _timeout,
            UseCache = false,
            Retries = 0
        });
    }

    /// <inheritdoc />
    public async Task<MxLookupResult> LookupAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(domain, out var cached) && cached.Until > DateTimeOffset.UtcNow)
        {
            return cached.Result;
        }

        MxLookupResult result;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);
            var response = await _client.QueryAsync(domain, QueryType.MX, QueryClass.IN, timeout.Token);
            result = Map(response);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MxLookupResult.Unknown;
        }
        catch (DnsResponseException)
        {
            return MxLookupResult.Unknown;
        }
        catch (Exception)
        {
            return MxLookupResult.Unknown;
        }

        if (result != MxLookupResult.Unknown)
        {
            _cache[domain] = new CacheEntry(result, DateTimeOffset.UtcNow.Add(CacheLifetime));
        }

        return result;
    }

    private static MxLookupResult Map(IDnsQueryResponse response)
    {
        if (response.HasError)
        {
            if (response.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain)
            {
                return MxLookupResult.NoMx;
            }

            return MxLookupResult.Unknown;
        }

        var records = response.Answers.OfType<MxRecord>().ToList();
        if (records.Count == 0)
        {
            return MxLookupResult.NoMx;
        }

        var real = records.Where(record => !IsNullMx(record.Exchange.Value)).ToList();
        if (real.Count == 0)
        {
            return MxLookupResult.NullMx;
        }

        return MxLookupResult.HasMx;
    }

    private static bool IsNullMx(string exchange)
    {
        var value = exchange.Trim().TrimEnd('.');
        return value.Length == 0;
    }

    private sealed record CacheEntry(MxLookupResult Result, DateTimeOffset Until);
}
