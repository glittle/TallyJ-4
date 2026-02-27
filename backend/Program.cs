using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using Backend.Domain.Context;
using Backend.Domain.Identity;
using Backend.EF.Data;
using Backend.Middleware;
using Serilog;
using Backend.Helpers;
using FluentValidation;
using FluentValidation.AspNetCore;
using Backend.Application.Services.Auth;
using Backend.Localization;
using Backend.Services;
using System.Reflection;

Console.WriteLine("Starting up..."); // for server log files

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ConfigureWithColorfulConsole(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var builderConfiguration = builder.Configuration;
var services = builder.Services;

// Connect to DB (skip in Testing environment - tests configure their own database)
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionStringName = "TallyJ4";
    var connectionString = builderConfiguration.GetConnectionString(connectionStringName);

    var regex = new System.Text.RegularExpressions.Regex("(Password|pwd)=[^;]*;");
    Log.Information(
      "Connection string {Name}: {ConnectionString}",
      connectionStringName,
      regex.Replace(connectionString ?? "(Empty)", "Password=******;")
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

// Add CORS
services.AddCors(options =>
{
    var frontendBaseUrl = builderConfiguration["Frontend:BaseUrl"];
    var allowedOrigins = new List<string>();

    if (!string.IsNullOrEmpty(frontendBaseUrl))
    {
        allowedOrigins.Add(frontendBaseUrl);
    }

    // Add development localhost origins as fallback
    allowedOrigins.AddRange(new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:8095" });

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins.Distinct().ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Identity (without the built-in API endpoints that conflict with JWT)
services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<MainDbContext>()
    .AddDefaultTokenProviders();

// Configure external authentication cookie for OAuth flows
services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
});

// Add JWT Bearer authentication
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builderConfiguration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builderConfiguration["Jwt:Issuer"],  // From appsettings.json
        ValidAudience = builderConfiguration["Jwt:Audience"],  // From appsettings.json
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
                // Try to read token from httpOnly cookie
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

// Add Google authentication (optional - gracefully handles missing credentials)
var googleClientId = builderConfiguration["Google:ClientId"];
var googleClientSecret = builderConfiguration["Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret)
    && !googleClientId.StartsWith("<") && !googleClientSecret.StartsWith("<"))
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

// Configure Identity options (password requirements and account lockout)
services.Configure<IdentityOptions>(options =>
{
    // Password requirements (NIST guidelines - longer passwords with complexity)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 1; // Prevent all identical characters

    // Account lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Lockout duration
    options.Lockout.MaxFailedAccessAttempts = 5; // Number of failed attempts before lockout
    options.Lockout.AllowedForNewUsers = true; // Enable lockout for new users
});

// Configure SuperAdmin settings
services.Configure<Backend.Authorization.SuperAdminSettings>(
    builderConfiguration.GetSection(Backend.Authorization.SuperAdminSettings.SectionName));

// Add authorization (for [Authorize] attributes)
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

// Register custom authorization handlers
services.AddScoped<IAuthorizationHandler, Backend.Authorization.ElectionAccessHandler>();
services.AddScoped<IAuthorizationHandler, Backend.Authorization.TellerAccessHandler>();
services.AddScoped<IAuthorizationHandler, Backend.Authorization.HeadTellerAccessHandler>();
services.AddScoped<IAuthorizationHandler, Backend.Authorization.SuperAdminHandler>();

// Add JSON localization
services.Configure<JsonLocalizationOptions>(builderConfiguration.GetSection(JsonLocalizationOptions.SectionName));
services.AddJsonLocalization();

// Add HTTP context accessor
services.AddHttpContextAccessor();

// Add controllers with FluentValidation
services.AddControllers();
services.AddFluentValidationAutoValidation();
services.AddValidatorsFromAssemblyContaining<Program>();

// Add AutoMapper
services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add application services
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
services.AddScoped<Backend.Services.IReportExportService, Backend.Services.ReportExportService>();
services.AddScoped<Backend.Services.IAdvancedReportingService, Backend.Services.AdvancedReportingService>();
services.AddScoped<Backend.Services.IFrontDeskService, Backend.Services.FrontDeskService>();
services.AddScoped<Backend.Services.IOnlineVotingService, Backend.Services.OnlineVotingService>();
services.AddScoped<Backend.Services.IAuditLogService, Backend.Services.AuditLogService>();
services.AddScoped<Backend.Services.ISuperAdminService, Backend.Services.SuperAdminService>();
services.AddScoped<Backend.Services.ImportService>();
services.AddScoped<Backend.Services.IPeopleImportService, Backend.Services.PeopleImportService>();

// Add Auth services
services.AddScoped<JwtTokenService>();
services.AddScoped<EmailService>();
services.AddScoped<LocalAuthService>();
services.AddScoped<PasswordResetService>();
services.AddScoped<TwoFactorService>();
services.AddScoped<EncryptionService>();

services.AddScoped<ISecurityAuditService, SecurityAuditService>();

// Add background services
services.AddHostedService<RefreshTokenCleanupService>();

// Register VoteCountBroadcastService as both singleton and hosted service
// The singleton registration provides dependency injection for IVoteCountBroadcastService
// The hosted service registration starts the background worker
// Note: Both registrations must use the same instance (singleton)
services.AddSingleton<VoteCountBroadcastService>();
services.AddSingleton<IVoteCountBroadcastService>(sp => sp.GetRequiredService<VoteCountBroadcastService>());
services.AddHostedService(sp => sp.GetRequiredService<VoteCountBroadcastService>());

// Add SignalR
services.AddSignalR();
services.AddSingleton<Backend.Services.ISignalRNotificationService, Backend.Services.SignalRNotificationService>();

// Add exception handler
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();

// Configure Swagger/OpenAPI
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

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.IncludeXmlComments(
    Path.Combine(
      AppContext.BaseDirectory,
      $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
    )
  );
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Generate OpenAPI file
    app.WriteOpenApiSpecToFile(Path.Combine("..", "frontend", "openApi", "tallyj.json"));

    // Seed database in development
    var seedOnStartup = builder.Configuration.GetValue<bool>("Database:SeedOnStartup", true);
    if (seedOnStartup)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context, userManager, roleManager, logger);
    }
}

// Middleware pipeline (order matters)
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TallyJ4 API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Serve static files (favicon.ico, etc.)
app.UseStaticFiles();

// Use CORS
app.UseCors("AllowFrontend");

// Use custom rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Configure request localization
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

app.UseAuthentication();  // Enables Identity
app.UseMiddleware<Backend.Middleware.ElectionContextMiddleware>();
app.UseAuthorization();
app.UseMiddleware<Backend.Middleware.AuditMiddleware>();

// Custom AuthController handles authentication endpoints

// Map API controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<Backend.Hubs.MainHub>("/hubs/main");
app.MapHub<Backend.Hubs.AnalyzeHub>("/hubs/analyze");
app.MapHub<Backend.Hubs.BallotImportHub>("/hubs/ballot-import");
app.MapHub<Backend.Hubs.PeopleImportHub>("/hubs/people-import");
app.MapHub<Backend.Hubs.FrontDeskHub>("/hubs/front-desk");
app.MapHub<Backend.Hubs.PublicHub>("/hubs/public");
app.MapHub<Backend.Hubs.OnlineVotingHub>("/hubs/online-voting");

// Test endpoint
app.MapGet("/protected", () => "This is protected!").RequireAuthorization();

// Start listening
await app.RunAsync();





