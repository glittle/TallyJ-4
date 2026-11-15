using System.Net;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.People;
using Backend.Enumerations;
using Backend.Models;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Processing Ballots guest tellers use Front Desk and Enter Ballots.
/// Stage filtering is the guest menu, not a second authorization policy:
/// <c>ElectionAccess</c> already admits the guest for the election on the token.
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
}
