using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;

namespace Backend.Helpers;

/// <summary>
/// Twilio <c>X-Twilio-Signature</c> validation (HMAC-SHA1 of URL + sorted POST params).
/// Same algorithm as Twilio <c>RequestValidator</c>; implemented here so we do not
/// take a Twilio SDK dependency for the status-callback path.
/// </summary>
public static class TwilioRequestSignature
{
    public const string HeaderName = "X-Twilio-Signature";

    /// <summary>
    /// True when <paramref name="authToken"/> is a real Twilio token (not missing or a
    /// <c>&lt;placeholder&gt;</c> like <c>PaidVerificationSender</c> uses).
    /// </summary>
    public static bool IsAuthTokenConfigured(string? authToken) =>
        !string.IsNullOrWhiteSpace(authToken) && !authToken.StartsWith('<');

    /// <summary>
    /// Validates the request against <paramref name="authToken"/>. Missing token,
    /// missing signature, or mismatch all return false. Does not log or inspect phones.
    /// </summary>
    public static bool IsValid(string? authToken, HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsAuthTokenConfigured(authToken))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            return false;
        }

        var expected = headerValues.ToString();
        if (string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var url = request.GetEncodedUrl();
        var form = request.HasFormContentType
            ? request.Form.ToDictionary(p => p.Key, p => p.Value.ToString(), StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        return IsValid(authToken!, url, form, expected);
    }

    /// <summary>
    /// Validates a pre-built URL + form against a signature (unit tests / Twilio docs vector).
    /// </summary>
    public static bool IsValid(
        string authToken,
        string url,
        IReadOnlyDictionary<string, string> form,
        string? signature)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(url))
        {
            return false;
        }

        var computed = Compute(authToken, url, form);
        var expectedBytes = Encoding.UTF8.GetBytes(signature);
        var computedBytes = Encoding.UTF8.GetBytes(computed);
        if (expectedBytes.Length != computedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, computedBytes);
    }

    /// <summary>
    /// HMAC-SHA1 (Base64) of <paramref name="url"/> plus each POST key and value
    /// in ordinal key order, with no delimiter — Twilio RequestValidator.
    /// </summary>
    public static string Compute(string authToken, string url, IReadOnlyDictionary<string, string> form)
    {
        ArgumentException.ThrowIfNullOrEmpty(authToken);
        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentNullException.ThrowIfNull(form);

        var builder = new StringBuilder(url);
        foreach (var key in form.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            builder.Append(key).Append(form[key]);
        }

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToBase64String(hash);
    }
}
