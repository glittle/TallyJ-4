using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Tests.IntegrationTests;

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
                ["Jwt:Issuer"] = "TallyJ4TestAPI",
                ["Jwt:Audience"] = "TallyJ4TestClient",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Add SQL Server LocalDB for testing
            // Note: Program.cs skips DbContext registration in Testing environment
            var uniqueDbName = $"TallyJ4TestDb_{Guid.NewGuid()}";
            var connectionString = $"(localdb)\\MSSQLLocalDB;Database={uniqueDbName};Trusted_Connection=True;";

            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
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
