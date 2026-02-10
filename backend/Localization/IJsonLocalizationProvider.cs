using System.Globalization;

namespace TallyJ4.Localization;

public interface IJsonLocalizationProvider
{
    string? GetString(string key, CultureInfo culture);
    Dictionary<string, string> GetAllStrings(CultureInfo culture);
}
