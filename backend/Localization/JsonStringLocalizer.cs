using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Backend.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly IJsonLocalizationProvider _provider;
    private readonly CultureInfo _culture;

    public JsonStringLocalizer(IJsonLocalizationProvider provider, CultureInfo culture)
    {
        _provider = provider;
        _culture = culture;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetString(name);
            var value = format == null ? name : string.Format(format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format == null);
        }
    }

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



