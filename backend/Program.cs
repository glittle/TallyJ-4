using System.Globalization;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Linq;

var machineName = Environment.MachineName;
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
var isTesting = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "testhost");

if (isTesting)
{
    Console.WriteLine("Starting up in TESTING mode");
}
if (isDevelopment)
{
    Console.WriteLine("Starting up in DEVELOPMENT mode");
}
if (!isDevelopment && !isTesting)
{
    Console.WriteLine("Starting up in NORMAL mode");
}

void ConfigureBuilder(WebApplicationBuilder builder)
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

    // look in a folder given by an environment variable, useful for docker and some hosting environments
    var envConfigPath = Environment.GetEnvironmentVariable("TALLYJ_CONFIG_PATH");
    if (!string.IsNullOrEmpty(envConfigPath))
    {
        builder.Configuration.AddJsonFile(envConfigPath, optional: true, reloadOnChange: true);
    }

    // Look in a fixed shared location... easier for some environments to keep it outside of the repo folders
    builder.Configuration.AddJsonFile($"c:\\AppSettings\\TallyJ4.json", optional: true, reloadOnChange: true);

    foreach (var fileInfo in from provider in ((IConfigurationRoot)builder.Configuration).Providers.OfType<JsonConfigurationProvider>()
                             let fileInfo = provider.Source.FileProvider?.GetFileInfo(provider.Source.Path ?? "")
                             where fileInfo?.Exists == true
                             select fileInfo)
    {
        Log.Information("Loaded config {Path}", fileInfo.PhysicalPath);
    }

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
    }

    services.AddCors(options =>
    {
        var frontendBaseUrl = builderConfiguration["Frontend:BaseUrl"];
        var allowedOrigins = new List<string>();

        if (!string.IsNullOrEmpty(frontendBaseUrl))
        {
            allowedOrigins.Add(frontendBaseUrl);
        }

        allowedOrigins.AddRange(new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:8095" });

        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins.Distinct().ToArray())
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

    TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);
    services.AddMapster();

    RegisterApplicationServices(services);
    RegisterAuthServices(services);
    RegisterBackgroundServices(services);

    services.AddSignalR();
    services.AddSingleton<Backend.Services.ISignalRNotificationService, Backend.Services.SignalRNotificationService>();

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
        Log.Warning("Google authentication not configured - ClientId or ClientSecret is missing or using placeholder values. Google login will not be available.");
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
    services.AddScoped<Backend.Services.IElectionService, Backend.Services.ElectionService>();
    services.AddScoped<Backend.Services.ILocationService, Backend.Services.LocationService>();
    services.AddScoped<Backend.Services.IComputerService, Backend.Services.ComputerService>();
    services.AddScoped<Backend.Services.ITellerService, Backend.Services.TellerService>();
    services.AddScoped<Backend.Services.IPeopleService, Backend.Services.PeopleService>();
    services.AddScoped<Backend.Services.IBallotService, Backend.Services.BallotService>();
    services.AddScoped<Backend.Services.IVoteService, Backend.Services.VoteService>();
    services.AddScoped<Backend.Services.IDashboardService, Backend.Services.DashboardService>();
    services.AddScoped<Backend.Services.ISetupService, Backend.Services.SetupService>();
    services.AddScoped<Backend.Services.IAccountService, Backend.Services.AccountService>();
    services.AddScoped<Backend.Services.IPublicService, Backend.Services.PublicService>();
    services.AddScoped<Backend.Services.ITallyService, Backend.Services.TallyService>();
    services.AddScoped<Backend.Services.IReportService, Backend.Services.ReportService>();
    services.AddScoped<Backend.Services.IFrontDeskService, Backend.Services.FrontDeskService>();
    services.AddScoped<Backend.Services.IOnlineVotingService, Backend.Services.OnlineVotingService>();
    services.AddScoped<Backend.Services.IAuditLogService, Backend.Services.AuditLogService>();
    services.AddScoped<Backend.Services.ISuperAdminService, Backend.Services.SuperAdminService>();
    services.AddScoped<Backend.Services.ImportService>();
    services.AddScoped<Backend.Services.IPeopleImportService, Backend.Services.PeopleImportService>();
    services.AddScoped<Backend.Services.ElectionExportImportService>();
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
    services.AddSingleton<Backend.Middleware.RateLimitStore>();

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
    if (isDevelopment)
    {
        app.WriteOpenApiSpecToFile(Path.Combine("..", "frontend", "openApi", "tallyj.json"));

        var seedOnStartup = configuration.GetValue("Database:SeedOnStartup", false);
        if (seedOnStartup)
        {
            Log.Information("Seeding database on startup as configured");
            using var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(context, userManager, roleManager, logger);
        }
    }
    else
    {
        Log.Information("Not migrating or seeding database on startup in non-development environment");
    }

    app.UseExceptionHandler();

    if (isDevelopment)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TallyJ4 API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors("AllowFrontend");
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
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
    app.UseMiddleware<Backend.Middleware.ElectionContextMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<Backend.Middleware.AuditMiddleware>();

    app.MapControllers();

    app.MapHub<Backend.Hubs.MainHub>("/hubs/main");
    app.MapHub<Backend.Hubs.AnalyzeHub>("/hubs/analyze");
    app.MapHub<Backend.Hubs.BallotImportHub>("/hubs/ballot-import");
    app.MapHub<Backend.Hubs.PeopleImportHub>("/hubs/people-import");
    app.MapHub<Backend.Hubs.FrontDeskHub>("/hubs/front-desk");
    app.MapHub<Backend.Hubs.PublicHub>("/hubs/public");
    app.MapHub<Backend.Hubs.OnlineVotingHub>("/hubs/online-voting");

    app.MapGet("/protected", () => "This is protected!").RequireAuthorization();
}

var builder = WebApplication.CreateBuilder(args);
ConfigureBuilder(builder);
ConfigureServices(builder);

var app = builder.Build();
await ConfigureApp(app, builder.Configuration);

await app.RunAsync();
