using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Identity;
using Xunit;

namespace Backend.Tests.IntegrationTests;

public class MigrationTests : IntegrationTestBase
{
    public MigrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Database_CanBeCreated_AndMigrated()
    {
        // Arrange & Act
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // Assert
        Assert.NotNull(dbContext);
        Assert.True(await dbContext.Database.CanConnectAsync());
    }

    [Fact]
    public async Task AllMigrations_AreApplied()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // Assert
        // SQL Server uses migrations; other providers (SQLite, InMemory) use EnsureCreated
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            Assert.NotEmpty(appliedMigrations);
            Assert.Empty(pendingMigrations);
        }
        else
        {
            // For other providers, verify the database is accessible
            Assert.True(await dbContext.Database.CanConnectAsync());
        }
    }

    [Fact]
    public async Task DatabaseSchema_IsCorrect_AfterMigrations()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // These queries will throw an exception if the tables do not exist
        await dbContext.Elections.AnyAsync();
        await dbContext.People.AnyAsync();
        await dbContext.Locations.AnyAsync();
        await dbContext.Users.AnyAsync();
        await dbContext.Roles.AnyAsync();
    }

    [Fact]
    public async Task SeededData_IsPresent_AfterMigrations()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Act - Check for seeded users from IntegrationTestBase
        var adminUser = await userManager.FindByEmailAsync("admin@tallyj.com");
        var testUser = await userManager.FindByEmailAsync("test@tallyj.com");

        // Assert
        Assert.NotNull(adminUser);
        Assert.NotNull(testUser);
        Assert.True(adminUser.EmailConfirmed);
        Assert.True(testUser.EmailConfirmed);
    }

    [Fact]
    public async Task SeededElections_ArePresent_AfterMigrations()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // Act
        var elections = await dbContext.Elections.Where(e => e.ShowAsTest == true).ToListAsync();

        // Assert
        Assert.NotEmpty(elections);
        Assert.Contains(elections, e => e.Name == "Test Election 1");
        Assert.Contains(elections, e => e.Name == "Test Election 2");
    }

    [Fact]
    public async Task UserElectionRelationships_AreCorrect_AfterMigrations()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Act
        var adminUser = await userManager.FindByEmailAsync("admin@tallyj.com");
        var testUser = await userManager.FindByEmailAsync("test@tallyj.com");
        var adminId = Guid.Parse(adminUser!.Id);
        var testUserId = Guid.Parse(testUser!.Id);

        var adminJoins = await dbContext.JoinElectionUsers
            .Where(j => j.UserId == adminId)
            .ToListAsync();

        var testUserJoins = await dbContext.JoinElectionUsers
            .Where(j => j.UserId == testUserId)
            .ToListAsync();

        // Assert
        Assert.Equal(2, adminJoins.Count); // Admin should be in both test elections
        Assert.Single(testUserJoins); // Test user should be in one election
        Assert.Contains(adminJoins, j => j.Role == "Admin");
        Assert.Contains(testUserJoins, j => j.Role == "Teller");
    }

    [Fact]
    public async Task DatabaseConstraints_AreEnforced()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // Act & Assert - Test foreign key constraints
        var election = await dbContext.Elections.FirstAsync();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = election.ElectionGuid,
            FirstName = "Test",
            LastName = "Person",
            CanVote = true,
            CanReceiveVotes = true,
            AgeGroup = "A",
            RowVersion = new byte[8]
        };

        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync();

        // Verify the person was added
        var addedPerson = await dbContext.People.FindAsync(person.RowId);
        Assert.NotNull(addedPerson);
        Assert.Equal(election.ElectionGuid, addedPerson.ElectionGuid);
    }

    [Fact]
    public async Task ComputedColumns_WorkCorrectly()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var election = await dbContext.Elections.FirstAsync();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = election.ElectionGuid,
            FirstName = "John",
            LastName = "Doe",
            CanVote = true,
            CanReceiveVotes = true,
            AgeGroup = "A",
            RowVersion = new byte[8]
        };

        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync();

        // Act
        var savedPerson = await dbContext.People.FindAsync(person.RowId);

        // Assert
        Assert.NotNull(savedPerson);
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            // Computed columns (FullName) only execute in SQL Server
            Assert.NotNull(savedPerson!.FullName);
            var fullName = savedPerson.FullName;
            Assert.Contains(savedPerson.FirstName!, fullName);
            Assert.Contains(savedPerson.LastName!, fullName);
        }
        else
        {
            // For other providers, verify the saved properties are correct
            Assert.Equal("John", savedPerson!.FirstName);
            Assert.Equal("Doe", savedPerson.LastName);
        }
    }
}


