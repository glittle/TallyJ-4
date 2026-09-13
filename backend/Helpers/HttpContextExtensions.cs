using System.Net;

namespace Backend.Helpers;

/// <summary>
/// Extensions for HttpContext to easily access correlation ID and other request-specific data
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Client IP for rate-limit keys and audit fields.
    /// Prefers the original client from X-Forwarded-For / Forwarded (leftmost valid IP)
    /// when a proxy hop is present; otherwise uses the connection remote address.
    /// </summary>
    public static string GetClientIpAddress(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (TryReadForwardedClientIp(httpContext.Request, out var forwardedIp))
        {
            return forwardedIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Reads the original client IP from proxy headers. Leftmost X-Forwarded-For
    /// entry is the originating client; later entries are successive proxies.
    /// </summary>
    internal static bool TryReadForwardedClientIp(HttpRequest request, out string ip)
    {
        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            foreach (var headerValue in forwardedFor)
            {
                if (string.IsNullOrWhiteSpace(headerValue))
                {
                    continue;
                }

                foreach (var part in headerValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (TryNormalizeIp(part, out ip))
                    {
                        return true;
                    }
                }
            }
        }

        if (request.Headers.TryGetValue("Forwarded", out var forwarded))
        {
            foreach (var headerValue in forwarded)
            {
                if (TryParseRfc7239ForwardedFor(headerValue, out ip))
                {
                    return true;
                }
            }
        }

        ip = string.Empty;
        return false;
    }

    private static bool TryParseRfc7239ForwardedFor(string? header, out string ip)
    {
        ip = string.Empty;
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        foreach (var element in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var pair in element.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!pair.StartsWith("for=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var raw = pair[4..].Trim().Trim('"');
                if (raw.StartsWith('['))
                {
                    var end = raw.IndexOf(']');
                    if (end > 1)
                    {
                        raw = raw[1..end];
                    }
                }
                else
                {
                    var lastColon = raw.LastIndexOf(':');
                    if (lastColon > 0 && raw.Contains('.'))
                    {
                        raw = raw[..lastColon];
                    }
                }

                if (TryNormalizeIp(raw, out ip))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryNormalizeIp(string candidate, out string ip)
    {
        ip = string.Empty;
        if (!IPAddress.TryParse(candidate, out var parsed))
        {
            return false;
        }

        ip = parsed.ToString();
        return true;
    }

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


