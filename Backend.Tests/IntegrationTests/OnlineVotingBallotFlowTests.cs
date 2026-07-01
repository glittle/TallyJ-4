using System.Net;
using System.Net.Http.Json;
using Backend.Context;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.IntegrationTests;

public class OnlineVotingBallotFlowTests : IntegrationTestBase
{
    public OnlineVotingBallotFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task VerifyCode_WithKioskCode_AuthenticatesDirectly()
    {
        var kioskCode = "JTEST";
        await SetupOpenElectionWithVoter(kioskCode: kioskCode);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(kioskCode, auth.VoterId);
        Assert.Equal("C", auth.VoterIdType);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task VerifyCode_WithKPrefixedKioskCode_AuthenticatesDirectly()
    {
        var kioskCode = "JABCD";
        await SetupOpenElectionWithVoter(kioskCode: kioskCode);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = $"K_{kioskCode}",
            VerifyCode = $"K_{kioskCode}"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(kioskCode, auth.VoterId);
    }

    [Fact]
    public async Task SubmitBallot_RandomModeB_WithNineFreeTextVotes_Succeeds()
    {
        var email = $"random_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email, selectionProcess: "B");
        await EnsureOnlineVoterAsync(email, "E");

        var votes = Enumerable.Range(1, 9)
            .Select(i => new OnlineVoteDto
            {
                VoteName = $"Free Voter {i}",
                PositionOnBallot = i
            })
            .ToList();

        var response = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes = votes
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitBallot_BothModeC_WithPoolAndNineVotes_StoresListPool()
    {
        var email = $"both_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email, selectionProcess: "C");
        await EnsureOnlineVoterAsync(email, "E");

        var candidates = await SetupCandidatesAsync(electionGuid, 9);
        var pool = new List<OnlinePoolEntryDto>
        {
            new() { FullName = "Pool Person One", FirstName = "Pool", LastName = "One" },
            new() { FullName = "Pool Person Two", FirstName = "Pool", LastName = "Two" }
        };

        var votes = new List<OnlineVoteDto>();
        for (var i = 0; i < 7; i++)
        {
            votes.Add(new OnlineVoteDto
            {
                PersonGuid = candidates[i],
                PositionOnBallot = i + 1
            });
        }

        votes.Add(new OnlineVoteDto
        {
            VoteName = "Pool Person One",
            PositionOnBallot = 8
        });
        votes.Add(new OnlineVoteDto
        {
            VoteName = "Pool Person Two",
            PositionOnBallot = 9
        });

        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                ListPool = pool,
                NotifyWhenProcessed = true,
                Votes = votes
            });

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var votingInfo = await context.OnlineVotingInfos
                .FirstOrDefaultAsync(ovi => ovi.ElectionGuid == electionGuid);
            Assert.NotNull(votingInfo?.BallotGuid);

            var poolVotes = await context.Votes
                .Where(v => v.BallotGuid == votingInfo.BallotGuid && v.VoteStatus == VoteStatus.Raw)
                .ToListAsync();
            Assert.Equal(2, poolVotes.Count);
            Assert.Contains(poolVotes, v => v.PersonCombinedInfo == "Pool Person One");
            Assert.Contains(poolVotes, v => v.PersonCombinedInfo == "Pool Person Two");
        }

        var statusResponse = await Client.GetAsync(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        var status = await statusResponse.Content.ReadFromJsonAsync<OnlineVoteStatusDto>();
        Assert.NotNull(status);
        Assert.Equal(9, status.PriorVotes.Count);
        Assert.Equal(2, status.ListPool.Count);
        Assert.Equal("Pool Person One", status.ListPool[0].FullName);
        Assert.Contains(status.PriorVotes, v => v.VoteName == "Pool Person One");
        Assert.Contains(status.PriorVotes, v => v.VoteName == "Pool Person Two");
        Assert.True(status.NotifyWhenProcessed);
    }

