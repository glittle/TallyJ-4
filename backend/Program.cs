using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TallyJ4.EF.Context;
using TallyJ4.Domain.Identity;
using TallyJ4.EF.Data;
using TallyJ4.Middleware;
using Serilog;
using TallyJ4.Backend.Helpers;
using FluentValidation;
using FluentValidation.AspNetCore;

Console.WriteLine("Starting up..."); // for server log files

Log.Logger = new LoggerConfiguration().ConfigureStartupConsole().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

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

// Add Identity API endpoints (this sets up core Identity + bearer auth)
services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<MainDbContext>();

// Optional: Customize Identity options (e.g., password requirements)
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    // Add more as needed...
});

// Add authorization (for [Authorize] attributes)
services.AddAuthorization();

// Add controllers with FluentValidation
services.AddControllers();
services.AddFluentValidationAutoValidation();
services.AddValidatorsFromAssemblyContaining<Program>();

// Add AutoMapper
services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add application services
services.AddScoped<TallyJ4.Services.IElectionService, TallyJ4.Services.ElectionService>();
services.AddScoped<TallyJ4.Services.IPeopleService, TallyJ4.Services.PeopleService>();
services.AddScoped<TallyJ4.Services.IBallotService, TallyJ4.Services.BallotService>();
services.AddScoped<TallyJ4.Services.IVoteService, TallyJ4.Services.VoteService>();
services.AddScoped<TallyJ4.Services.IDashboardService, TallyJ4.Services.DashboardService>();
services.AddScoped<TallyJ4.Services.ISetupService, TallyJ4.Services.SetupService>();
services.AddScoped<TallyJ4.Services.IAccountService, TallyJ4.Services.AccountService>();
services.AddScoped<TallyJ4.Services.IPublicService, TallyJ4.Services.PublicService>();
services.AddScoped<TallyJ4.Services.ITallyService, TallyJ4.Services.TallyService>();

// Add exception handler
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();

// Configure Swagger/OpenAPI
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
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
});

// Optional: If you need full JWT customization (the built-in bearer is similar but not pure JWT)
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],  // From appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))  // Secure key (min 256 bits)
    };
});

var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    var seedOnStartup = builder.Configuration.GetValue<bool>("Database:SeedOnStartup", true);
    if (seedOnStartup)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context, userManager, logger);
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
app.UseAuthorization();

// Map the Identity API endpoints (e.g., under /auth prefix)
app.MapGroup("/auth").MapIdentityApi<AppUser>();  // Adds /auth/register, /auth/login, etc.

// Map API controllers
app.MapControllers();

// Test endpoint
app.MapGet("/protected", () => "This is protected!").RequireAuthorization();

// Start listening
await app.RunAsync();

public partial class Program { }