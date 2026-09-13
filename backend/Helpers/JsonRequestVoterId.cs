using System.Text.Json;

namespace Backend.Helpers;

public enum JsonRequestVoterIdStatus
{
    Found,
    Missing,
    TooLarge
}

public readonly record struct JsonRequestVoterIdRead(JsonRequestVoterIdStatus Status, string? VoterId)
{
    public static JsonRequestVoterIdRead Found(string voterId) =>
        new(JsonRequestVoterIdStatus.Found, voterId);

    public static JsonRequestVoterIdRead Missing() =>
        new(JsonRequestVoterIdStatus.Missing, null);

    public static JsonRequestVoterIdRead TooLarge() =>
        new(JsonRequestVoterIdStatus.TooLarge, null);
}

/// <summary>
/// Reads <c>voterId</c> from a JSON request body without consuming it for later model binding.
/// Used by auth rate limits that key on the identifier in the body.
/// </summary>
public static class JsonRequestVoterId
{
    internal const int MaxBodyBytes = 16 * 1024;

    /// <summary>
    /// Parses <c>voterId</c> / <c>VoterId</c> from JSON, then leaves a rewindable body
    /// for model binding when the body fits in <see cref="MaxBodyBytes"/>.
    /// <see cref="JsonRequestVoterIdStatus.TooLarge"/> is returned when ContentLength
    /// exceeds the cap or a chunked peek overflows — the caller must reject, not
    /// treat that as a missing identifier. Empty / malformed / absent voterId is
    /// <see cref="JsonRequestVoterIdStatus.Missing"/>.
    /// </summary>
    public static async Task<JsonRequestVoterIdRead> TryReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ContentLength is 0)
        {
            return JsonRequestVoterIdRead.Missing();
        }

        if (request.ContentLength is > MaxBodyBytes)
        {
            return JsonRequestVoterIdRead.TooLarge();
        }

        var capped = await ReadAtMostAsync(request.Body, MaxBodyBytes, cancellationToken);
        if (capped == null)
        {
            return JsonRequestVoterIdRead.TooLarge();
        }

        request.Body = new MemoryStream(capped, writable: false);
        request.ContentLength = capped.Length;

        if (capped.Length == 0)
        {
            return JsonRequestVoterIdRead.Missing();
        }

        try
        {
            using var document = JsonDocument.Parse(capped);
            var voterId = FindVoterId(document.RootElement);
            return voterId == null
                ? JsonRequestVoterIdRead.Missing()
                : JsonRequestVoterIdRead.Found(voterId);
        }
        catch (JsonException)
        {
            return JsonRequestVoterIdRead.Missing();
        }
    }

    /// <summary>
    /// Reads at most <paramref name="maxBytes"/> from <paramref name="body"/>.
    /// Returns null if the stream has more than that (overflow); otherwise the bytes read.
    /// Stops after <paramref name="maxBytes"/> + 1 so a chunked body cannot be slurped.
    /// </summary>
    internal static async Task<byte[]?> ReadAtMostAsync(Stream body, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[maxBytes + 1];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await body.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        if (total > maxBytes)
        {
            return null;
        }

        if (total == 0)
        {
            return [];
        }

        return buffer.AsSpan(0, total).ToArray();
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
