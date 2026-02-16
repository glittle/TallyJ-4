using System.Globalization;

namespace Backend.Localization;

public interface IJsonLocalizationProvider
{
    string? GetString(string key, CultureInfo culture);
    Dictionary<string, string> GetAllStrings(CultureInfo culture);
}



