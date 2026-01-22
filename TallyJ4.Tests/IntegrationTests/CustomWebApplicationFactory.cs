using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TallyJ4.EF.Context;

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
                ["Database:SeedOnStartup"] = "false"
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
            });
        });
    }
}
