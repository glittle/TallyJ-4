using Microsoft.Extensions.Localization;
using System.Globalization;

namespace TallyJ4.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IJsonLocalizationProvider _provider;

    public JsonStringLocalizerFactory(IJsonLocalizationProvider provider)
    {
        _provider = provider;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return new JsonStringLocalizer(_provider, CultureInfo.CurrentUICulture);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new JsonStringLocalizer(_provider, CultureInfo.CurrentUICulture);
    }
}
