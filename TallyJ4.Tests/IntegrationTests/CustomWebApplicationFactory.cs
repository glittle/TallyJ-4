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

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        return host;
    }

    private async Task SeedDatabase(MainDbContext dbContext)
    {
        // Only seed if database doesn't have our test data
        if (await dbContext.Users.AnyAsync(u => u.Email == "admin@tallyj.com"))
        {
            return;
        }

        // Create test users
        var userManager = Services.GetRequiredService<UserManager<AppUser>>();
        var adminUser = new AppUser
        {
            UserName = "admin@tallyj.com",
            Email = "admin@tallyj.com",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");

        var testUser = new AppUser
        {
            UserName = "test@tallyj.com",
            Email = "test@tallyj.com",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(testUser, "Test123!");

        // Create test elections
        var election1 = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election 1",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = "STV",
            NumberToElect = 3,
            TallyStatus = "NotStarted",
            ShowAsTest = true
        };

        var election2 = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election 2",
            DateOfElection = DateTime.UtcNow.AddDays(60),
            ElectionType = "FPTP",
            NumberToElect = 1,
            TallyStatus = "NotStarted",
            ShowAsTest = true
        };

        dbContext.Elections.AddRange(election1, election2);
        await dbContext.SaveChangesAsync();

        // Create user-election relationships
        var join1 = new JoinElectionUser
        {
            ElectionGuid = election1.ElectionGuid,
            UserId = Guid.Parse(adminUser.Id),
            Role = "Admin"
        };

        var join2 = new JoinElectionUser
        {
            ElectionGuid = election2.ElectionGuid,
            UserId = Guid.Parse(adminUser.Id),
            Role = "Admin"
        };

        var join3 = new JoinElectionUser
        {
            ElectionGuid = election1.ElectionGuid,
            UserId = Guid.Parse(testUser.Id),
            Role = "Teller"
        };

        dbContext.JoinElectionUsers.AddRange(join1, join2, join3);
        await dbContext.SaveChangesAsync();
    }
}
