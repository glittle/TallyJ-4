using System.Globalization;

namespace Backend.Localization;

/// <summary>
/// Interface for providing localized strings from JSON resource files.
/// </summary>
public interface IJsonLocalizationProvider
{
    /// <summary>
    /// Gets a localized string for the specified key and culture.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="culture">The culture for which to get the localized string.</param>
    /// <returns>The localized string, or null if not found.</returns>
    string? GetString(string key, CultureInfo culture);

    /// <summary>
    /// Gets all localized strings for the specified culture.
    /// </summary>
    /// <param name="culture">The culture for which to get all localized strings.</param>
    /// <returns>A dictionary containing all key-value pairs for the culture.</returns>
    Dictionary<string, string> GetAllStrings(CultureInfo culture);
}



