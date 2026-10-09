using System.Diagnostics;
using Backend.Configuration;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Pads <c>requestCode</c> so a reject is not instant compared with a listed voter
/// whose provider call has not started yet. It does not imitate a full provider
/// round trip.
/// </summary>
public class RequestCodePacer : IRequestCodePacer
{
    private readonly TimeSpan _minimum;

    /// <summary>
    /// Initializes the pacer from <see cref="AntiAbuseOptions.RequestCodeMinimumMilliseconds"/>.
    /// Zero waits nothing. Tests set zero so the suite does not sleep on every code request.
    /// </summary>
    public RequestCodePacer(IOptions<AntiAbuseOptions> options)
    {
        var milliseconds = options.Value.ResolvedRequestCodeMinimumMilliseconds;
        _minimum = TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <inheritdoc />
    public async Task PaceAsync(long startedTimestamp, CancellationToken cancellationToken = default)
    {
        if (_minimum <= TimeSpan.Zero)
        {
            return;
        }

        var remaining = _minimum - Stopwatch.GetElapsedTime(startedTimestamp);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, cancellationToken);
        }
    }
}
