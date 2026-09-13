using System.Text.Json;

namespace Backend.Helpers;

/// <summary>
/// Reads <c>voterId</c> from a JSON request body without consuming it for later model binding.
/// Used by auth rate limits that key on the identifier in the body.
/// </summary>
public static class JsonRequestVoterId
{
    internal const int MaxBodyBytes = 16 * 1024;

    /// <summary>
    /// Enables buffering, parses <c>voterId</c> / <c>VoterId</c> from JSON, then rewinds the body.
    /// Returns null when the body is missing, too large, not JSON, or has no non-empty voterId.
    /// </summary>
    public static async Task<string?> TryReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ContentLength is 0)
        {
            return null;
        }

        if (request.ContentLength is > MaxBodyBytes)
        {
            return null;
        }

        request.EnableBuffering();
        var body = request.Body;
        var originalPosition = body.CanSeek ? body.Position : 0;

        try
        {
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            return FindVoterId(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            if (body.CanSeek)
            {
                body.Position = originalPosition;
            }
        }
    }

    internal static string? FindVoterId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (!property.Name.Equals("voterId", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = property.Value.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        return null;
    }

    /// <summary>
    /// Normalizes a voterId for rate-limit keys (trim + case-fold).
    /// Does not rewrite phone shapes — auth still matches <c>Person.Phone == dto.VoterId</c>.
    /// </summary>
    public static string NormalizeForRateLimit(string voterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(voterId);
        return voterId.Trim().ToLowerInvariant();
    }
}
