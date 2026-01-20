using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TallyJ4.Domain.Identity;
using TallyJ4.EF.Context;

namespace TallyJ4.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:SeedOnStartup"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core and DbContext-related service descriptors
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType.IsGenericType && (
                    d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) ||
                    d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextFactory<>)
                ) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(MainDbContext)
            ).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<MainDbContext>(options =>
            {
                options.UseInMemoryDatabase("TallyJ4TestDb");
            });
        });

        builder.UseEnvironment("Testing");
    }
}
