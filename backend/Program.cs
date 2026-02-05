using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Identity;
using TallyJ4.EF.Data;
using TallyJ4.Middleware;
using Serilog;
using TallyJ4.Backend.Helpers;
using FluentValidation;
using FluentValidation.AspNetCore;
using TallyJ4.Application.Services.Auth;
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
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:8095")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Identity (without the built-in API endpoints that conflict with JWT)
services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<MainDbContext>()
    .AddDefaultTokenProviders();

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
        ValidAudience = builderConfiguration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))  // Secure key (min 256 bits)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Exception}", context.Exception.Message);
            Log.Warning("JWT Failure details: {FailureMessage}", context.Exception.ToString());
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? context.Principal?.FindFirst("sub")?.Value;
            Log.Information("JWT Token validated successfully for user: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
                Log.Information("JWT Token received from query string for SignalR hub: {Path}, TokenLength: {TokenLength}",
                    path, accessToken.ToString().Length);
            }
            else
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                var hasBearer = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
                var tokenLength = hasBearer ? authHeader.Substring(7).Length : 0;
                Log.Information("JWT Message received - Header: {HasHeader}, HasBearer: {HasBearer}, TokenLength: {TokenLength}, ActualHeader: '{AuthHeader}'",
                    !string.IsNullOrEmpty(authHeader), hasBearer, tokenLength, authHeader.Length > 100 ? authHeader.Substring(0, 100) + "..." : authHeader);
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT Challenge initiated - Error: {Error}, ErrorDescription: {ErrorDescription}, AuthFailure: {AuthFailure}",
                context.Error, context.ErrorDescription, context.AuthenticateFailure?.Message);
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
            options.CallbackPath = "/api/auth/google/callback";
            options.SaveTokens = true;
        });
    Log.Information("Google authentication configured successfully");
}
else
{
    Log.Warning("Google authentication not configured - ClientId or ClientSecret is missing or using placeholder values. Google login will not be available.");
}

// Optional: Customize Identity options (e.g., password requirements)
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    // Add more as needed...
});

// Add authorization (for [Authorize] attributes)
services.AddAuthorization(options =>
{
    options.AddPolicy("ElectionAccess", policy =>
        policy.Requirements.Add(new TallyJ4.Authorization.ElectionAccessRequirement()));

    options.AddPolicy("TellerAccess", policy =>
        policy.Requirements.Add(new TallyJ4.Authorization.TellerAccessRequirement()));

    options.AddPolicy("HeadTellerAccess", policy =>
        policy.Requirements.Add(new TallyJ4.Authorization.HeadTellerAccessRequirement()));
});

// Register custom authorization handlers
services.AddScoped<IAuthorizationHandler, TallyJ4.Authorization.ElectionAccessHandler>();
services.AddScoped<IAuthorizationHandler, TallyJ4.Authorization.TellerAccessHandler>();
services.AddScoped<IAuthorizationHandler, TallyJ4.Authorization.HeadTellerAccessHandler>();

// Add localization
services.AddLocalization();

// Add HTTP context accessor
services.AddHttpContextAccessor();

// Add controllers with FluentValidation
services.AddControllers();
services.AddFluentValidationAutoValidation();
services.AddValidatorsFromAssemblyContaining<Program>();

// Add AutoMapper
services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add application services
services.AddScoped<TallyJ4.Services.IElectionService, TallyJ4.Services.ElectionService>();
services.AddScoped<TallyJ4.Services.ILocationService, TallyJ4.Services.LocationService>();
services.AddScoped<TallyJ4.Services.IComputerService, TallyJ4.Services.ComputerService>();
services.AddScoped<TallyJ4.Services.ITellerService, TallyJ4.Services.TellerService>();
services.AddScoped<TallyJ4.Services.IPeopleService, TallyJ4.Services.PeopleService>();
services.AddScoped<TallyJ4.Services.IBallotService, TallyJ4.Services.BallotService>();
services.AddScoped<TallyJ4.Services.IVoteService, TallyJ4.Services.VoteService>();
services.AddScoped<TallyJ4.Services.IDashboardService, TallyJ4.Services.DashboardService>();
services.AddScoped<TallyJ4.Services.ISetupService, TallyJ4.Services.SetupService>();
services.AddScoped<TallyJ4.Services.IAccountService, TallyJ4.Services.AccountService>();
services.AddScoped<TallyJ4.Services.IPublicService, TallyJ4.Services.PublicService>();
services.AddScoped<TallyJ4.Services.ITallyService, TallyJ4.Services.TallyService>();
services.AddScoped<TallyJ4.Services.IReportExportService, TallyJ4.Services.ReportExportService>();
services.AddScoped<TallyJ4.Services.IAdvancedReportingService, TallyJ4.Services.AdvancedReportingService>();
services.AddScoped<TallyJ4.Services.IFrontDeskService, TallyJ4.Services.FrontDeskService>();
services.AddScoped<TallyJ4.Services.IOnlineVotingService, TallyJ4.Services.OnlineVotingService>();
services.AddScoped<TallyJ4.Services.IAuditLogService, TallyJ4.Services.AuditLogService>();
services.AddScoped<TallyJ4.Backend.Services.ImportService>();

// Add Auth services
services.AddScoped<JwtTokenService>();
services.AddScoped<EmailService>();
services.AddScoped<LocalAuthService>();
services.AddScoped<PasswordResetService>();
services.AddScoped<TwoFactorService>();

// Add SignalR
services.AddSignalR();
services.AddSingleton<TallyJ4.Services.ISignalRNotificationService, TallyJ4.Services.SignalRNotificationService>();

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
    app.WriteOpenApiSpecToFile("..\\frontend\\openapi\\tallyj.json");

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

// Use CORS
app.UseCors("AllowFrontend");

// Use localization middleware
app.UseRequestLocalization();

app.UseAuthentication();  // Enables Identity
app.UseMiddleware<TallyJ4.Middleware.ElectionContextMiddleware>();
app.UseAuthorization();
app.UseMiddleware<TallyJ4.Middleware.AuditMiddleware>();

// Custom AuthController handles authentication endpoints

// Map API controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<TallyJ4.Hubs.MainHub>("/hubs/main");
app.MapHub<TallyJ4.Hubs.AnalyzeHub>("/hubs/analyze");
app.MapHub<TallyJ4.Hubs.BallotImportHub>("/hubs/ballot-import");
app.MapHub<TallyJ4.Hubs.FrontDeskHub>("/hubs/front-desk");
app.MapHub<TallyJ4.Hubs.PublicHub>("/hubs/public");
app.MapHub<TallyJ4.Hubs.OnlineVotingHub>("/hubs/online-voting");

// Test endpoint
app.MapGet("/protected", () => "This is protected!").RequireAuthorization();

// Start listening
await app.RunAsync();

