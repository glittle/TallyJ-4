using Microsoft.Extensions.Configuration;

namespace Backend.Helpers;

/// <summary>
/// Absolute URL Twilio should POST for message/call status (v3 <c>twilio-CallbackUrl</c>).
/// Must match the public <c>POST /api/Public/smsStatus</c> endpoint Twilio can reach.
/// </summary>
public static class TwilioStatusCallbackUrl
{
    /// <summary>
    /// Path of the single public status-callback endpoint.
    /// </summary>
    public const string Path = "/api/Public/smsStatus";

    /// <summary>
    /// Optional full callback URL (v3 <c>twilio-CallbackUrl</c>). Used as-is when it is
    /// a usable absolute http(s) URL, not a <c>&lt;placeholder&gt;</c>.
    /// </summary>
    public const string ConfigKey = "Twilio:StatusCallbackUrl";

    /// <summary>
    /// Resolves the StatusCallback URL, or null when none is usable (omit the form field).
    /// Order: <see cref="ConfigKey"/>, then <c>ClientEnv:apiUrl</c> + <see cref="Path"/>,
    /// then <c>ClientEnv:frontendUrl</c> + <see cref="Path"/> (same-host production).
    /// </summary>
    public static string? TryResolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (TryAbsoluteHttpUrl(configuration[ConfigKey], out var explicitUrl))
        {
            if (explicitUrl.AbsolutePath is "" or "/")
            {
                return new Uri(explicitUrl, Path).ToString();
            }

            return explicitUrl.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }

        if (TryAbsoluteHttpUrl(configuration["ClientEnv:apiUrl"], out var apiOrigin))
        {
            return new Uri(apiOrigin, Path).ToString();
        }

        if (TryAbsoluteHttpUrl(configuration["ClientEnv:frontendUrl"], out var frontendOrigin))
        {
            return new Uri(frontendOrigin, Path).ToString();
        }

        return null;
    }

    private static bool TryAbsoluteHttpUrl(string? raw, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith('<'))
        {
            return false;
        }

        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            return false;
        }

        uri = parsed;
        return true;
    }
}
