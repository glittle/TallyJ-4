using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Context;
using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.OnlineVoting;
using Backend.DTOs.Reports;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Middleware;
using Backend.Models;
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
        Assert.True(string.IsNullOrEmpty(auth.Token));
        Assert.False(string.IsNullOrWhiteSpace(GetSetCookieValue(response, "voter_token")));
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
    public async Task VerifyCode_WithExpiredKioskWindow_ReturnsCodeExpired()
    {
        var kioskCode = "JEXPD";
        await SetupOpenElectionWithVoter(kioskCode: kioskCode);
        await SetKioskVerifyCodeDateAsync(kioskCode, DateTimeOffset.UtcNow.AddMinutes(-16));

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("voting.auth.verify.codeExpired", body);
    }

    [Fact]
    public async Task GenerateKioskCode_RenewsExpiredWindow_ThenAuthSucceeds()
    {
        var kioskCode = "JRENW";
        var electionGuid = await SetupOpenElectionWithVoter(kioskCode: kioskCode);
        await SetElectionVotingMethodsAsync(electionGuid, "K");
        await SetKioskVerifyCodeDateAsync(kioskCode, DateTimeOffset.UtcNow.AddMinutes(-16));

        var personGuid = await GetPersonGuidByKioskCodeAsync(kioskCode);
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var generate = await Client.PostAsync($"/api/People/{personGuid}/generateKioskCode", null);
        Assert.Equal(HttpStatusCode.OK, generate.StatusCode);

        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitKioskBallot_EndsLoginWindow_ThenSameCodeCannotAuthenticate()
    {
        var kioskCode = "JSUBM";
        var electionGuid = await SetupOpenElectionWithVoter(kioskCode: kioskCode);

        var auth = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });
        Assert.Equal(HttpStatusCode.OK, auth.StatusCode);

        var submit = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = kioskCode,
                Votes =
                [
                    new OnlineVoteDto { VoteName = "Kiosk Choice", PositionOnBallot = 1 }
                ]
            });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var again = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        var body = await again.Content.ReadAsStringAsync();
        Assert.Contains("voting.auth.verify.codeExpired", body);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("P")]
    public async Task VerifyCode_WithEmailOrPhoneRowMatchingKioskCode_DoesNotAuthenticateAsKiosk(string occupantType)
    {
        var kioskCode = occupantType == "E" ? "JEMAL" : "JPHON";
        await SetupOpenElectionWithVoter(kioskCode: kioskCode);
        await ReplaceKioskOnlineVoterAsAsync(kioskCode, occupantType);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("voting.auth.verify.codeExpired", body);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await context.OnlineVoters.SingleAsync(ov => ov.VoterId == kioskCode);
        Assert.Equal(occupantType, row.VoterIdType);
    }

    [Fact]
    public async Task KioskLogout_ClearsVoterCookies_ThenMeIsUnauthorized()
    {
        var kioskCode = "JLOUT";
        await SetupOpenElectionWithVoter(kioskCode: kioskCode);

        var auth = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });
        Assert.Equal(HttpStatusCode.OK, auth.StatusCode);
        var token = GetSetCookieValue(auth, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.False(string.IsNullOrWhiteSpace(token));

        SetVoterCookie(token!);
        var me = await Client.GetAsync("/api/online-voting/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var logout = await Client.PostAsync("/api/online-voting/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var clearedTokenHeader = GetSetCookieHeader(logout, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.False(string.IsNullOrWhiteSpace(clearedTokenHeader));
        Assert.StartsWith($"{SecureCookieMiddleware.VoterTokenCookieName}=", clearedTokenHeader, StringComparison.OrdinalIgnoreCase);
        var clearedToken = GetSetCookieValue(logout, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.True(string.IsNullOrEmpty(clearedToken));
        Assert.Contains("max-age=0", clearedTokenHeader, StringComparison.OrdinalIgnoreCase);

        SetVoterCookie(clearedToken ?? string.Empty);
        var after = await Client.GetAsync("/api/online-voting/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task SubmitBallot_WhenElectionFinalized_AndOnlineWindowOpen_Returns400WithVoterPhraseKey()
    {
        var email = $"finalized_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email);
        await EnsureOnlineVoterAsync(email, "E");
        await SetElectionStageAsync(electionGuid, ElectionStage.Finalized);

        var response = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes =
                [
                    new OnlineVoteDto
                    {
                        VoteName = "Someone",
                        PositionOnBallot = 1
                    }
                ]
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(ElectionStageMessageKeys.FinalizedOnlineSubmit, body);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        Assert.Equal(0, await context.OnlineVotingInfos.CountAsync(ovi => ovi.ElectionGuid == electionGuid));
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

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var votingInfo = await context.OnlineVotingInfos
                .FirstAsync(ovi => ovi.ElectionGuid == electionGuid);
            Assert.Equal("Submitted", votingInfo.Status);
            Assert.Null(votingInfo.BallotGuid);
            Assert.False(string.IsNullOrWhiteSpace(votingInfo.ListPool));
            Assert.Equal(0, await context.Ballots.CountAsync(b => b.Location.ElectionGuid == electionGuid));
        }

        var status = await Client.GetFromJsonAsync<OnlineVoteStatusDto>(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        Assert.NotNull(status);
        Assert.True(status.HasVoted);
        Assert.True(status.CanChangeVote);
        Assert.Equal(9, status.PriorVotes.Count);
        Assert.All(status.PriorVotes, v => Assert.Contains("Free Voter", v.VoteName ?? ""));
    }

    [Fact]
    public async Task SubmitBallot_BothModeC_WithPoolAndNineVotes_StoresListPool()
    {
        var email = $"both_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email, selectionProcess: "C");
        await EnsureOnlineVoterAsync(email, "E");

        var people = await SetupPeopleAsync(electionGuid, 9);
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
                PersonGuid = people[i],
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
            Assert.NotNull(votingInfo);
            Assert.Equal("Submitted", votingInfo.Status);
            Assert.Null(votingInfo.BallotGuid);
            Assert.Equal(0, await context.Votes.CountAsync(v => v.Ballot.Location.ElectionGuid == electionGuid));
        }

        var statusResponse = await Client.GetAsync(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        var status = await statusResponse.Content.ReadFromJsonAsync<OnlineVoteStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.CanChangeVote);
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
        var people = await SetupPeopleAsync(electionGuid, 2);

        async Task<DateTimeOffset?> SubmitAndGetTimestamp(int voteCount)
        {
            var dto = new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes = people.Take(voteCount).Select((c, i) => new OnlineVoteDto
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
        var phone = "+16478971234";
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

        var people = await SetupPeopleAsync(electionGuid, 3);
        var submitDto = new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            NotifyWhenProcessed = true,
            Votes = people.Select((c, i) => new OnlineVoteDto
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
            Votes = people.Take(2).Select((c, i) => new OnlineVoteDto
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

    [Fact]
    public async Task SubmitBallot_EmptyVotes_OverwritesDraftAndSubmittedThroughValidation()
    {
        var email = $"empty_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SetupOpenElectionWithVoter(email);
        await EnsureOnlineVoterAsync(email, "E");

        var draft = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                IsDraft = true,
                Votes =
                [
                    new OnlineVoteDto { VoteName = "Draft Name", PositionOnBallot = 1 }
                ]
            });
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);

        var clearDraft = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                IsDraft = true,
                Votes = []
            });
        Assert.Equal(HttpStatusCode.OK, clearDraft.StatusCode);

        var draftStatus = await Client.GetFromJsonAsync<OnlineVoteStatusDto>(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        Assert.NotNull(draftStatus);
        Assert.Empty(draftStatus.PriorVotes ?? []);
        Assert.Null(draftStatus.WhenSubmitted);

        var submit = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                IsDraft = false,
                Votes =
                [
                    new OnlineVoteDto { VoteName = "Submitted Name", PositionOnBallot = 1 }
                ]
            });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var clearSubmitted = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                IsDraft = false,
                Votes = []
            });
        Assert.Equal(HttpStatusCode.OK, clearSubmitted.StatusCode);

        var submittedStatus = await Client.GetFromJsonAsync<OnlineVoteStatusDto>(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");
        Assert.NotNull(submittedStatus);
        Assert.Empty(submittedStatus.PriorVotes ?? []);
        Assert.NotNull(submittedStatus.WhenSubmitted);
    }

    [Fact]
    public async Task AcceptAll_WithoutTellerAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"/api/elections/{Guid.NewGuid()}/online-ballots/accept-all",
            null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitOnline_ThenTellerAcceptAll_UpdatesCounts_WipesPendingPayload_AndCreatesOlBallotForTally()
    {
        // HTTP path: voter submit stays pending; teller Accept-all creates the
        // regular OL ballot (relational ExecuteUpdate claim) and wipes ListPool.
        var voterEmail = $"zelda_{Guid.NewGuid():N}@example.com";
        const string voterFirst = "ZeldaQuorum";
        const string voterLast = "Nightingale";
        const string poolMarker = "SecretPoolAlpha";

        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createResponse = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = "Accept-all flow election",
            DateOfElection = DateTime.UtcNow.AddDays(7),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 2,
            UseOnlineVoting = true,
            OnlineSelectionProcess = "A"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = created!.Data!.ElectionGuid;

        var windowResponse = await PutJsonAsync(
            $"/api/elections/{electionGuid}/online-voting-window",
            new UpdateOnlineVotingWindowDto
            {
                OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
                OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(2),
                OnlineCloseIsEstimate = true
            });
        Assert.Equal(HttpStatusCode.OK, windowResponse.StatusCode);

        var candidates = await SeedVoterAndCandidatesAsync(
            electionGuid, voterEmail, voterFirst, voterLast);

        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = voterEmail,
                ListPool = [new OnlinePoolEntryDto { FullName = poolMarker }],
                Votes = candidates.Select((personGuid, i) => new OnlineVoteDto
                {
                    PersonGuid = personGuid,
                    PositionOnBallot = i + 1
                }).ToList()
            });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var pending = await context.OnlineVotingInfos
                .SingleAsync(o => o.ElectionGuid == electionGuid);
            Assert.Equal(OnlineBallotStatus.Submitted, pending.Status);
            Assert.Null(pending.BallotGuid);
            Assert.Contains(poolMarker, pending.ListPool);
            Assert.Equal(0, await context.Ballots.CountAsync(b => b.Location.ElectionGuid == electionGuid));
        }

        var summaryBeforeResponse = await GetAsync(
            $"/api/elections/{electionGuid}/online-ballots/accept-all-summary");
        Assert.Equal(HttpStatusCode.OK, summaryBeforeResponse.StatusCode);
        var summaryBefore = await DeserializeResponseAsync<AcceptAllOnlineBallotsSummaryDto>(
            summaryBeforeResponse);
        Assert.NotNull(summaryBefore);
        Assert.Equal(1, summaryBefore.PendingCount);
        Assert.Equal(0, summaryBefore.ProcessedCount);
        await AssertAnonymousCountPayloadAsync(
            await GetAsync($"/api/elections/{electionGuid}/online-ballots/accept-all-summary"),
            voterEmail, voterFirst, voterLast, poolMarker);

        var monitorBeforeResponse = await GetAsync(
            $"/api/results/election/{electionGuid}/monitor");
        Assert.Equal(HttpStatusCode.OK, monitorBeforeResponse.StatusCode);
        var monitorBefore = await DeserializeResponseAsync<MonitorInfoDto>(monitorBeforeResponse);
        Assert.NotNull(monitorBefore);
        Assert.Equal(1, monitorBefore.OnlineVotingInfo.PendingOnlineBallots);
        Assert.Equal(1, monitorBefore.OnlineVotingInfo.SubmittedOnlineBallots);
        Assert.Equal(0, monitorBefore.OnlineVotingInfo.ProcessingOnlineBallots);
        Assert.Equal(0, monitorBefore.OnlineVotingInfo.ProcessedOnlineBallots);
        Assert.Equal(0, monitorBefore.TotalBallots);
        await AssertAnonymousCountPayloadAsync(
            await GetAsync($"/api/results/election/{electionGuid}/monitor"),
            voterEmail, voterFirst, voterLast, poolMarker);

        var acceptResponse = await Client.PostAsync(
            $"/api/elections/{electionGuid}/online-ballots/accept-all",
            null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var accept = await DeserializeResponseAsync<AcceptAllOnlineBallotsResultDto>(acceptResponse);
        Assert.NotNull(accept);
        Assert.True(accept.Success);
        Assert.Equal(1, accept.AcceptedCount);
        Assert.Equal(0, accept.PendingRemaining);
        Assert.Equal("monitoring.acceptAll.complete", accept.MessageKey);

        var summaryAfter = await DeserializeResponseAsync<AcceptAllOnlineBallotsSummaryDto>(
            await GetAsync($"/api/elections/{electionGuid}/online-ballots/accept-all-summary"));
        Assert.NotNull(summaryAfter);
        Assert.Equal(0, summaryAfter.PendingCount);
        Assert.Equal(1, summaryAfter.ProcessedCount);

        var monitorAfter = await DeserializeResponseAsync<MonitorInfoDto>(
            await GetAsync($"/api/results/election/{electionGuid}/monitor"));
        Assert.NotNull(monitorAfter);
        Assert.Equal(0, monitorAfter.OnlineVotingInfo.PendingOnlineBallots);
        Assert.Equal(0, monitorAfter.OnlineVotingInfo.SubmittedOnlineBallots);
        Assert.Equal(0, monitorAfter.OnlineVotingInfo.ProcessingOnlineBallots);
        Assert.Equal(1, monitorAfter.OnlineVotingInfo.ProcessedOnlineBallots);
        Assert.Equal(1, monitorAfter.TotalBallots);
        Assert.Equal(2, monitorAfter.TotalVotes);
        Assert.Contains(monitorAfter.Locations, l => l.BallotCount == 1);
        Assert.DoesNotContain(
            monitorAfter.OnlineVotingInfo.AcceptAllRuns,
            run => (run.AcceptedBy ?? "").Contains(voterEmail, StringComparison.OrdinalIgnoreCase));
        await AssertAnonymousCountPayloadAsync(
            await Client.GetAsync($"/api/results/election/{electionGuid}/monitor"),
            voterEmail, voterFirst, voterLast, poolMarker);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var processed = await context.OnlineVotingInfos
                .SingleAsync(o => o.ElectionGuid == electionGuid);
            Assert.Equal(OnlineBallotStatus.Processed, processed.Status);
            Assert.Null(processed.ListPool);
            Assert.Null(processed.PoolLocked);
            Assert.Null(processed.BallotGuid);
            Assert.Contains(OnlineBallotStatus.Processed, processed.HistoryStatus);

            var location = await context.Locations.SingleAsync(l =>
                l.ElectionGuid == electionGuid
                && l.LocationTypeCode == nameof(LocationType.Online));
            var ballot = await context.Ballots.SingleAsync(b => b.LocationGuid == location.LocationGuid);
            Assert.Equal(ComputerCodeHelper.Online, ballot.ComputerCode);
            Assert.Equal($"{ComputerCodeHelper.Online}1", ballot.BallotCode);
            Assert.Equal(2, await context.Votes.CountAsync(v => v.BallotGuid == ballot.BallotGuid));
        }

        var ballotsResponse = await GetAsync($"/api/ballots/{electionGuid}/ballots");
        Assert.Equal(HttpStatusCode.OK, ballotsResponse.StatusCode);
        var ballots = await DeserializeResponseAsync<PaginatedResponse<BallotDto>>(ballotsResponse);
        var olBallot = Assert.Single(ballots!.Items);
        Assert.Equal(ComputerCodeHelper.Online, olBallot.ComputerCode);
        Assert.Equal($"{ComputerCodeHelper.Online}1", olBallot.BallotCode);
        Assert.Equal(2, olBallot.VoteCount);

        var onlineReportResponse = await GetAsync($"/api/reports/{electionGuid}/BallotsOnline");
        Assert.Equal(HttpStatusCode.OK, onlineReportResponse.StatusCode);
        var onlineReport = await DeserializeResponseAsync<BallotsReportDto>(onlineReportResponse);
        var reported = Assert.Single(onlineReport!.Ballots);
        Assert.True(reported.IsOnline);

        var tallyResponse = await Client.PostAsync(
            $"/api/results/election/{electionGuid}/calculate",
            null);
        Assert.Equal(HttpStatusCode.OK, tallyResponse.StatusCode);
        var tally = await DeserializeResponseAsync<TallyResultDto>(tallyResponse);
        Assert.NotNull(tally);
        Assert.Equal(1, tally.Statistics.BallotsReceived);
        Assert.Equal(2, tally.Statistics.TotalVotes);
        Assert.Contains(tally.Results, r => r.PersonGuid == candidates[0] && r.VoteCount >= 1);
        Assert.Contains(tally.Results, r => r.PersonGuid == candidates[1] && r.VoteCount >= 1);

        var voteStatus = await Client.GetFromJsonAsync<OnlineVoteStatusDto>(
            $"/api/online-voting/{electionGuid}/{voterEmail}/voteStatus");
        Assert.NotNull(voteStatus);
        Assert.True(voteStatus.HasVoted);
        Assert.False(voteStatus.CanChangeVote);
        Assert.Empty(voteStatus.PriorVotes);
        Assert.Empty(voteStatus.ListPool);
    }

    private static async Task AssertAnonymousCountPayloadAsync(
        HttpResponseMessage response,
        string voterEmail,
        string voterFirst,
        string voterLast,
        string poolMarker)
    {
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(voterEmail, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(voterFirst, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(voterLast, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(poolMarker, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("personName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("whenStatus", json, StringComparison.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        AssertNoIdentityArrays(doc.RootElement);
    }

    private static void AssertNoIdentityArrays(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("acceptAllRuns") || property.NameEquals("locations")
                    || property.NameEquals("computers"))
                {
                    continue;
                }

                Assert.False(
                    property.Name.Contains("pending", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Array,
                    "Monitor/summary must not return a pending row list.");
                Assert.False(
                    property.Name.Contains("accepted", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Array
                    && property.Name != "acceptAllRuns",
                    "Monitor/summary must not return an accepted row list.");
                AssertNoIdentityArrays(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AssertNoIdentityArrays(item);
            }
        }
    }

    private async Task<List<Guid>> SeedVoterAndCandidatesAsync(
        Guid electionGuid,
        string voterEmail,
        string voterFirst,
        string voterLast)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = voterFirst,
            LastName = voterLast,
            Email = voterEmail,
            CanVote = true,
            RowVersion = new byte[8]
        });

        var candidates = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            var guid = Guid.NewGuid();
            candidates.Add(guid);
            context.People.Add(new Person
            {
                ElectionGuid = electionGuid,
                PersonGuid = guid,
                FirstName = $"Candidate{i}",
                LastName = "Eligible",
                CanReceiveVotes = true,
                CanVote = true,
                RowVersion = new byte[8]
            });
        }

        if (!await context.OnlineVoters.AnyAsync(ov => ov.VoterId == voterEmail))
        {
            context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = voterEmail,
                VoterIdType = "E",
                WhenRegistered = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return candidates;
    }

    private async Task SetKioskVerifyCodeDateAsync(string kioskCode, DateTimeOffset verifyCodeDate)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var onlineVoter = await context.OnlineVoters.SingleAsync(ov => ov.VoterId == kioskCode);
        onlineVoter.VerifyCodeDate = verifyCodeDate;
        await context.SaveChangesAsync();
    }

    private async Task SetElectionVotingMethodsAsync(Guid electionGuid, string votingMethods)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var election = await context.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
        election.VotingMethods = votingMethods;
        await context.SaveChangesAsync();
    }

    private async Task<Guid> GetPersonGuidByKioskCodeAsync(string kioskCode)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await context.People
            .Where(p => p.KioskCode == kioskCode)
            .Select(p => p.PersonGuid)
            .SingleAsync();
    }

    private async Task SetElectionStageAsync(Guid electionGuid, ElectionStage stage)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var election = await context.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
        election.ElectionStage = stage;
        await context.SaveChangesAsync();
    }

    private async Task ReplaceKioskOnlineVoterAsAsync(string voterId, string voterIdType)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await context.OnlineVoters.SingleAsync(ov => ov.VoterId == voterId);
        row.VoterIdType = voterIdType;
        await context.SaveChangesAsync();
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

    private async Task<List<Guid>> SetupPeopleAsync(Guid electionGuid, int count)
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
                FirstName = $"Person{i}",
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
            UseOnlineVoting = true,
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

        if (!string.IsNullOrWhiteSpace(kioskCode))
        {
            context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = kioskCode,
                VoterIdType = KioskCodeLifetime.VoterIdType,
                WhenRegistered = DateTimeOffset.UtcNow,
                VerifyCode = kioskCode,
                VerifyCodeDate = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return electionGuid;
    }
}