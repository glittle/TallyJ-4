using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Backend.Domain.Context;
using Backend.EF.Data;
using Backend.Application.Services.Auth;
using MimeKit;

namespace Backend.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:MigrateOnStartup"] = "false",
                ["Database:SeedOnStartup"] = "false",
                ["Jwt:Key"] = "ThisIsATestKeyThatIsAtLeast32CharactersLongForJWT",
                ["Jwt:Issuer"] = "BackendTestAPI",
                ["Jwt:Audience"] = "BackendTestClient",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Encryption:Key"] = "ThisIsATestEncryptionKeyThatIsAtLeast32CharactersLong",
                ["Email:SmtpHost"] = "<not-configured-for-tests>",
                ["Email:FromAddress"] = "test@tallyj.com",
                ["Email:FromName"] = "TallyJ Test",
                ["Localization:ResourcesPath"] = "../frontend/src/locales",
                ["Logging:LogLevel:Backend.Services.SecurityAuditService"] = "Error"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Use InMemory database for testing - ensures fresh database for each test run
            // Note: Program.cs skips DbContext registration in Testing environment
            var uniqueDbName = $"BackendTestDb_{Guid.NewGuid()}";

            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseInMemoryDatabase(uniqueDbName);
                options.EnableSensitiveDataLogging();
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // Add DataProtection context for testing
            services.AddDbContext<DataProtectionDbContext>(options =>
            {
                options.UseInMemoryDatabase($"{uniqueDbName}_DataProtection");
            });

            // Mock external services for testing
            services.AddSingleton<Backend.Application.Services.Auth.IEmailSender, MockEmailSender>();
        });

        // Avoid calling builder.Configure(app => ...) here as it overwrites the pipeline.
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Console.WriteLine("[TEST FACTORY] Creating host...");
        var host = base.CreateHost(builder);
        Console.WriteLine("[TEST FACTORY] Host created successfully");

        // Ensure the InMemory database schema is initialized
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        dbContext.Database.EnsureCreated();
        Console.WriteLine("[TEST FACTORY] Database schema initialized");

        return host;
    }

    /// <summary>
    /// Mock email sender that doesn't make any external connections.
    /// Used in integration tests to avoid external dependencies.
    /// </summary>
    private class MockEmailSender : IEmailSender
    {
        public Task SendAsync(MimeMessage message)
        {
            // Do nothing - just log that we would have sent an email
            Console.WriteLine($"[MOCK EMAIL] Would send email to {message.To} with subject '{message.Subject}'");
            return Task.CompletedTask;
        }
    }


}



