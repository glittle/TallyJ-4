using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TallyJ4.Domain.Context;

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
            // Add InMemory database for testing
            // Note: Program.cs skips DbContext registration in Testing environment
            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseInMemoryDatabase("TallyJ4TestDb");
                options.EnableSensitiveDataLogging();
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }
}
