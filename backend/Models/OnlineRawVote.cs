using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Models;

/// <summary>
/// Represents a raw vote entry from online voting or imported ballot data.
/// Stored as JSON on <c>Vote.OnlineVoteRaw</c> (v3-compatible PascalCase).
/// </summary>
public class OnlineRawVote
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineRawVote"/> class.
    /// </summary>
    public OnlineRawVote()
    {
        // Need this for JSON deserializing
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineRawVote"/> class with text to parse.
    /// </summary>
    /// <param name="text">The text to parse for first and last names.</param>
    public OnlineRawVote(string text)
    {
        // This constructor used by CDN ballot importer

        OtherInfo = text;

        // Do a rough guess at first and last name
        First = "";
        Last = "";

        // Likely   first last
        //     or   last, first

        if (text.Contains(','))
        {
            var split = text.Split(new[] { ',' }, 2);
            Last = split[0].Trim();
            First = split[1].Trim();
        }
        else
        {
            var split = text.Split(' ');

            // If > 2 words, cannot guess which are for first name or last name. Default to last word --> Last
            Last = split.Last();
            First = string.Join(" ", split.Reverse().Skip(1).Reverse());
        }
    }

    /// <summary>
    /// Gets or sets the ID of the vote.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the first name extracted from the vote text.
    /// </summary>
    public string First { get; set; } = "";

    /// <summary>
    /// Gets or sets the last name extracted from the vote text.
    /// </summary>
    public string Last { get; set; } = "";

    /// <summary>
    /// Gets or sets the original vote text.
    /// </summary>
    public string OtherInfo { get; set; } = "";

    /// <summary>
    /// First + last when present, otherwise the original text.
    /// </summary>
    public string ToDisplayName()
    {
        var name = $"{First} {Last}".Trim();
        return string.IsNullOrEmpty(name) ? OtherInfo ?? "" : name;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Parses stored <c>OnlineVoteRaw</c>: v3/v4 JSON, or a legacy plain name string.
    /// </summary>
    public static OnlineRawVote Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new OnlineRawVote();
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<OnlineRawVote>(trimmed, JsonOptions);
                if (parsed != null)
                {
                    parsed.First ??= "";
                    parsed.Last ??= "";
                    parsed.OtherInfo ??= "";
                    return parsed;
                }
            }
            catch (JsonException)
            {
                // Fall through to free-text parsing
            }
        }

        return new OnlineRawVote(text);
    }
}
