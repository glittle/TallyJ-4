using Microsoft.Extensions.Localization;

namespace Backend.Localization;

/// <summary>
/// Extension methods for configuring JSON-based localization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds JSON-based localization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Optional configuration action for localization options.</param>
    /// <returns>The service collection with localization services added.</returns>
    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services,
        Action<JsonLocalizationOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddMemoryCache();
        services.AddSingleton<IJsonLocalizationProvider, JsonLocalizationProvider>();
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.AddTransient(typeof(IStringLocalizer<>), typeof(JsonStringLocalizer<>));

        return services;
    }
}



