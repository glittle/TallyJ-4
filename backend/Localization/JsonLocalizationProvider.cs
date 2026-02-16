using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace Backend.Localization;

public class JsonLocalizationProvider : IJsonLocalizationProvider
{
    private readonly IMemoryCache _cache;
    private readonly JsonLocalizationOptions _options;
    private readonly ILogger<JsonLocalizationProvider> _logger;

    public JsonLocalizationProvider(
        IMemoryCache cache,
        IOptions<JsonLocalizationOptions> options,
        ILogger<JsonLocalizationProvider> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public string? GetString(string key, CultureInfo culture)
    {
        var resources = GetResourcesForCulture(culture);
        return resources.TryGetValue(key, out var value) ? value : null;
    }

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

            _logger.LogInformation("Loading localization resources for culture: {Culture}", cultureName);

            var resources = new Dictionary<string, string>();
            var localeDirectory = Path.Combine(_options.ResourcesPath, cultureName);

            if (!Directory.Exists(localeDirectory))
            {
                _logger.LogWarning("Localization directory not found: {Directory}", localeDirectory);
                return resources;
            }

            var jsonFiles = Directory.GetFiles(localeDirectory, "*.json");
            _logger.LogInformation("Found {Count} JSON files in {Directory}", jsonFiles.Length, localeDirectory);

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    var jsonContent = File.ReadAllText(jsonFile);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

                    if (data != null)
                    {
                        foreach (var kvp in data)
                        {
                            if (!resources.ContainsKey(kvp.Key))
                            {
                                resources[kvp.Key] = kvp.Value;
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
}



