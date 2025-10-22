namespace TallyJ4.Backend.Helpers;

/// <summary>
/// Extensions for HttpContext to easily access correlation ID and other request-specific data
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the correlation ID for the current request
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>The correlation ID (TraceIdentifier) for this request</returns>
    public static string GetCorrelationId(this HttpContext httpContext)
    {
        return httpContext.TraceIdentifier;
    }

    /// <summary>
    /// Gets the correlation ID from HttpContext.Items if available, otherwise from TraceIdentifier
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>The correlation ID for this request</returns>
    public static string? TryGetCorrelationId(this HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        // Try to get from Items first (set by middleware)
        if (
          httpContext.Items.TryGetValue("CorrelationId", out var correlationId)
          && correlationId is string id
        )
        {
            return id;
        }

        // Fallback to TraceIdentifier
        return httpContext.TraceIdentifier;
    }
}