using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Backend.Helpers;

/// <summary>
/// Shared UseForwardedHeaders settings for Azure App Service + Front Door.
/// Rate-limit keying uses RemoteIpAddress after this middleware runs;
/// the same hop limit is used if headers must be parsed when remote is unset.
/// </summary>
public static class AuthForwardedHeaders
{
    /// <summary>
    /// Two platform hops (Front Door, then App Service). Matches
    /// ForwardedHeadersOptions.ForwardLimit. Entries to the left of that
    /// window are client-supplied and must not choose the rate-limit bucket.
    /// </summary>
    public const int ForwardLimit = 2;

    public static void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
#pragma warning disable ASPDEPR005
        // .NET 10 dual-list: clear both so platform hops are trusted (issue #63627).
        options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
        options.ForwardLimit = ForwardLimit;
    }
}
