using System.Net;
using Backend.Context;
using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.Locations;
using Backend.DTOs.OnlineVoting;
using Backend.DTOs.People;
using Backend.DTOs.Votes;
using Backend.Enumerations;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

public class FinalizedPeopleBallotWriteTests : IntegrationTestBase
{
    public FinalizedPeopleBallotWriteTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task FinalizedElection_RefusesPeopleBallotAndRollWrites_AllowsReads()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var (electionGuid, locationGuid, personGuid) = await SeedWritableElectionAsync();

        var ballotBeforeLock = await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerCode = "A",
            Teller1 = "Ada"
        });
        ballotBeforeLock.EnsureSuccessStatusCode();
        var existingBallot = await DeserializeResponseAsync<ApiResponse<BallotDto>>(ballotBeforeLock);

        await SetStageAsync(electionGuid, ElectionStage.Finalized);

        var createPerson = await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Blocked",
            FirstName = "New"
        });
        await AssertFinalizedWriteRefusal(createPerson);

        var updatePerson = await PutJsonAsync($"/api/People/{personGuid}/updatePerson", new UpdatePersonDto
        {
            LastName = "Changed",
            FirstName = "Ada"
        });
        await AssertFinalizedWriteRefusal(updatePerson);

        var deletePerson = await DeleteAsync($"/api/People/{personGuid}/deletePerson");
        await AssertFinalizedWriteRefusal(deletePerson);

        var createBallot = await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerCode = "B",
            Teller1 = "Ada"
        });
        await AssertFinalizedWriteRefusal(createBallot);

        var createVote = await PostJsonAsync("/api/Votes/createVote", new CreateVoteDto
        {
            BallotGuid = existingBallot!.Data!.BallotGuid,
            PersonGuid = personGuid,
            PositionOnBallot = 1
        });
        await AssertFinalizedWriteRefusal(createVote);

        var checkIn = await PostJsonAsync($"/api/{electionGuid}/frontdesk/checkInVoter", new CheckInVoterDto
        {
            PersonGuid = personGuid,
            VotingMethod = "P",
            Teller1 = "Ada"
        });
        await AssertFinalizedWriteRefusal(checkIn);

        var acceptAll = await Client.PostAsync(
            $"/api/elections/{electionGuid}/online-ballots/accept-all",
            null);
        Assert.Equal(HttpStatusCode.BadRequest, acceptAll.StatusCode);
        var acceptAllBody = await DeserializeResponseAsync<AcceptAllOnlineBallotsResultDto>(acceptAll);
        Assert.False(acceptAllBody!.Success);
        Assert.Equal("monitoring.acceptAll.finalized", acceptAllBody.MessageKey);

        var peopleRead = await GetAsync($"/api/People/{electionGuid}/getPeople?pageNumber=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, peopleRead.StatusCode);

        var personRead = await GetAsync($"/api/People/{personGuid}/getPerson");
        Assert.Equal(HttpStatusCode.OK, personRead.StatusCode);

        var ballotsRead = await GetAsync($"/api/Ballots/{electionGuid}/ballots?pageNumber=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, ballotsRead.StatusCode);

        var rollRead = await GetAsync($"/api/{electionGuid}/frontdesk/eligibleVoters");
        Assert.Equal(HttpStatusCode.OK, rollRead.StatusCode);

        var votesRead = await GetAsync($"/api/Votes/{existingBallot.Data.BallotGuid}/getVotesByBallot");
        Assert.Equal(HttpStatusCode.OK, votesRead.StatusCode);

        var ballotRead = await GetAsync($"/api/Ballots/{existingBallot.Data.BallotGuid}/ballot");
        Assert.Equal(HttpStatusCode.OK, ballotRead.StatusCode);
    }

    [Fact]
    public async Task ProcessingBallotsElection_AllowsPeopleBallotAndRollWrites()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var (electionGuid, locationGuid, personGuid) = await SeedWritableElectionAsync();
        await SetStageAsync(electionGuid, ElectionStage.ProcessingBallots);

        var createPerson = await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Allowed",
            FirstName = "New"
        });
        Assert.Equal(HttpStatusCode.Created, createPerson.StatusCode);

        var createBallot = await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerCode = "A",
            Teller1 = "Ada"
        });
        Assert.Equal(HttpStatusCode.Created, createBallot.StatusCode);
        var ballot = await DeserializeResponseAsync<ApiResponse<BallotDto>>(createBallot);
        Assert.NotNull(ballot?.Data?.BallotGuid);

        var createVote = await PostJsonAsync("/api/Votes/createVote", new CreateVoteDto
        {
            BallotGuid = ballot!.Data!.BallotGuid,
            PersonGuid = personGuid,
            PositionOnBallot = 1
        });
        Assert.Equal(HttpStatusCode.Created, createVote.StatusCode);

        var checkIn = await PostJsonAsync($"/api/{electionGuid}/frontdesk/checkInVoter", new CheckInVoterDto
        {
            PersonGuid = personGuid,
            VotingMethod = "P",
            Teller1 = "Ada"
        });
        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);
    }

    private async Task<(Guid ElectionGuid, Guid LocationGuid, Guid PersonGuid)> SeedWritableElectionAsync()
    {
        var createElection = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = $"Finalized write gate {Guid.NewGuid():N}",
            DateOfElection = DateTime.UtcNow.AddDays(1),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        });
        createElection.EnsureSuccessStatusCode();
        var election = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createElection);
        var electionGuid = election!.Data!.ElectionGuid;

        var createLocation = await PostJsonAsync($"/api/{electionGuid}/locations/createLocation", new CreateLocationDto
        {
            ElectionGuid = electionGuid,
            Name = "Main Hall"
        });
        createLocation.EnsureSuccessStatusCode();
        var location = await DeserializeResponseAsync<ApiResponse<LocationDto>>(createLocation);
        var locationGuid = location!.Data!.LocationGuid;

        var createPerson = await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "Ada"
        });
        createPerson.EnsureSuccessStatusCode();
        var person = await DeserializeResponseAsync<ApiResponse<PersonDto>>(createPerson);

        return (electionGuid, locationGuid, person!.Data!.PersonGuid);
    }

    private async Task SetStageAsync(Guid electionGuid, ElectionStage stage)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var election = await db.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
        election.ElectionStage = stage;
        await db.SaveChangesAsync();
    }

    private async Task AssertFinalizedWriteRefusal(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(ElectionStageMessageKeys.FinalizedWriteBlocked, body);
    }
}
