using System.Net;

namespace Backend.Helpers;

/// <summary>
/// Extensions for HttpContext to easily access correlation ID and other request-specific data
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Client IP for rate-limit keys and audit fields.
    /// Prefers Connection.RemoteIpAddress after UseForwardedHeaders has applied
    /// X-Forwarded-For / Forwarded with AuthForwardedHeaders.ForwardLimit.
    /// Parses those headers only when remote is null, walking from the right
    /// with the same limit (leftmost is forgeable when proxies append).
    /// </summary>
    public static string GetClientIpAddress(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is not null)
        {
            return remoteIp.ToString();
        }

        if (TryApplyForwardedForHopLimit(httpContext.Request, AuthForwardedHeaders.ForwardLimit, out var forwardedIp))
        {
            return forwardedIp;
        }

        return "unknown";
    }

    /// <summary>
    /// Applies the same right-to-left walk as UseForwardedHeaders: consume up to
    /// <paramref name="hopLimit"/> addresses from the right; the last consumed
    /// value is the platform-corrected client. Used only when RemoteIpAddress is unset.
    /// </summary>
    internal static bool TryApplyForwardedForHopLimit(HttpRequest request, int hopLimit, out string ip)
    {
        var addresses = ReadXForwardedForAddresses(request);
        if (addresses.Count == 0)
        {
            addresses = ReadForwardedForAddresses(request);
        }

        if (addresses.Count == 0 || hopLimit <= 0)
        {
            ip = string.Empty;
            return false;
        }

        IPAddress? chosen = null;
        var applied = 0;
        for (var i = addresses.Count - 1; i >= 0 && applied < hopLimit; i--)
        {
            chosen = addresses[i];
            applied++;
        }

        ip = chosen!.ToString();
        return true;
    }

    internal static IReadOnlyList<IPAddress> ReadXForwardedForAddresses(HttpRequest request)
    {
        var addresses = new List<IPAddress>();
        if (!request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return addresses;
        }

        foreach (var headerValue in forwardedFor)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                continue;
            }

            foreach (var part in headerValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (IPAddress.TryParse(part, out var parsed))
                {
                    addresses.Add(parsed);
                }
            }
        }

        return addresses;
    }

    internal static IReadOnlyList<IPAddress> ReadForwardedForAddresses(HttpRequest request)
    {
        var addresses = new List<IPAddress>();
        if (!request.Headers.TryGetValue("Forwarded", out var forwarded))
        {
            return addresses;
        }

        foreach (var headerValue in forwarded)
        {
            AppendRfc7239ForwardedFor(headerValue, addresses);
        }

        return addresses;
    }

    private static void AppendRfc7239ForwardedFor(string? header, List<IPAddress> addresses)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return;
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

                if (IPAddress.TryParse(raw, out var parsed))
                {
                    addresses.Add(parsed);
                }
            }
        }
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
