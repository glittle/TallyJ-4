using System.Globalization;
using System.IO;
using System.Text;
using Backend.Application.Services.Auth;
using Backend.Domain.Context;
using Backend.Domain.Identity;
using Backend.EF.Data;
using Backend.Helpers;
using Backend.Localization;
using Backend.Middleware;
using Backend.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Settings.Configuration;
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
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var frontendBaseUrl = builderConfiguration["Frontend:BaseUrl"];

        if (!string.IsNullOrEmpty(frontendBaseUrl))
        {
            allowedOrigins = allowedOrigins.Append(frontendBaseUrl).ToArray();
        }

        allowedOrigins = allowedOrigins.Distinct().ToArray();

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

    ConfigureAuthentication(services, builderConfiguration);
    ConfigureIdentityOptions(services);

    services.Configure<Backend.Authorization.SuperAdminSettings>(
        builderConfiguration.GetSection(Backend.Authorization.SuperAdminSettings.SectionName));

    ConfigureAuthorization(services);

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

    if (TypeAdapterConfig.GlobalSettings.RuleMap.Count == 0)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);
    }
    services.AddMapster();

    RegisterApplicationServices(services);
    RegisterAuthServices(services);
    RegisterBackgroundServices(services);

    services.AddSignalR();
    services.AddSingleton<ISignalRNotificationService, SignalRNotificationService>();

    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails();

    if (isDevelopment)
    {
        ConfigureSwagger(services);
    }
}

void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                else
                {
                    var tokenCookie = context.Request.Cookies["auth_token"];
                    if (!string.IsNullOrEmpty(tokenCookie))
                    {
                        context.Token = tokenCookie;
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

    var googleClientId = configuration["Google:ClientId"];
    var googleClientSecret = configuration["Google:ClientSecret"];

    if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret)
        && !googleClientId.StartsWith('<') && !googleClientSecret.StartsWith('<'))
    {
        services.AddAuthentication()
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = true;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/login?error=" + context.Failure?.Message);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        Log.Information("Google authentication configured successfully");
    }
    else
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        if (isDevelopment)
        {
            Log.Warning("Google authentication not configured - ClientId or ClientSecret is missing or using placeholder values. Google login will not be available.");
        }
        else
        {
            Log.Information("Google authentication not configured. Google login will not be available.");
        }
    }
}

void ConfigureIdentityOptions(IServiceCollection services)
{
    services.Configure<IdentityOptions>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    });
}

void ConfigureAuthorization(IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        options.AddPolicy("ElectionAccess", policy =>
            policy.Requirements.Add(new Backend.Authorization.ElectionAccessRequirement()));

        options.AddPolicy("TellerAccess", policy =>
            policy.Requirements.Add(new Backend.Authorization.TellerAccessRequirement()));

        options.AddPolicy("HeadTellerAccess", policy =>
            policy.Requirements.Add(new Backend.Authorization.HeadTellerAccessRequirement()));

        options.AddPolicy("SuperAdmin", policy =>
            policy.Requirements.Add(new Backend.Authorization.SuperAdminRequirement()));
    });

    services.AddScoped<IAuthorizationHandler, Backend.Authorization.ElectionAccessHandler>();
    services.AddScoped<IAuthorizationHandler, Backend.Authorization.TellerAccessHandler>();
    services.AddScoped<IAuthorizationHandler, Backend.Authorization.HeadTellerAccessHandler>();
    services.AddScoped<IAuthorizationHandler, Backend.Authorization.SuperAdminHandler>();
}

void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<IElectionService, ElectionService>();
    services.AddScoped<ILocationService, LocationService>();
    services.AddScoped<IComputerService, ComputerService>();
    services.AddScoped<ITellerService, TellerService>();
    services.AddScoped<IPeopleService, PeopleService>();
    services.AddScoped<IBallotService, BallotService>();
    services.AddScoped<IVoteService, VoteService>();
    services.AddScoped<IDashboardService, DashboardService>();
    services.AddScoped<ISetupService, SetupService>();
    services.AddScoped<IAccountService, AccountService>();
    services.AddScoped<IPublicService, PublicService>();
    services.AddScoped<ITallyService, TallyService>();
    services.AddScoped<IReportService, ReportService>();
    services.AddScoped<IFrontDeskService, FrontDeskService>();
    services.AddScoped<IOnlineVotingService, OnlineVotingService>();
    services.AddScoped<IAuditLogService, AuditLogService>();
    services.AddScoped<ISuperAdminService, SuperAdminService>();
    services.AddScoped<ImportService>();
    services.AddScoped<IPeopleImportService, PeopleImportService>();
    services.AddScoped<CdnBallotImportService>();
    services.AddScoped<TallyJv3ElectionImportService>();
    services.AddScoped<JsonElectionImportExportService>();
    services.AddScoped<ElectionExportImportService>();
    services.AddSingleton<IRemoteLogService, RemoteLogService>();
}

