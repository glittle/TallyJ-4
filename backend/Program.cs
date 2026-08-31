using Backend.Services.Auth;
using Backend.Configuration;
using Backend.Context;
using Backend.Identity;
using Backend.EF.Data;
using Backend.Helpers;
using Backend.Localization;
using Backend.Middleware;
using Backend.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .ConfigureStartupConsole()
    .CreateLogger();

var machineName = Environment.MachineName;
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
var isTesting = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "testhost");
var siteType = Environment.CommandLine.DetermineSiteType();

var nonTestMode = isDevelopment ? "DEVELOPMENT" : "PRODUCTION";
var siteMode = isTesting ? "TESTING" : nonTestMode;

Log.Information("Starting up in {SiteType} mode ({SiteMode}) on machine {MachineName}", siteType, siteMode, machineName);

void AddLogging(WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .ConfigureWithColorfulConsole(builder.Configuration)
        .CreateLogger();

    builder.Host.UseSerilog();
}

void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var builderConfiguration = builder.Configuration;

    builder.Configuration.AddJsonFile($"appsettings.{machineName}.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddJsonFile($"appsettings.{siteType}.json", optional: true, reloadOnChange: true);

    // Add version.json from repository root
    var versionJsonPath = isDevelopment || isTesting
        ? Path.Combine(builder.Environment.ContentRootPath, "..", "version.json")
        : Path.Combine(builder.Environment.ContentRootPath, "version.json");
    builder.Configuration.AddJsonFile(versionJsonPath, optional: false, reloadOnChange: true);

    // look in a folder given by an environment variable, useful for docker and some hosting environments
    var envConfigPath = Environment.GetEnvironmentVariable("TALLYJ_CONFIG_PATH");
    if (!string.IsNullOrEmpty(envConfigPath))
    {
        builder.Configuration.AddJsonFile(envConfigPath, optional: true, reloadOnChange: true);
    }

    // Look in a fixed shared location... easier for some environments to keep it outside of the repo folders
    builder.Configuration.AddJsonFile(Path.Combine("c:", "AppSettings", "TallyJ4.json"), optional: true, reloadOnChange: true);
    builder.Configuration.AddJsonFile(Path.Combine("c:", "AppSettings", $"TallyJ4.{siteType}.json"), optional: true, reloadOnChange: true);

    // report on which files were actually used
    foreach (var fileInfo in from provider in ((IConfigurationRoot)builder.Configuration).Providers.OfType<JsonConfigurationProvider>()
                             let fileInfo = provider.Source.FileProvider?.GetFileInfo(provider.Source.Path ?? "")
                             where fileInfo?.Exists == true
                             select fileInfo)
    {
        Log.Information("Applied config from {Path}", fileInfo.PhysicalPath);
    }

    Log.Information("Version: {Version}", builderConfiguration["version"]);

    if (!isTesting)
    {
        var connectionStringName = "TallyJ4";
        var connectionString = builderConfiguration.GetConnectionString(connectionStringName);

        var regexToRemovePw = new System.Text.RegularExpressions.Regex("(Password|pwd)=[^;]*;");
        Log.Information(
          "Connection string {Name}: {ConnectionString}",
          connectionStringName,
          regexToRemovePw.Replace(connectionString ?? "(Empty)", "---;")
        );
        if (connectionString == null)
        {
            Log.Fatal(
              "Connection string {Name} is not set. Check your appsettings.json configuration.",
              connectionStringName
            );
            Environment.Exit(1);
        }

        services.AddDbContext<MainDbContext>(connectionStringName, connectionString);
        services.AddDbContext<DataProtectionDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionDbContext>();
    }

    services.AddCors(options =>
    {
        // Support the documented structure in appsettings.json ("Cors": { "AllowedOrigins": [...] })
        // as well as top-level for backward compat / machine-specific overrides.
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        // ClientEnv:frontendUrl is the public SPA origin (required outside Development/Testing).
        var frontendOrigin = FrontendUrlResolver.GetOrigin(builderConfiguration, builder.Environment);
        allowedOrigins = allowedOrigins.Append(frontendOrigin).ToArray();

        allowedOrigins = allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        Log.Information("CORS allowed origins: {AllowedOrigins}", allowedOrigins);

        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    services.AddIdentity<AppUser, IdentityRole>()
        .AddEntityFrameworkStores<MainDbContext>()
        .AddDefaultTokenProviders();

    services.ConfigureExternalCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    });

    ProgramAuthSetup.ConfigureAuthentication(services, builderConfiguration);
    ProgramAuthSetup.ConfigureIdentityOptions(services);

    services.Configure<Backend.Authorization.SuperAdminSettings>(
        builderConfiguration.GetSection(Backend.Authorization.SuperAdminSettings.SectionName));

    ProgramAuthSetup.ConfigureAuthorization(services);

    services.Configure<JsonLocalizationOptions>(builderConfiguration.GetSection(JsonLocalizationOptions.SectionName));
    services.AddJsonLocalization();

    services.AddHttpClient("GreenApi");
    services.AddHttpClient("Facebook", c =>
    {
        c.BaseAddress = new Uri(builderConfiguration["Facebook:BaseUrl"]!);
    });
    services.AddHttpClient("Kakao", c =>
    {
        c.BaseAddress = new Uri(builderConfiguration["KakaoApi:BaseUrl"]!);
    });

    services.AddHttpContextAccessor();

    services.AddControllers();
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<Program>();

    ProgramServiceRegistration.RegisterApplicationServices(services);
    ProgramServiceRegistration.RegisterAuthServices(services);
    ProgramServiceRegistration.RegisterBackgroundServices(services, isTesting);

    services.AddSignalR();
    services.AddSingleton<ISignalRNotificationService, SignalRNotificationService>();

    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails();

    if (isDevelopment)
    {
        ProgramAuthSetup.ConfigureSwagger(services);
    }
}

var builder = WebApplication.CreateBuilder(args);

AddLogging(builder);
ConfigureServices(builder);

if (!isDevelopment && !isTesting)
{
    var dsn = builder.Configuration["Sentry"];
    if (dsn.HasContent())
    {
        builder.WebHost.UseSentry(o =>
        {
            o.Dsn = dsn;
            // When configuring for the first time, to see what the SDK is doing:
            // o.Debug = true;
        });
    }
}

var app = builder.Build();

// Reconfigure logger with correlation ID enricher now that services are available
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With(new CorrelationIdEnricher(app.Services.GetRequiredService<IHttpContextAccessor>()));

// Create new logger with the same sinks as the original but with correlation ID enricher
var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
var isWatch = Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1";
var theme = (isDev || isWatch) ? CustomConsoleTheme.RichColors : AnsiConsoleTheme.Code;

Log.Logger = loggerConfiguration
    .WriteTo.Console(
        theme: theme,
        outputTemplate: SerilogExtensions.OutputTemplates.WithCorrelationId,
        applyThemeToRedirectedOutput: isDev || isWatch
    )
    .CreateLogger();

await ProgramAppPipeline.ConfigureApp(app, builder.Configuration, isDevelopment, isTesting, siteType);

// `dotnet run -- --exit-after-start` writes the OpenAPI spec (Development) then stops.
if (args.Contains("--exit-after-start"))
{
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("Startup completed. Stopping.");
        lifetime.StopApplication();
    });
}

await app.RunAsync();
