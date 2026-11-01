using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Backend.Localization;

/// <summary>
/// Implementation of IJsonLocalizationProvider that loads localized strings from JSON files.
/// Supports caching and fallback to default culture.
/// </summary>
public class JsonLocalizationProvider : IJsonLocalizationProvider
{
    private readonly IMemoryCache _cache;
    private readonly JsonLocalizationOptions _options;
    private readonly ILogger<JsonLocalizationProvider> _logger;
    private readonly string _resourcesPath;

    /// <summary>
    /// Initializes a new instance of the JsonLocalizationProvider.
    /// </summary>
    /// <param name="cache">The memory cache for storing loaded localization resources.</param>
    /// <param name="options">The localization configuration options.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public JsonLocalizationProvider(
        IMemoryCache cache,
        IOptions<JsonLocalizationOptions> options,
        ILogger<JsonLocalizationProvider> logger,
        IHostEnvironment? hostEnvironment = null)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _resourcesPath = LocalizationPathResolver.Resolve(_options.ResourcesPath, hostEnvironment?.ContentRootPath);
    }

    /// <summary>
    /// Gets a localized string for the specified key and culture.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="culture">The culture for which to get the localized string.</param>
    /// <returns>The localized string, or null if not found.</returns>
    public string? GetString(string key, CultureInfo culture)
    {
        var resources = GetResourcesForCulture(culture);
        return resources.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets all localized strings for the specified culture.
    /// </summary>
    /// <param name="culture">The culture for which to get all localized strings.</param>
    /// <returns>A dictionary containing all key-value pairs for the culture.</returns>
    public Dictionary<string, string> GetAllStrings(CultureInfo culture)
    {
        return GetResourcesForCulture(culture);
    }

    private Dictionary<string, string> GetResourcesForCulture(CultureInfo culture)
    {
        var cultureName = culture.TwoLetterISOLanguageName;
        var cacheKey = $"Localization_{cultureName}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            entry.Priority = CacheItemPriority.Normal;

            var resources = new Dictionary<string, string>();
            var localeDirectory = Path.Combine(_resourcesPath, cultureName);

            if (!Directory.Exists(localeDirectory))
            {
                // Published hosts often have no frontend/src/locales tree. Skip quietly.
                _logger.LogDebug("Localization directory not found: {Directory}. Skipping.", localeDirectory);
                return resources;
            }

            _logger.LogInformation("Loading localization resources for culture: {Culture}", cultureName);

            var jsonFiles = Directory.GetFiles(localeDirectory, "*.json");
            _logger.LogInformation("Found {Count} JSON files in {Directory}", jsonFiles.Length, localeDirectory);

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    var jsonContent = File.ReadAllText(jsonFile);
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

                    if (data != null)
                    {
                        foreach (var kvp in data)
                        {
                            if (!TryReadMessageText(kvp.Value, out var text))
                            {
                                _logger.LogWarning(
                                    "Skipping non-message value for key '{Key}' in {File}.",
                                    kvp.Key,
                                    Path.GetFileName(jsonFile));
                                continue;
                            }

                            if (!resources.ContainsKey(kvp.Key))
                            {
                                resources[kvp.Key] = text;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Duplicate key '{Key}' found in {File}. Skipping.",
                                    kvp.Key,
                                    Path.GetFileName(jsonFile));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading localization file: {File}", jsonFile);
                }
            }

            _logger.LogInformation("Loaded {Count} translation keys for culture: {Culture}", resources.Count, cultureName);
            return resources;
        }) ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Reads the text of a message leaf.
    /// Source files store <c>{ t, s, w }</c>; <c>t</c> is the string the API returns.
    /// A bare string is still accepted so older catalogs and tests keep loading.
    /// </summary>
    private static bool TryReadMessageText(JsonElement value, out string text)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            text = value.GetString() ?? string.Empty;
            return true;
        }

        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("t", out var textElement)
            && textElement.ValueKind == JsonValueKind.String)
        {
            text = textElement.GetString() ?? string.Empty;
            return true;
        }

        text = string.Empty;
        return false;
    }
}



