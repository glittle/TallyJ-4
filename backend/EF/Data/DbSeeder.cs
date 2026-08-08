using Backend.Context;
using Backend.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.EF.Data;

/// <summary>
/// Static class responsible for seeding the database with initial test data.
/// Creates sample elections, users, roles, and related entities for development and testing.
/// </summary>
public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the database with initial data if it hasn't been seeded already.
    /// Creates roles, users, and sample elections with associated data.
    /// </summary>
    /// <param name="context">The main database context.</param>
    /// <param name="userManager">The user manager for identity operations.</param>
    /// <param name="roleManager">The role manager for identity operations.</param>
    /// <param name="logger">The logger for recording seeding operations.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static async Task SeedAsync(
        MainDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        if (await context.Elections.AnyAsync())
        {
            logger.LogInformation("Database already seeded");
            await SeedElection1OnlineVotingAsync(context, logger);
            await SeedOnlineVotingTestElectionsAsync(context, logger);
            await context.SaveChangesAsync();
            return;
        }

        logger.LogInformation("Starting database seeding...");

        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, logger);
        await SeedElection1Async(context, userManager, logger);
        await SeedElection2Async(context, userManager, logger);
        await SeedOnlineVotingTestElectionsAsync(context, logger);
        await SeedLogsAsync(context, logger);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeding complete");
    }
}
