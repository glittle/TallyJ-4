using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Backend.Localization;

/// <summary>
/// Implementation of IStringLocalizer that uses JSON-based localization provider.
/// </summary>
public class JsonStringLocalizer : IStringLocalizer
{
    private readonly IJsonLocalizationProvider _provider;
    private readonly CultureInfo _culture;

    /// <summary>
    /// Initializes a new instance of the JsonStringLocalizer.
    /// </summary>
    /// <param name="provider">The JSON localization provider.</param>
    /// <param name="culture">The culture for which this localizer provides strings.</param>
    public JsonStringLocalizer(IJsonLocalizationProvider provider, CultureInfo culture)
    {
        _provider = provider;
        _culture = culture;
    }

    /// <summary>
    /// Gets the localized string for the specified name.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <returns>The localized string.</returns>
    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
        }
    }

    /// <summary>
    /// Gets the localized string for the specified name with formatting arguments.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="arguments">The arguments to format the string with.</param>
    /// <returns>The localized and formatted string.</returns>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetString(name);
            var value = format == null ? name : string.Format(format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format == null);
        }
    }

    /// <summary>
    /// Gets all localized strings for the current culture.
    /// </summary>
    /// <param name="includeParentCultures">Whether to include strings from parent cultures (not supported).</param>
    /// <returns>An enumerable of localized strings.</returns>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var resources = _provider.GetAllStrings(_culture);
        foreach (var resource in resources)
        {
            yield return new LocalizedString(resource.Key, resource.Value, resourceNotFound: false);
        }
    }

    private string? GetString(string name)
    {
        return _provider.GetString(name, _culture);
    }
}



