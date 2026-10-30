using System.Text.RegularExpressions;

namespace Backend.Helpers;

/// <summary>
/// Short provider or block text safe to store and show to a teller.
/// Strips addresses, long digit runs, and URLs so a provider echo cannot
/// put a phone, an email, or a token into the result.
/// </summary>
public static partial class AnnouncementErrorText
{
    /// <summary>
    /// Maximum length stored on <c>PersonLastAnnouncement.Error</c> and returned on a result row.
    /// </summary>
    public const int MaxLength = 200;

    /// <summary>
    /// Returns a clipped reason, or null when nothing safe remains.
    /// </summary>
    public static string? Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        text = UrlPattern().Replace(text, "***");
        text = EmailPattern().Replace(text, "***");
        text = DigitRunPattern().Replace(text, "***");
        text = text.Trim();
        if (text.Length == 0 || text.All(ch => ch == '*' || char.IsWhiteSpace(ch)))
        {
            return null;
        }

        return text.Length <= MaxLength ? text : text[..MaxLength];
    }

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"[^\s@]+@[^\s@]+\.[^\s@]+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\d{6,}")]
    private static partial Regex DigitRunPattern();
}
