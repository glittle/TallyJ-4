using System.Reflection;
using Backend.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Backend.Tests.UnitTests;

/// <summary>
/// EF Core discovers migrations by <see cref="DbContextAttribute"/> +
/// <see cref="MigrationAttribute"/>, not by filename. A hand-written
/// <see cref="Migration"/> subclass without those attributes is compiled
/// but never applied, so startup logs "already up to date" while columns
/// are still missing.
/// </summary>
public class MainDbContextMigrationDiscoveryTests
{
    [Fact]
    public void MainDbContext_migrations_have_DbContext_and_Migration_attributes()
    {
        var types = typeof(MainDbContext).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .Where(t => t.Namespace == "Backend.Migrations")
            .ToList();

        Assert.NotEmpty(types);

        var missing = types
            .Where(t =>
                t.GetCustomAttribute<DbContextAttribute>()?.ContextType != typeof(MainDbContext)
                || t.GetCustomAttribute<MigrationAttribute>() is null)
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "EF Core will not apply these MainDbContext migrations (missing [DbContext] and/or [Migration]): "
            + string.Join(", ", missing));
    }

    [Fact]
    public void MainDbContext_migration_ids_include_hand_written_september_columns()
    {
        var ids = typeof(MainDbContext).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .Where(t => t.Namespace == "Backend.Migrations")
            .Select(t => t.GetCustomAttribute<MigrationAttribute>()?.Id)
            .Where(id => id is not null)
            .ToHashSet();

        Assert.Contains("20260913160000_AddGuestTellersCanAddPeople", ids);
        Assert.Contains("20260913220000_AddOnlineVoterWhatsAppStatus", ids);
    }
}
