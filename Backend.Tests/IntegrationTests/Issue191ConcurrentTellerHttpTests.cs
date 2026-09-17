using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Context;
using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.Locations;
using Backend.DTOs.OnlineVoting;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// #191 HTTP slice: two authenticated teller clients on the shared SQLite
/// factory. Service-level races stay in <c>Issue191ConcurrentTellerTests</c>.
/// </summary>
public class Issue191ConcurrentTellerHttpTests : IntegrationTestBase
{
    public Issue191ConcurrentTellerHttpTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task TwoTellerClients_CreatePaperBallotsConcurrently_BothPersist()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateOpenElectionAsync();
        var locationGuid = await CreatePaperLocationAsync(electionGuid);

        using var tellerA = CreateTellerClient(token);
        using var tellerB = CreateTellerClient(token);

        var responses = await Task.WhenAll(
            CreatePaperBallotHttpAsync(tellerA, electionGuid, locationGuid, "A", "Ada"),
            CreatePaperBallotHttpAsync(tellerB, electionGuid, locationGuid, "B", "Ben"));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var created = await Task.WhenAll(responses.Select(ReadBallotAsync));
        Assert.Equal(2, created.Select(b => b.BallotGuid).Distinct().Count());
        Assert.Contains(created, b => b.ComputerCode == "A" && b.BallotCode == "A1");
        Assert.Contains(created, b => b.ComputerCode == "B" && b.BallotCode == "B1");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var ballots = await context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .ToListAsync();
        Assert.Equal(2, ballots.Count);
        Assert.Equal(2, ballots.Select(b => b.BallotGuid).Distinct().Count());
        Assert.Equal(["A1", "B1"], ballots.OrderBy(b => b.ComputerCode).Select(b => b.BallotCode));
    }

    [Fact]
    public async Task TellerA_PaperBallot_WhileTellerB_AcceptAll_MonitorTotalsMatch()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateOpenElectionAsync();
        var locationGuid = await CreatePaperLocationAsync(electionGuid);
        var email = $"online_{Guid.NewGuid():N}@example.com";
        var personGuid = await AddPersonAsync(electionGuid, "Online", "Voter", email);
        await AddPersonAsync(electionGuid, "Paper", "Voter");

        var submit = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes =
                [
                    new OnlineVoteDto { PersonGuid = personGuid, PositionOnBallot = 1 }
                ]
            });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        using var tellerA = CreateTellerClient(token);
        using var tellerB = CreateTellerClient(token);

        var paperTask = CreatePaperBallotHttpAsync(tellerA, electionGuid, locationGuid, "A", "Ada");
        var acceptTask = tellerB.PostAsync(
            $"/api/elections/{electionGuid}/online-ballots/accept-all",
            null);
        await Task.WhenAll(paperTask, acceptTask);

        Assert.Equal(HttpStatusCode.Created, (await paperTask).StatusCode);
        var accept = await acceptTask;
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var acceptBody = await accept.Content.ReadFromJsonAsync<AcceptAllOnlineBallotsResultDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(acceptBody);
        Assert.True(acceptBody.Success);
        Assert.Equal(1, acceptBody.AcceptedCount);

        var monitor = await DeserializeResponseAsync<MonitorInfoDto>(
            await GetAsync($"/api/results/election/{electionGuid}/monitor"));
        Assert.NotNull(monitor);
        Assert.Equal(2, monitor.TotalBallots);
        Assert.Equal(1, monitor.BallotsByMethod.Online);
        Assert.Equal(0, monitor.OnlineVotingInfo.PendingOnlineBallots);
        Assert.Equal(1, monitor.OnlineVotingInfo.ProcessedOnlineBallots);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        Assert.Equal(1, await context.Ballots.CountAsync(b =>
            b.LocationGuid == locationGuid && b.ComputerCode == "A"));
        Assert.Equal(1, await context.Ballots.CountAsync(b =>
            b.ComputerCode == ComputerCodeHelper.Online));
        Assert.Equal(OnlineBallotStatus.Processed, (await context.OnlineVotingInfos
            .SingleAsync(o => o.PersonGuid == personGuid)).Status);
    }

    private HttpClient CreateTellerClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<HttpResponseMessage> CreatePaperBallotHttpAsync(
        HttpClient client,
        Guid electionGuid,
        Guid locationGuid,
        string computerCode,
        string teller)
    {
        return await client.PostAsJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerCode = computerCode,
            Teller1 = teller
        });
    }

    private async Task<BallotDto> ReadBallotAsync(HttpResponseMessage response)
    {
        var body = await DeserializeResponseAsync<ApiResponse<BallotDto>>(response);
        Assert.NotNull(body?.Data);
        return body.Data;
    }

    private async Task<Guid> CreateOpenElectionAsync()
    {
        var create = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = $"Concurrent tellers {Guid.NewGuid():N}",
            DateOfElection = DateTime.UtcNow.AddDays(1),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            UseOnlineVoting = true,
            VotingMethods = "PMDKO"
        });
        create.EnsureSuccessStatusCode();
        var election = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(create);
        var electionGuid = election!.Data!.ElectionGuid;

        var window = await PutJsonAsync(
            $"/api/elections/{electionGuid}/online-voting-window",
            new UpdateOnlineVotingWindowDto
            {
                OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
                OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(2),
                OnlineCloseIsEstimate = true
            });
        window.EnsureSuccessStatusCode();
        return electionGuid;
    }

    private async Task<Guid> CreatePaperLocationAsync(Guid electionGuid)
    {
        var create = await PostJsonAsync($"/api/{electionGuid}/locations/createLocation", new CreateLocationDto
        {
            ElectionGuid = electionGuid,
            Name = "Hall"
        });
        create.EnsureSuccessStatusCode();
        var location = await DeserializeResponseAsync<ApiResponse<LocationDto>>(create);
        return location!.Data!.LocationGuid;
    }

    private async Task<Guid> AddPersonAsync(
        Guid electionGuid,
        string first,
        string last,
        string? email = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var personGuid = Guid.NewGuid();
        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            FirstName = first,
            LastName = last,
            Email = email,
            CanVote = true,
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        });
        if (!string.IsNullOrEmpty(email) && !await context.OnlineVoters.AnyAsync(ov => ov.VoterId == email))
        {
            context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = email,
                VoterIdType = "E",
                WhenRegistered = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return personGuid;
    }
}
