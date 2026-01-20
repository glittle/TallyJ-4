using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TallyJ4.DTOs.Elections;
using TallyJ4.DTOs.Results;
using TallyJ4.Models;
using TallyJ4.Domain.Entities;
using TallyJ4.EF.Context;

namespace TallyJ4.Tests.IntegrationTests;

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

        var response = await Client.GetAsync($"/api/results/election/{electionGuid}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<TallyResultDto>(response);
        Assert.NotNull(result);
        Assert.Equal(electionGuid, result.ElectionGuid);
        Assert.NotNull(result.Results);
    }

    private async Task<Guid> CreateTestElectionWithBallotsAsync()
    {
        var createDto = new CreateElectionDto
        {
            Name = "Test Election for Results",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = "Normal",
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections", createDto);
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
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Alice", LastName = "Anderson", ElectionGuid = electionGuid },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Bob", LastName = "Brown", ElectionGuid = electionGuid },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Carol", LastName = "Clark", ElectionGuid = electionGuid },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "David", LastName = "Davis", ElectionGuid = electionGuid },
            new() { PersonGuid = Guid.NewGuid(), FirstName = "Eve", LastName = "Evans", ElectionGuid = electionGuid }
        };

        context.People.AddRange(people);
        await context.SaveChangesAsync();

        var ballots = new List<Ballot>();
        for (int i = 0; i < 10; i++)
        {
            var ballot = new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                StatusCode = "Ok",
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
                    StatusCode = "Ok",
                    InvalidReasonGuid = null
                });
            }
            context.Votes.AddRange(votes);
        }

        context.Ballots.AddRange(ballots);
        await context.SaveChangesAsync();
    }
}
