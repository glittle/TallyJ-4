using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Backend.Localization;

/// <summary>
/// Factory for creating JsonStringLocalizer instances.
/// </summary>
public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IJsonLocalizationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the JsonStringLocalizerFactory.
    /// </summary>
    /// <param name="provider">The JSON localization provider.</param>
    public JsonStringLocalizerFactory(IJsonLocalizationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Creates a string localizer for the specified resource source type.
    /// </summary>
    /// <param name="resourceSource">The type of the resource source (ignored).</param>
    /// <returns>A new JsonStringLocalizer instance.</returns>
    public IStringLocalizer Create(Type resourceSource)
    {
        return new JsonStringLocalizer(_provider, CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Creates a string localizer for the specified base name and location.
    /// </summary>
    /// <param name="baseName">The base name of the resource (ignored).</param>
    /// <param name="location">The location of the resource (ignored).</param>
    /// <returns>A new JsonStringLocalizer instance.</returns>
    public IStringLocalizer Create(string baseName, string location)
    {
        return new JsonStringLocalizer(_provider, CultureInfo.CurrentUICulture);
    }
}



