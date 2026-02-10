using Microsoft.Extensions.Localization;
using System.Globalization;

namespace TallyJ4.Localization;

public class JsonStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly IStringLocalizer _localizer;

    public JsonStringLocalizer(IJsonLocalizationProvider provider)
    {
        _localizer = new JsonStringLocalizer(provider, CultureInfo.CurrentUICulture);
    }

    public LocalizedString this[string name] => _localizer[name];

    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _localizer.GetAllStrings(includeParentCultures);
    }
}
