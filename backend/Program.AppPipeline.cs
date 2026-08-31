using System.Globalization;
using Backend.Configuration;
using Backend.Context;
using Backend.EF.Data;
using Backend.Helpers;
using Backend.Identity;
using Backend.Localization;
using Backend.Middleware;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

/// <summary>
/// Middleware pipeline, startup migrate/seed, and related app configuration.
/// Extracted from Program.cs top-level local functions — no behavior change.
/// </summary>
public static class ProgramAppPipeline
{
    public static async Task ConfigureApp(
        WebApplication app,
        IConfiguration configuration,
        bool isDevelopment,
        bool isTesting,
        string siteType,
        bool skipDatabase = false)
    {
        var migrateOnStartup = !skipDatabase && configuration.GetValue("Database:MigrateOnStartup", false);
        if (skipDatabase)
        {
            Log.Information("Skipping database migrate/seed");
        }
        if (migrateOnStartup)
        {
            Log.Information("Migrating the database on startup as configured");
            using var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

            await EnsureConsistentMigrationHistoryAsync(context, Log.Logger);
            await context.Database.MigrateAsync();
        }

        var seedOnStartup = !skipDatabase && configuration.GetValue("Database:SeedOnStartup", false);
        if (seedOnStartup)
        {
            if (!migrateOnStartup)
            {
                Log.Information("Migrating the database before seeding as required");
                using var migrationScope = app.Services.CreateScope();
                var migrationContext = migrationScope.ServiceProvider.GetRequiredService<MainDbContext>();

                await EnsureConsistentMigrationHistoryAsync(migrationContext, Log.Logger);
                await migrationContext.Database.MigrateAsync();
            }

            Log.Information("Seeding the database on startup as configured");
            using var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            await DbSeeder.SeedAsync(context, userManager, roleManager, logger);
        }

        if (!skipDatabase)
        {
            using var scope = app.Services.CreateScope();
            var remoteLogService = scope.ServiceProvider.GetRequiredService<IRemoteLogService>();
            await remoteLogService.SendLogAsync($"Started up - SiteType: {siteType} - Url: {FrontendUrlResolver.GetOrigin(configuration, app.Environment)} at {DateTime.Now}");
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
        app.UseMiddleware<ClientEnvMiddleware>();
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
        app.UseMiddleware<HardeningMiddleware>();
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
        app.MapHub<Backend.Hubs.ElectionPackageImportHub>("/hubs/election-package-import");
        app.MapHub<Backend.Hubs.FrontDeskHub>("/hubs/front-desk");
        app.MapHub<Backend.Hubs.PublicHub>("/hubs/public");
        app.MapHub<Backend.Hubs.AllVotersHub>("/hubs/all-voters");
        app.MapHub<Backend.Hubs.VoterPersonalHub>("/hubs/voter-personal");

        app.MapGet("/protected", () => "This is protected!").RequireAuthorization();

        // add a shutdown hook to help with server logs
        app.Lifetime.ApplicationStopping.Register(async () =>
        {
            Log.Information("Application Stopping...");
            await Log.CloseAndFlushAsync();
        });
    }

    /// <summary>
    /// Detects a common problematic state after migration squashing:
    /// The new "Initial" migration is still pending according to EF, but the database
    /// already contains TallyJ tables. This typically means an existing production/client
    /// database is missing the required row in the custom migrations history table
    /// (__EFMigrations_TallyJ4).
    ///
    /// Instead of letting EF fail with a cryptic "object already exists" error,
    /// we fail fast with a clear, actionable message.
    /// </summary>
    private static async Task EnsureConsistentMigrationHistoryAsync(MainDbContext context, Serilog.ILogger logger)
    {
        try
        {
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();

            const string initialMigrationId = "20260531185953_Initial";

            if (pendingMigrations.Contains(initialMigrationId))
            {
                // Probe the database to see if this looks like an existing TallyJ installation
                // rather than a brand new empty database.
                var connection = context.Database.GetDbConnection();
                var openedHere = false;

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                    openedHere = true;
                }

                try
                {
                    using var cmd = connection.CreateCommand();
                    // Elections is a core domain table created very early in the Initial migration.
                    cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(N'[Elections]', N'U') IS NOT NULL THEN 1 ELSE 0 END";
                    var result = await cmd.ExecuteScalarAsync();
                    var hasExistingElectionsTable = result is not null && Convert.ToInt32(result) == 1;

                    if (hasExistingElectionsTable)
                    {
                        const string historyTable = "__EFMigrations_TallyJ4";

                        var diagnosticMessage =
                            $"""
                            ╔══════════════════════════════════════════════════════════════════════════════╗
                            ║  CRITICAL: DATABASE MIGRATION STATE INCONSISTENT                             ║
                            ╠══════════════════════════════════════════════════════════════════════════════╣
                            ║                                                                              ║
                            ║  EF Core reports that migration '{initialMigrationId}' is still pending.     ║
                            ║  However, the database already contains core TallyJ tables (e.g. Elections). ║
                            ║                                                                              ║
                            ║  This almost always means the migration history row is missing from the      ║
                            ║  custom history table used by MainDbContext.                                   ║
                            ║                                                                              ║
                            ║  History table actually used by this application: [{historyTable}]           ║
                            ║                                                                              ║
                            ║  TO FIX:                                                                     ║
                            ║  1. Connect to the SQL Server database with a tool that can run SQL.         ║
                            ║  2. Execute the following statement:                                         ║
                            ║                                                                              ║
                            ║     INSERT INTO [{historyTable}] ([MigrationId], [ProductVersion])           ║
                            ║     VALUES ('{initialMigrationId}', '10.0.6');                               ║
                            ║                                                                              ║
                            ║  3. Verify the row was added:                                                ║
                            ║     SELECT * FROM [{historyTable}];                                          ║
                            ║                                                                              ║
                            ║  4. Restart the application.                                                 ║
                            ║                                                                              ║
                            ║  If you are unsure, or this is a production system, take a backup first.     ║
                            ║  After applying the fix, migrations will proceed normally on future updates. ║
                            ║                                                                              ║
                            ╚══════════════════════════════════════════════════════════════════════════════╝
                            """;

                        logger.Fatal(diagnosticMessage);

                        // Also log the full pending list for support diagnostics
                        logger.Fatal("Pending migrations detected: {PendingMigrations}", string.Join(", ", pendingMigrations));

                        throw new InvalidOperationException(
                            $"Database migration blocked. The row for migration '{initialMigrationId}' is missing from [{historyTable}]. " +
                            "See the Fatal log message above for the exact SQL command to run.");
                    }
                }
                finally
                {
                    if (openedHere && connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Re-throw our intentional blocking exception
            throw;
        }
        catch (Exception ex)
        {
            // If the guard itself fails (e.g. permission issues on the history table or schema probe),
            // log it but do not block startup. Let the normal EF MigrateAsync() run and fail
            // with whatever error it would have produced. This avoids making things worse.
            logger.Warning(ex, "Migration history consistency check encountered an unexpected error and was skipped");
        }
    }
}
