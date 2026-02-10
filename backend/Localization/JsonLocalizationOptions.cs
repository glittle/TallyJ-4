namespace TallyJ4.Localization;

public class JsonLocalizationOptions
{
    public const string SectionName = "Localization";

    public string ResourcesPath { get; set; } = string.Empty;
    public string[] SupportedCultures { get; set; } = Array.Empty<string>();
    public string DefaultCulture { get; set; } = "en";
}
