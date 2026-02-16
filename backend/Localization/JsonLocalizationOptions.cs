namespace Backend.Localization;

/// <summary>
/// Configuration options for JSON-based localization.
/// </summary>
public class JsonLocalizationOptions
{
    /// <summary>
    /// The configuration section name for localization settings.
    /// </summary>
    public const string SectionName = "Localization";

    /// <summary>
    /// The path to the directory containing localization resource files.
    /// </summary>
    public string ResourcesPath { get; set; } = string.Empty;

    /// <summary>
    /// Array of supported culture codes (e.g., "en", "fr", "es").
    /// </summary>
    public string[] SupportedCultures { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The default culture to use when a requested culture is not available. Defaults to "en".
    /// </summary>
    public string DefaultCulture { get; set; } = "en";
}



