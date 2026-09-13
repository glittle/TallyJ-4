using System.Net;
using System.Net.Sockets;

namespace Backend.Helpers;

/// <summary>
/// Extensions for HttpContext to easily access correlation ID and other request-specific data
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Client IP for rate-limit keys and audit fields.
    /// When the TCP peer is an infrastructure hop (loopback, private, or unspecified),
    /// uses the rightmost public address from X-Forwarded-For / Forwarded — the hop
    /// Azure App Service / Front Door appended. Otherwise uses the connection address
    /// and ignores client-supplied forwarded headers.
    /// </summary>
    public static string GetClientIpAddress(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (IsInfrastructurePeer(remoteIp) &&
            TryGetTrustedIngressClientIp(httpContext.Request, out var forwardedIp))
        {
            return forwardedIp;
        }

        return remoteIp?.ToString() ?? "unknown";
    }

    /// <summary>
    /// True when the TCP peer is a reverse-proxy / platform hop we can recognize
    /// without a proxy allow-list (TestServer, App Service ARR, Docker).
    /// A public RemoteIpAddress is treated as the connecting client; XFF is not trusted.
    /// </summary>
    internal static bool IsInfrastructurePeer(IPAddress? remoteIp)
    {
        return remoteIp is null || IsNonPublicAddress(remoteIp);
    }

    /// <summary>
    /// Rightmost public IP from X-Forwarded-For (preferred) or RFC 7239 Forwarded.
    /// Azure Front Door and App Service append the connecting socket IP; leftmost
    /// entries are client-supplied and must not choose the rate-limit bucket.
    /// </summary>
    internal static bool TryGetTrustedIngressClientIp(HttpRequest request, out string ip)
    {
        if (TryGetRightmostPublicIp(ReadXForwardedForAddresses(request), out ip))
        {
            return true;
        }

        return TryGetRightmostPublicIp(ReadForwardedForAddresses(request), out ip);
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
                if (TryParseIp(part, out var parsed))
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

    private static bool TryGetRightmostPublicIp(IReadOnlyList<IPAddress> addresses, out string ip)
    {
        for (var i = addresses.Count - 1; i >= 0; i--)
        {
            var candidate = NormalizeMappedAddress(addresses[i]);
            if (!IsNonPublicAddress(candidate))
            {
                ip = candidate.ToString();
                return true;
            }
        }

        ip = string.Empty;
        return false;
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

                if (TryParseIp(raw, out var parsed))
                {
                    addresses.Add(parsed);
                }
            }
        }
    }

    internal static bool IsNonPublicAddress(IPAddress address)
    {
        address = NormalizeMappedAddress(address);

        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal ||
            address.IsIPv6SiteLocal ||
            address.IsIPv6UniqueLocal)
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
               || bytes[0] == 127
               || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
               || (bytes[0] == 192 && bytes[1] == 168)
               || (bytes[0] == 169 && bytes[1] == 254)
               || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127);
    }

    private static IPAddress NormalizeMappedAddress(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private static bool TryParseIp(string candidate, out IPAddress parsed)
    {
        return IPAddress.TryParse(candidate, out parsed!);
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
