using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TallyJ4.EF.Context;
using TallyJ4.EF.Identity;
using TallyJ4.EF.Data;
using Serilog;
using TallyJ4.Backend.Helpers;

Console.WriteLine("Starting up..."); // for server log files

Log.Logger = new LoggerConfiguration().ConfigureStartupConsole().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var builderConfiguration = builder.Configuration;
var services = builder.Services;

// Connect to DB
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

// Add localization
services.AddLocalization(options => options.ResourcesPath = "Resources");
services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "fr" };
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

// Add controllers
services.AddControllers();

// Optional: If you need full JWT customization (the built-in bearer is similar but not pure JWT)
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],  // From appsettings.json
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))  // Secure key (min 256 bits)
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
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

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