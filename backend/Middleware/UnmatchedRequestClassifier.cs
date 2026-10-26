using Microsoft.AspNetCore.Diagnostics;

namespace Backend.Middleware;

/// <summary>
/// Decides which responses are unmatched probes rather than application results.
/// </summary>
public static class UnmatchedRequestClassifier
{
    /// <summary>
    /// Item set by the framework when the pipeline ends without running an endpoint.
    /// Matches <c>ApplicationBuilder</c>'s private <c>__RequestUnhandled</c> key.
    /// </summary>
    public const string RequestUnhandledItemKey = "__RequestUnhandled";

    /// <summary>
    /// Display name of the endpoint ASP.NET Core uses for HTTP 405 method mismatch.
    /// Matches <c>HttpMethodMatcherPolicy.Http405EndpointDisplayName</c>.
    /// </summary>
    public const string MethodNotAllowedDisplayName = "405 HTTP Method Not Supported";

    /// <summary>
    /// Event id of Hosting's "request reached the end of the middleware pipeline" log.
    /// </summary>
    public const int HostingRequestUnhandledEventId = 16;

    public const string HostingDiagnosticsCategory = "Microsoft.AspNetCore.Hosting.Diagnostics";

    public const string UnhandledMessageFragment =
        "without being handled by application code";

    /// <summary>
    /// Set on the request after it is classified as unmatched, so logs written while the
    /// response is being completed can be filtered without an AsyncLocal.
    /// </summary>
    public const string SuppressLoggingItemKey = "__UnmatchedRequestSuppressLogging";

    /// <summary>
    /// True for a 404 that no endpoint handled, or for the framework's 405 method rejection.
    /// A matched endpoint that returns 404 for missing data is not unmatched.
    /// The SPA history fallback is a matched endpoint and is not unmatched.
    /// </summary>
    public static bool IsUnmatched(HttpContext context)
    {
        if (context.Features.Get<IExceptionHandlerFeature>()?.Error is not null)
        {
            return false;
        }

        var status = context.Response.StatusCode;
        if (status == StatusCodes.Status404NotFound
            && context.Items.ContainsKey(RequestUnhandledItemKey))
        {
            return true;
        }

        if (status != StatusCodes.Status405MethodNotAllowed)
        {
            return false;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return context.Items.ContainsKey(RequestUnhandledItemKey);
        }

        return string.Equals(
            endpoint.DisplayName,
            MethodNotAllowedDisplayName,
            StringComparison.Ordinal);
    }
}
