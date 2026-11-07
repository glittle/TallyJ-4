namespace Backend.Middleware;

/// <summary>
/// Counts unmatched 404s and framework 405s. Does not write a log line per request.
/// </summary>
public sealed class UnmatchedRequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly UnmatchedRequestTracker _tracker;

    public UnmatchedRequestMiddleware(RequestDelegate next, UnmatchedRequestTracker tracker)
    {
        _next = next;
        _tracker = tracker;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (!UnmatchedRequestClassifier.IsUnmatched(context))
        {
            return;
        }

        _tracker.Record(context.Request.Path.Value);
        UnmatchedRequestSentry.MarkCurrentScope();
        context.Items[UnmatchedRequestClassifier.SuppressLoggingItemKey] = true;
    }
}
