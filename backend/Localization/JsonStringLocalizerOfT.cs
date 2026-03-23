using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Backend.Localization;

/// <summary>
/// Generic implementation of IStringLocalizer&lt;T&gt; that uses JSON-based localization.
/// </summary>
/// <typeparam name="T">The type for which this localizer provides strings.</typeparam>
public class JsonStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly IStringLocalizer _localizer;

    /// <summary>
    /// Initializes a new instance of the JsonStringLocalizer&lt;T&gt;.
    /// </summary>
    /// <param name="provider">The JSON localization provider.</param>
    public JsonStringLocalizer(IJsonLocalizationProvider provider)
    {
        _localizer = new JsonStringLocalizer(provider, CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Gets the localized string for the specified name.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <returns>The localized string.</returns>
    public LocalizedString this[string name] => _localizer[name];

    /// <summary>
    /// Gets the localized string for the specified name with formatting arguments.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="arguments">The arguments to format the string with.</param>
    /// <returns>The localized and formatted string.</returns>
    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    /// <summary>
    /// Gets all localized strings for the current culture.
    /// </summary>
    /// <param name="includeParentCultures">Whether to include strings from parent cultures.</param>
    /// <returns>An enumerable of localized strings.</returns>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _localizer.GetAllStrings(includeParentCultures);
    }
}