void RegisterAuthServices(IServiceCollection services)
{
    services.AddScoped<IEmailSender, SmtpEmailSender>();
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<EmailService>();
    services.AddScoped<ILocalAuthService, LocalAuthService>();
    services.AddScoped<IPasswordResetService, PasswordResetService>();
    services.AddScoped<ITwoFactorService, TwoFactorService>();
    services.AddScoped<EncryptionService>();

    services.AddScoped<ISecurityAuditService, SecurityAuditService>();
}

void RegisterBackgroundServices(IServiceCollection services)
{
    services.AddSingleton<RateLimitStore>();

    if (isTesting) // don't register the real broadcast service during testing, to avoid interference with tests and allow testing of the broadcast mechanism itself
    {
        services.AddSingleton<IVoteCountBroadcastService, NullVoteCountBroadcastService>();
        return;
    }

    services.AddHostedService<RefreshTokenCleanupService>();
    services.AddSingleton<VoteCountBroadcastService>();
    services.AddSingleton<IVoteCountBroadcastService>(sp => sp.GetRequiredService<VoteCountBroadcastService>());
    services.AddHostedService(sp => sp.GetRequiredService<VoteCountBroadcastService>());
}

void ConfigureSwagger(IServiceCollection services)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.UseAllOfForInheritance();
        options.CustomSchemaIds(type =>
        {
            static string GetSchemaId(Type t)
            {
                if (!t.IsGenericType)
                    return t.Name;

                var typeName = t.Name.Substring(0, t.Name.IndexOf('`'));
                var genericArgs = string.Join("", t.GetGenericArguments().Select(GetSchemaId));
                return $"{typeName}{genericArgs}";
            }

            return GetSchemaId(type);
        });

        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TallyJ4 API",
            Version = "v1",
            Description = "Election management and vote tallying system API"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
        });

        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            return apiDesc.RelativePath?.StartsWith("api/") == true;
        });
    });
}

async Task ConfigureApp(WebApplication app, IConfiguration configuration)
{
    var migrateOnStartup = configuration.GetValue("Database:MigrateOnStartup", false);
    if (migrateOnStartup)
    {
        Log.Information("Migrating the database on startup as configured");
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        await context.Database.MigrateAsync();
    }

    var seedOnStartup = configuration.GetValue("Database:SeedOnStartup", false);
    if (seedOnStartup)
    {
        Log.Information("Seeding the database on startup as configured");
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await DbSeeder.SeedAsync(context, userManager, roleManager, logger);
    }

    // Send startup notification to remote log
    {
        using var scope = app.Services.CreateScope();
        var remoteLogService = scope.ServiceProvider.GetRequiredService<IRemoteLogService>();
        await remoteLogService.SendLogAsync($"Started up - SiteType: {siteType} - Url: {configuration["Frontend:BaseUrl"]}");
    }

    app.UseExceptionHandler();

    if (isDevelopment)
    {
        app.WriteOpenApiSpecToFile(Path.Combine("..", "frontend", "openApi", "tallyj.json"));
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TallyJ4 API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseMiddleware<ConfigMiddleware>();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Content-hashed assets (in /assets/) are immutable — cache for 1 year
            if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            }
        }
    });
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseCors("AllowFrontend");
    app.Use(async (context, next) =>
    {
        // Only add HSTS if in Production
        if (!isDevelopment && !isTesting)
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }
        await next();
    });
    app.UseMiddleware<RateLimitingMiddleware>();

    var localizationOptions = app.Services.GetRequiredService<IOptions<JsonLocalizationOptions>>().Value;
    var supportedCultures = localizationOptions.SupportedCultures
        .Select(c => new CultureInfo(c))
        .ToArray();

    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(localizationOptions.DefaultCulture),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    app.UseAuthentication();
    app.UseMiddleware<ElectionContextMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<AuditMiddleware>();

    app.MapControllers();

    // Add SPA fallback for web history routing - exclude systemhealth and hub routes
    app.MapFallbackToFile("{*path:regex(^(?!api/|systemhealth|hubs/|assets/|config\\.json).*$)}", "index.html").AllowAnonymous();

    app.MapHub<Backend.Hubs.MainHub>("/hubs/main");
    app.MapHub<Backend.Hubs.AnalyzeHub>("/hubs/analyze");
    app.MapHub<Backend.Hubs.BallotImportHub>("/hubs/ballot-import");
    app.MapHub<Backend.Hubs.PeopleImportHub>("/hubs/people-import");
    app.MapHub<Backend.Hubs.FrontDeskHub>("/hubs/front-desk");
    app.MapHub<Backend.Hubs.PublicHub>("/hubs/public");
    app.MapHub<Backend.Hubs.OnlineVotingHub>("/hubs/online-voting");

    app.MapGet("/protected", () => "This is protected!").RequireAuthorization();

    // add a shutdown hook to help with server logs
    app.Lifetime.ApplicationStopping.Register(async () =>
    {
        Log.Information("Application Stopping...");
        await Log.CloseAndFlushAsync();
    });
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

await ConfigureApp(app, builder.Configuration);

await app.RunAsync();
