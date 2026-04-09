using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Identity;
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
            // Use SQLite file database for testing (supports transactions unlike EF InMemory)
            // Note: Program.cs skips DbContext registration in Testing environment
            var uniqueDbName = $"BackendTestDb_{Guid.NewGuid()}.db";

            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlite($"Data Source={uniqueDbName}");
                options.EnableSensitiveDataLogging();
            });

            // Mock external services for testing
            services.AddSingleton<Backend.Application.Services.Auth.IEmailSender, MockEmailSender>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure the SQLite database schema is initialized
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        dbContext.Database.EnsureCreated();

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