    [Fact]
    public async Task SubmitBallot_ThenResubmitTwice_UpdatesTimestampEachTime()
    {
        var email = $"twice_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email);
        await EnsureOnlineVoterAsync(email, "E");
        var candidates = await SetupCandidatesAsync(electionGuid, 2);

        async Task<DateTimeOffset?> SubmitAndGetTimestamp(int voteCount)
        {
            var dto = new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes = candidates.Take(voteCount).Select((c, i) => new OnlineVoteDto
                {
                    PersonGuid = c,
                    PositionOnBallot = i + 1
                }).ToList()
            };
            var response = await Client.PostAsJsonAsync(
                $"/api/online-voting/{electionGuid}/submitBallot", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var status = await Client.GetFromJsonAsync<OnlineVoteStatusDto>(
                $"/api/online-voting/{electionGuid}/{email}/voteStatus");
            return status?.WhenSubmitted;
        }

        var first = await SubmitAndGetTimestamp(2);
        var second = await SubmitAndGetTimestamp(1);
        var third = await SubmitAndGetTimestamp(2);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        Assert.True(second > first);
        Assert.True(third > second);
    }

    [Fact]
    public async Task RequestCode_WithPhoneNumber_SucceedsForRegisteredVoter()
    {
        var phone = "+15559876543";
        await SetupOpenElectionWithVoter(phone: phone);

        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", new RequestCodeDto
        {
            VoterId = phone,
            VoterIdType = "P",
            DeliveryMethod = "sms"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("voting.auth.requestCode.", content);
    }

    [Fact]
    public async Task SubmitBallot_ThenResubmit_UpdatesBallotAndReturnsPriorVotes()
    {
        var email = $"ballot_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email);
        await EnsureOnlineVoterAsync(email, "E");

        var candidates = await SetupCandidatesAsync(electionGuid, 3);
        var submitDto = new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            NotifyWhenProcessed = true,
            Votes = candidates.Select((c, i) => new OnlineVoteDto
            {
                PersonGuid = c,
                PositionOnBallot = i + 1
            }).ToList()
        };

        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot", submitDto);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var statusResponse = await Client.GetAsync(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<OnlineVoteStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.HasVoted);
        Assert.Equal(3, status.PriorVotes.Count);
        Assert.True(status.NotifyWhenProcessed);

        var resubmitDto = new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            NotifyWhenProcessed = false,
            Votes = candidates.Take(2).Select((c, i) => new OnlineVoteDto
            {
                PersonGuid = c,
                PositionOnBallot = i + 1
            }).ToList()
        };

        var resubmitResponse = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot", resubmitDto);
        Assert.Equal(HttpStatusCode.OK, resubmitResponse.StatusCode);

        var updatedStatusResponse = await Client.GetAsync(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        var updatedStatus = await updatedStatusResponse.Content.ReadFromJsonAsync<OnlineVoteStatusDto>();
        Assert.NotNull(updatedStatus);
        Assert.Equal(2, updatedStatus.PriorVotes.Count);
        Assert.False(updatedStatus.NotifyWhenProcessed);
        Assert.NotEqual(status.WhenSubmitted, updatedStatus.WhenSubmitted);
    }

    private async Task EnsureOnlineVoterAsync(string voterId, string voterIdType)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        if (!await context.OnlineVoters.AnyAsync(ov => ov.VoterId == voterId))
        {
            context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = voterId,
                VoterIdType = voterIdType,
                WhenRegistered = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    private async Task<List<Guid>> SetupCandidatesAsync(Guid electionGuid, int count)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var guids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var guid = Guid.NewGuid();
            guids.Add(guid);
            context.People.Add(new Person
            {
                ElectionGuid = electionGuid,
                PersonGuid = guid,
                FirstName = $"Candidate{i}",
                LastName = "Test",
                CanReceiveVotes = true,
                CanVote = true,
                RowVersion = new byte[8]
            });
        }

        await context.SaveChangesAsync();
        return guids;
    }

    private async Task<Guid> SetupOpenElectionWithVoter(
        string? email = null,
        string? kioskCode = null,
        string? phone = null,
        string? selectionProcess = "A")
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Ballot Flow Election",
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            NumberToElect = 9,
            OnlineSelectionProcess = selectionProcess,
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });

        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Voter",
            Email = email,
            Phone = phone,
            KioskCode = kioskCode,
            CanVote = true,
            RowVersion = new byte[8]
        });

        await context.SaveChangesAsync();
        return electionGuid;
    }
}