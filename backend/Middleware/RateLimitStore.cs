using System.Collections.Concurrent;

namespace Backend.Middleware;

/// <summary>
/// Singleton store for rate limiting state. Extracted to allow resetting in tests.
/// </summary>
public class RateLimitStore
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();

    public ConcurrentDictionary<string, List<DateTime>> RequestLog => _requestLog;

    /// <summary>
    /// Clears all recorded requests, resetting rate limit counters.
    /// </summary>
    public void Reset() => _requestLog.Clear();
}
