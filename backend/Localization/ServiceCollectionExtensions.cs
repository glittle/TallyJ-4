using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace TallyJ4.Localization;

public static class ServiceCollectionExtensions
{
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
