using System.Threading;
using Backend.Helpers;

namespace Backend.Middleware;

/// <summary>
/// Middleware that generates short correlation IDs for requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private static int _counter = 0;
    private static readonly AsyncLocal<string> _currentCorrelationId = new();

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a short correlation ID using a counter
        var correlationId = GenerateShortCorrelationId();
        context.Items["CorrelationId"] = correlationId;

        // Also set it in AsyncLocal for logging that doesn't have HttpContext
        _currentCorrelationId.Value = correlationId;

        await _next(context);
    }

    /// <summary>
    /// Gets the current correlation ID from AsyncLocal storage
    /// </summary>
    public static string? CurrentCorrelationId => _currentCorrelationId.Value;

    private static string GenerateShortCorrelationId()
    {
        // Use Interlocked to make this thread-safe
        var id = Interlocked.Increment(ref _counter);

        // Convert to base36 (alphanumeric) for shorter representation
        // This gives us IDs like "1", "2", "A", "B", etc.
        return ToBase36(id);
    }

    private static string ToBase36(int value)
    {
        if (value == 0) return "0";

        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sb = new System.Text.StringBuilder();

        while (value > 0)
        {
            sb.Insert(0, chars[value % 36]);
            value /= 36;
        }

        return sb.ToString();
    }
}