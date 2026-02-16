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
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Add SQL Server LocalDB for testing
            // Note: Program.cs skips DbContext registration in Testing environment
            var uniqueDbName = $"BackendTestDb_{Guid.NewGuid()}";
            var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={uniqueDbName};Trusted_Connection=True;";

            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Backend"));
                options.EnableSensitiveDataLogging();
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure database is created and migrations are applied
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        dbContext.Database.Migrate();

        return host;
    }


}



