using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;

namespace TallyJ4.Backend.Helpers;

/// <summary>
/// Extensions for Serilog configuration
/// </summary>
public static class SerilogExtensions
{
  /// <summary>
  /// Configures Serilog with colorful console output and configuration-based settings
  /// </summary>
  /// <param name="configuration">The logger configuration</param>
  /// <param name="appConfiguration">The application configuration</param>
  /// <param name="sectionName">The configuration section name (optional)</param>
  /// <param name="serviceProvider">Service provider for dependency injection (optional)</param>
  /// <returns>The configured logger configuration</returns>
  public static LoggerConfiguration ConfigureWithColorfulConsole(
    this LoggerConfiguration configuration,
    IConfiguration appConfiguration,
    string? sectionName = null,
    IServiceProvider? serviceProvider = null
  )
  {
    var options = string.IsNullOrEmpty(sectionName)
      ? new ConfigurationReaderOptions()
      : new ConfigurationReaderOptions { SectionName = sectionName };

    // First apply configuration-based settings (excluding console sinks)
    configuration = configuration.ReadFrom.Configuration(appConfiguration, options);

    // Add correlation ID enricher if service provider is available
    if (serviceProvider != null)
    {
      var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
      if (httpContextAccessor != null)
      {
        configuration = configuration.Enrich.With(new CorrelationIdEnricher(httpContextAccessor));
      }
    }

    // Force colors for dotnet watch and development scenarios
    var isDevelopment =
      Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    var isDotnetWatch = Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1";

    // Use our custom theme for development, especially for dotnet watch
    var theme =
      (isDevelopment || isDotnetWatch) ? CustomConsoleTheme.RichColors : AnsiConsoleTheme.Code;

    // Then add our colorful console sink (this will override any console configuration)
    return configuration.WriteTo.Console(
      theme: theme,
      outputTemplate: OutputTemplates.WithCorrelationId,
      applyThemeToRedirectedOutput: isDevelopment || isDotnetWatch // Force colors for dev/watch
    );
  }

  /// <summary>
  /// Configures a simple colorful console logger for early startup
  /// </summary>
  /// <param name="configuration">The logger configuration</param>
  /// <returns>The configured logger configuration</returns>
  public static LoggerConfiguration ConfigureStartupConsole(this LoggerConfiguration configuration)
  {
    var isDevelopment =
      Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    var isDotnetWatch = Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1";

    return configuration.WriteTo.Console(
      theme: CustomConsoleTheme.RichColors,
      outputTemplate: OutputTemplates.Clean,
      applyThemeToRedirectedOutput: isDevelopment || isDotnetWatch // Force colors for dev/watch
    );
  }

  /// <summary>
  /// Gets different output template options
  /// </summary>
  public static class OutputTemplates
  {
    /// <summary>
    /// Clean template without properties - just timestamp, level, and message
    /// </summary>
    public const string Clean =
      "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId:l}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Template with all properties shown as JSON
    /// </summary>
    public const string WithProperties =
      "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId:l}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    /// <summary>
    /// Template with only SourceContext shown (class name)
    /// </summary>
    public const string WithSourceContext =
      "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId:l}] {Message:lj} {SourceContext}{NewLine}{Exception}";

    /// <summary>
    /// Template with short SourceContext (just class name, not full namespace)
    /// </summary>
    public const string WithShortSourceContext =
      "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId:l}] {Message:lj} {SourceContext:l}{NewLine}{Exception}";

    /// <summary>
    /// Template with correlation ID for request tracking
    /// </summary>
    public const string WithCorrelationId =
      "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId:l}] {Message:lj} {SourceContext:l}{NewLine}{Exception}";
  }
}