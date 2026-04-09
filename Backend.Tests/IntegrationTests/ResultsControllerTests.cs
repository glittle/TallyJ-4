using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs.Elections;
using Backend.DTOs.Results;
using Backend.Domain.Enumerations;
using Backend.Models;
using Backend.Domain.Entities;
using Backend.Domain.Context;

namespace Backend.Tests.IntegrationTests;

public class ResultsControllerTests : IntegrationTestBase
{
    public ResultsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CalculateTally_WithValidElection_ReturnsOkWithResults()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateTestElectionWithBallotsAsync();

        var response = await Client.PostAsync($"/api/results/election/{electionGuid}/calculate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<TallyResultDto>(response);
        Assert.NotNull(result);
        Assert.Equal(electionGuid, result.ElectionGuid);
        Assert.NotNull(result.Statistics);
        Assert.NotNull(result.Results);
        Assert.True(result.Results.Count > 0);
    }

    [Fact]
    public async Task RowVersion_ConcurrencyControl_WorksCorrectly()
    {
        // This test verifies that RowVersion provides optimistic concurrency control
        // by attempting concurrent updates to the same entity

        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateTestElectionWithBallotsAsync();

        // Get the election entity
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var election = await dbContext.Elections.FirstAsync(e => e.ElectionGuid == electionGuid);
        var originalName = election.Name;
        var originalRowVersion = election.RowVersion;

        // Simulate concurrent updates by creating two contexts
        using var scope1 = Factory.Services.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<MainDbContext>();
        using var scope2 = Factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<MainDbContext>();

        var election1 = await context1.Elections.FirstAsync(e => e.ElectionGuid == electionGuid);
        var election2 = await context2.Elections.FirstAsync(e => e.ElectionGuid == electionGuid);

        // Initialize RowVersion if null (for SQLite)
        if (election1.RowVersion == null)
        {
            election1.RowVersion = new byte[8];
            await context1.SaveChangesAsync();
            // Refresh the second context
            await context2.Entry(election2).ReloadAsync();
        }

        // First update succeeds
        election1.Name = "Updated Name 1";
        // Manually update RowVersion for SQLite (since it doesn't auto-increment like SQL Server)
        if (election1.RowVersion != null)
        {
            var newVersion = new byte[election1.RowVersion.Length];
            election1.RowVersion.CopyTo(newVersion, 0);
            newVersion[newVersion.Length - 1]++; // Increment last byte
            election1.RowVersion = newVersion;
        }
        await context1.SaveChangesAsync();

        // Verify RowVersion changed
        Assert.NotEqual(originalRowVersion, election1.RowVersion);

        // Second update should fail due to concurrency conflict
        election2.Name = "Updated Name 2";

        // This should throw DbUpdateConcurrencyException if RowVersion is working
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await context2.SaveChangesAsync();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task CalculateTally_WithInvalidElectionGuid_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var invalidGuid = Guid.NewGuid();

        var response = await Client.PostAsync($"/api/results/election/{invalidGuid}/calculate", null);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetResults_WithExistingResults_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateTestElectionWithBallotsAsync();

        await Client.PostAsync($"/api/results/election/{electionGuid}/calculate", null);

        var response = await GetAsync($"/api/results/election/{electionGuid}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<TallyResultDto>(response);
        Assert.NotNull(result);
        Assert.Equal(electionGuid, result.ElectionGuid);
        Assert.NotNull(result.Results);
    }

    [Fact]
    public async Task GetSummary_WithCalculatedResults_ReturnsOkWithStatistics()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateTestElectionWithBallotsAsync();

        await Client.PostAsync($"/api/results/election/{electionGuid}/calculate", null);

        var response = await Client.GetAsync($"/api/results/election/{electionGuid}/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<TallyStatisticsDto>(response);
        Assert.NotNull(result);
        Assert.True(result.BallotsReceived > 0);
        Assert.True(result.TotalVotes > 0);
    }

    [Fact]
    public async Task GetFinal_ReturnsOnlyElectedAndExtraSections()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateTestElectionWithBallotsAsync();

        await Client.PostAsync($"/api/results/election/{electionGuid}/calculate", null);

        var response = await Client.GetAsync($"/api/results/election/{electionGuid}/final");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<TallyResultDto>(response);
        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        
        var sections = result.Results.Select(r => r.Section).Distinct().ToList();
        Assert.All(sections, section => Assert.True(section == "E" || section == "X"));
        Assert.DoesNotContain("O", sections);
    }

    [Fact]
    public async Task CalculateTally_WithoutAuthentication_ReturnsUnauthorized()
    {
        var electionGuid = Guid.NewGuid();

        var response = await Client.PostAsync($"/api/results/election/{electionGuid}/calculate", null);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateTestElectionWithBallotsAsync()
    {
        var createDto = new CreateElectionDto
        {
            Name = "Test Election for Results",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        await SeedElectionDataAsync(electionGuid);

        return electionGuid;
    }

    private async Task SeedElectionDataAsync(Guid electionGuid)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var location = new Location
        {
            LocationGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            Name = "Test Location"
        };
        context.Locations.Add(location);
        await context.SaveChangesAsync();

        var people = new List<Person>
        {
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Alice", LastName = "Anderson", ElectionGuid = electionGuid, CanReceiveVotes = true },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Bob", LastName = "Brown", ElectionGuid = electionGuid, CanReceiveVotes = true },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Carol", LastName = "Clark", ElectionGuid = electionGuid, CanReceiveVotes = true },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "David", LastName = "Davis", ElectionGuid = electionGuid, CanReceiveVotes = true },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Eve", LastName = "Evans", ElectionGuid = electionGuid, CanReceiveVotes = true }
        };

        context.People.AddRange(people);
        await context.SaveChangesAsync();

        var ballots = new List<Ballot>();
        for (int i = 0; i < 10; i++)
        {
            var ballot = new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                StatusCode = BallotStatus.Ok,
                BallotNumAtComputer = i + 1,
                ComputerCode = "C1",
                LocationGuid = location.LocationGuid
            };
            ballots.Add(ballot);

            var votes = new List<Vote>();
            for (int v = 0; v < 3; v++)
            {
                votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[v % people.Count].PersonGuid,
                    PositionOnBallot = v + 1,
                    VoteStatus = VoteStatus.Ok,
                    IneligibleReasonCode = null
                });
            }
            context.Votes.AddRange(votes);
        }

        context.Ballots.AddRange(ballots);
        await context.SaveChangesAsync();
    }
}



