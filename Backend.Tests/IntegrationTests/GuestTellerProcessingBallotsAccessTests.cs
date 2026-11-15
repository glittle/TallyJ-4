using System.Net;
using Backend.Context;
using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.Locations;
using Backend.DTOs.People;
using Backend.DTOs.Votes;
using Backend.Enumerations;
using Backend.Models;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Processing Ballots guest tellers use Front Desk and Enter Ballots.
/// Front Desk and ballot reads stay on <c>ElectionAccess</c> at any stage.
/// Guest paper-entry writes succeed only during Processing Ballots.
/// </summary>
public class GuestTellerProcessingBallotsAccessTests : IntegrationTestBase
{
    public GuestTellerProcessingBallotsAccessTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ProcessingBallots_GuestTeller_CanListAndCheckIn_AndListBallots()
    {
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var (electionGuid, personGuid) = await SeedElectionWithVoterAsync();
        await SetStageAsync(electionGuid, ElectionStage.ProcessingBallots);

        using (var scope = Factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            SetAuthToken(tokens.GenerateTellerToken(electionGuid));
        }

        var voters = await GetAsync($"/api/{electionGuid}/frontdesk/eligibleVoters");
        Assert.Equal(HttpStatusCode.OK, voters.StatusCode);
        var voterList = await DeserializeResponseAsync<ApiResponse<List<FrontDeskVoterDto>>>(voters);
        Assert.Contains(voterList!.Data!, v => v.PersonGuid == personGuid);

        var stats = await GetAsync($"/api/{electionGuid}/frontdesk/stats");
        Assert.Equal(HttpStatusCode.OK, stats.StatusCode);

        var rollCall = await GetAsync($"/api/{electionGuid}/frontdesk/rollCall");
        Assert.Equal(HttpStatusCode.OK, rollCall.StatusCode);

        var checkIn = await PostJsonAsync($"/api/{electionGuid}/frontdesk/checkInVoter", new CheckInVoterDto
        {
            PersonGuid = personGuid,
            VotingMethod = "P",
            Teller1 = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);
        var checkedIn = await DeserializeResponseAsync<ApiResponse<FrontDeskVoterDto>>(checkIn);
        Assert.Equal(personGuid, checkedIn!.Data!.PersonGuid);

        var ballots = await GetAsync($"/api/Ballots/{electionGuid}/ballots?pageNumber=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, ballots.StatusCode);
    }

    [Fact]
    public async Task GatheringBallots_GuestTeller_PaperEntryWrites_Return403_ReadsStayOpen()
    {
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var seeded = await SeedPaperEntryElectionAsync();
        var ballot = await CreateBallotAsync(seeded.ElectionGuid, seeded.LocationGuid);
        var vote = await CreateVoteAsync(ballot.BallotGuid, seeded.PersonGuid);
        await SetStageAsync(seeded.ElectionGuid, ElectionStage.GatheringBallots);
        UseGuestToken(seeded.ElectionGuid);

        await AssertGuestPaperEntryForbidden(await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = seeded.ElectionGuid,
            LocationGuid = seeded.LocationGuid,
            ComputerCode = "B",
            Teller1 = "Guest"
        }));
        await AssertGuestPaperEntryForbidden(await PostJsonAsync("/api/Votes/createVote", new CreateVoteDto
        {
            BallotGuid = ballot.BallotGuid,
            PersonGuid = seeded.PersonGuid,
            PositionOnBallot = 2
        }));
        await AssertGuestPaperEntryForbidden(await PutJsonAsync($"/api/Ballots/{ballot.BallotGuid}/updateBallot", new UpdateBallotDto
        {
            Teller1 = "Guest"
        }));
        await AssertGuestPaperEntryForbidden(await DeleteAsync($"/api/Ballots/{ballot.BallotGuid}/deleteBallot"));
        await AssertGuestPaperEntryForbidden(await PutJsonAsync($"/api/Votes/{vote.RowId}/updateVote", new CreateVoteDto
        {
            BallotGuid = ballot.BallotGuid,
            PersonGuid = seeded.PersonGuid
        }));
        await AssertGuestPaperEntryForbidden(await PutJsonAsync("/api/Votes/reorderVotes", new ReorderVotesDto
        {
            BallotGuid = ballot.BallotGuid,
            VoteRowIds = [vote.RowId]
        }));
        await AssertGuestPaperEntryForbidden(await DeleteAsync($"/api/Votes/{vote.RowId}/deleteVote"));

        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Ballots/{seeded.ElectionGuid}/ballots?pageNumber=1&pageSize=50")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Ballots/{ballot.BallotGuid}/ballot")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Votes/{ballot.BallotGuid}/getVotesByBallot")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/{seeded.ElectionGuid}/frontdesk/eligibleVoters")).StatusCode);
    }

    [Fact]
    public async Task GatheringBallots_Owner_CanCreateBallot()
    {
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var seeded = await SeedPaperEntryElectionAsync();
        await SetStageAsync(seeded.ElectionGuid, ElectionStage.GatheringBallots);

        var created = await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = seeded.ElectionGuid,
            LocationGuid = seeded.LocationGuid,
            ComputerCode = "A",
            Teller1 = "Owner"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task ProcessingBallots_GuestTeller_CanCreateBallotAndVote()
    {
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var seeded = await SeedPaperEntryElectionAsync();
        await SetStageAsync(seeded.ElectionGuid, ElectionStage.ProcessingBallots);
        UseGuestToken(seeded.ElectionGuid);

        var ballot = await CreateBallotAsync(seeded.ElectionGuid, seeded.LocationGuid);
        var vote = await CreateVoteAsync(ballot.BallotGuid, seeded.PersonGuid);
        Assert.True(vote.RowId > 0);

        var listed = await GetAsync($"/api/Ballots/{seeded.ElectionGuid}/ballots?pageNumber=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
    }

    private async Task<(Guid ElectionGuid, Guid PersonGuid)> SeedElectionWithVoterAsync()
    {
        var createElection = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = $"Guest processing {Guid.NewGuid():N}",
            DateOfElection = DateTime.UtcNow.AddDays(1),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            ShowAsTest = true
        });
        createElection.EnsureSuccessStatusCode();
        var election = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createElection);
        var electionGuid = election!.Data!.ElectionGuid;

        var createPerson = await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "Ada"
        });
        createPerson.EnsureSuccessStatusCode();
        var person = await DeserializeResponseAsync<ApiResponse<PersonDto>>(createPerson);

        return (electionGuid, person!.Data!.PersonGuid);
    }

    private async Task SetStageAsync(Guid electionGuid, ElectionStage stage)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var election = await db.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
        election.ElectionStage = stage;
        await db.SaveChangesAsync();
    }

    private async Task<(Guid ElectionGuid, Guid LocationGuid, Guid PersonGuid)> SeedPaperEntryElectionAsync()
    {
        var (electionGuid, personGuid) = await SeedElectionWithVoterAsync();
        var createLocation = await PostJsonAsync($"/api/{electionGuid}/locations/createLocation", new CreateLocationDto
        {
            ElectionGuid = electionGuid,
            Name = "Main Hall"
        });
        createLocation.EnsureSuccessStatusCode();
        var location = await DeserializeResponseAsync<ApiResponse<LocationDto>>(createLocation);
        return (electionGuid, location!.Data!.LocationGuid, personGuid);
    }

    private void UseGuestToken(Guid electionGuid)
    {
        using var scope = Factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        SetAuthToken(tokens.GenerateTellerToken(electionGuid));
    }

    private async Task<BallotDto> CreateBallotAsync(Guid electionGuid, Guid locationGuid)
    {
        var response = await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerCode = "A",
            Teller1 = "Ada"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await DeserializeResponseAsync<ApiResponse<BallotDto>>(response);
        return created!.Data!;
    }

    private async Task<VoteDto> CreateVoteAsync(Guid ballotGuid, Guid personGuid)
    {
        var response = await PostJsonAsync("/api/Votes/createVote", new CreateVoteDto
        {
            BallotGuid = ballotGuid,
            PersonGuid = personGuid,
            PositionOnBallot = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await DeserializeResponseAsync<ApiResponse<VoteWithBallotStatusDto>>(response);
        return created!.Data!.Vote!;
    }

    private async Task AssertGuestPaperEntryForbidden(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await DeserializeResponseAsync<ErrorBody>(response);
        Assert.Equal(ElectionStageMessageKeys.GuestBallotEntryOnlyDuringProcessing, body!.Error);
    }

    private sealed class ErrorBody
    {
        public string? Error { get; set; }
    }
}
