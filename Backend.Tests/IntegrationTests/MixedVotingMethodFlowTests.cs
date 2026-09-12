using System.Net;
using System.Net.Http.Json;
using Backend.Context;
using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.Locations;
using Backend.DTOs.OnlineVoting;
using Backend.DTOs.Reports;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

public class MixedVotingMethodFlowTests : IntegrationTestBase
{
    public MixedVotingMethodFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task MixedMethods_SwitchFromSubmittedOnline_DoesNotDoubleCount()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var electionGuid = await CreateOpenElectionAsync();
        var locationGuid = await CreatePaperLocationAsync(electionGuid);
        var onlineEmail = $"online_{Guid.NewGuid():N}@example.com";
        var switchEmail = $"switch_{Guid.NewGuid():N}@example.com";
        var mailedGuid = await AddPersonAsync(electionGuid, "Mailed", "Voter");
        var kioskGuid = await AddPersonAsync(electionGuid, "Kiosk", "Voter");
        var onlineGuid = await AddPersonAsync(electionGuid, "Online", "Voter", onlineEmail);
        var switchGuid = await AddPersonAsync(electionGuid, "Switch", "Voter", switchEmail);
        var pendingEmail = $"pending_{Guid.NewGuid():N}@example.com";
        await AddPersonAsync(electionGuid, "Pending", "Voter", pendingEmail);
        var candidates = await AddCandidatesAsync(electionGuid, 2);

        await CheckInAsync(electionGuid, mailedGuid, "M");
        await CheckInAsync(electionGuid, kioskGuid, "K");
        await CreatePaperBallotAsync(electionGuid, locationGuid);
        await CreatePaperBallotAsync(electionGuid, locationGuid);

        await SubmitOnlineAsync(electionGuid, onlineEmail, candidates);
        await SubmitOnlineAsync(electionGuid, switchEmail, candidates);
        await SubmitOnlineAsync(electionGuid, pendingEmail, candidates);

        var switchCheckIn = await PostJsonAsync(
            $"/api/{electionGuid}/frontdesk/checkInVoter",
            new CheckInVoterDto
            {
                PersonGuid = switchGuid,
                VotingMethod = "P",
                Teller1 = "Ada"
            });
        Assert.Equal(HttpStatusCode.OK, switchCheckIn.StatusCode);
        await CreatePaperBallotAsync(electionGuid, locationGuid);

        var accept = await Client.PostAsync(
            $"/api/elections/{electionGuid}/online-ballots/accept-all",
            null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var acceptBody = await DeserializeResponseAsync<AcceptAllOnlineBallotsResultDto>(accept);
        Assert.True(acceptBody!.Success);
        Assert.Equal(1, acceptBody.AcceptedCount);

        var monitor = await DeserializeResponseAsync<MonitorInfoDto>(
            await GetAsync($"/api/results/election/{electionGuid}/monitor"));
        Assert.NotNull(monitor);
        Assert.Equal(1, monitor.BallotsByMethod.InPerson);
        Assert.Equal(1, monitor.BallotsByMethod.Mailed);
        Assert.Equal(1, monitor.BallotsByMethod.Kiosk);
        Assert.Equal(1, monitor.BallotsByMethod.Online);
        Assert.Equal(0, monitor.BallotsByMethod.DroppedOff);
        Assert.Equal(1, monitor.OnlineVotingInfo.PendingOnlineBallots);
        Assert.Equal(0, monitor.OnlineVotingInfo.PendingOnlineVotedAnotherWay);
        Assert.Equal(1, monitor.OnlineVotingInfo.ProcessedOnlineBallots);
        Assert.Equal(4, monitor.TotalBallots);

        var refused = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = switchEmail,
                Votes =
                [
                    new OnlineVoteDto { PersonGuid = candidates[0], PositionOnBallot = 1 }
                ]
            });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var refusedBody = await refused.Content.ReadAsStringAsync();
        Assert.Contains("voting.submit.alreadyVotedAnotherWay", refusedBody);

        var processedCheckIn = await PostJsonAsync(
            $"/api/{electionGuid}/frontdesk/checkInVoter",
            new CheckInVoterDto
            {
                PersonGuid = onlineGuid,
                VotingMethod = "P",
                Teller1 = "Ada"
            });
        Assert.Equal(HttpStatusCode.BadRequest, processedCheckIn.StatusCode);
        var processedBody = await processedCheckIn.Content.ReadAsStringAsync();
        Assert.Contains(FrontDeskMessageKeys.AlreadyAcceptedOnline, processedBody);

        var recon = await DeserializeResponseAsync<CountReconciliationReportDto>(
            await GetAsync($"/api/results/election/{electionGuid}/reconciliation"));
        Assert.NotNull(recon);
        Assert.False(recon.IsReconciled);
        Assert.Equal(1, recon.PendingOnlineCount);
        Assert.Contains(recon.Mismatches, m => m.Kind == CountReconciliationMismatchKinds.PendingOnline);
        Assert.DoesNotContain(
            recon.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.DuplicateVotingPath);

        var votersByArea = await DeserializeResponseAsync<VotersByAreaReportDto>(
            await GetAsync($"/api/reports/{electionGuid}/VotersByArea"));
        Assert.NotNull(votersByArea);
        Assert.Equal(1, votersByArea.Total.InPerson);
        Assert.Equal(1, votersByArea.Total.MailedIn);
        Assert.Equal(1, votersByArea.Total.OnlineKiosk);
        Assert.Equal(1, votersByArea.Total.Online);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        Assert.Equal(1, await context.OnlineVotingInfos.CountAsync(o =>
            o.ElectionGuid == electionGuid && o.Status == OnlineBallotStatus.Processed));
        Assert.Equal(1, await context.OnlineVotingInfos.CountAsync(o =>
            o.ElectionGuid == electionGuid && o.Status == OnlineBallotStatus.Submitted));
        Assert.False(await context.OnlineVotingInfos.AnyAsync(o =>
            o.ElectionGuid == electionGuid && o.PersonGuid == switchGuid));
        Assert.Equal(4, await context.Ballots.CountAsync(b => b.Location.ElectionGuid == electionGuid));
    }

    private async Task<Guid> CreateOpenElectionAsync()
    {
        var create = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = $"Mixed methods {Guid.NewGuid():N}",
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

    private async Task<List<Guid>> AddCandidatesAsync(Guid electionGuid, int count)
    {
        var guids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            guids.Add(await AddPersonAsync(electionGuid, $"Cand{i}", "Eligible"));
        }

        return guids;
    }

    private async Task CheckInAsync(Guid electionGuid, Guid personGuid, string method)
    {
        var response = await PostJsonAsync(
            $"/api/{electionGuid}/frontdesk/checkInVoter",
            new CheckInVoterDto
            {
                PersonGuid = personGuid,
                VotingMethod = method,
                Teller1 = "Ada"
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task CreatePaperBallotAsync(Guid electionGuid, Guid locationGuid)
    {
        var response = await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            ComputerCode = "A",
            Teller1 = "Ada"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task SubmitOnlineAsync(
        Guid electionGuid,
        string email,
        IReadOnlyList<Guid> candidates)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes = candidates.Select((guid, i) => new OnlineVoteDto
                {
                    PersonGuid = guid,
                    PositionOnBallot = i + 1
                }).ToList()
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
