using Backend.Identity;
using Microsoft.AspNetCore.Identity;

namespace Backend.EF.Data;

public static partial class DbSeeder
{
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        logger.LogInformation("Seeding roles...");

        var roles = new[] { "Admin", "Teller", "Guest" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {Role}", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager, ILogger logger)
    {
        logger.LogInformation("Seeding users...");

        var users = new[]
        {
            new { Email = "admin@tallyj.test", Password = "TestPass123!", Role = "Admin" },
            new { Email = "teller@tallyj.test", Password = "TestPass123!", Role = "Teller" },
            new { Email = "voter@tallyj.test", Password = "TestPass123!", Role = "Guest" }
        };

        foreach (var userData in users)
        {
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new AppUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, userData.Role);
                    logger.LogInformation("Created user: {Email} with role {Role}", userData.Email, userData.Role);
                }
                else
                {
                    logger.LogError("Failed to create user {Email}: {Errors}",
                        userData.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
