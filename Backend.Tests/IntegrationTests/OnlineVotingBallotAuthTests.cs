using System.Net;
using System.Net.Http.Json;
using Backend.Context;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Voter ballot write/read requires the httpOnly voter session. The client
/// cannot choose another person's ballot or another election's list.
/// </summary>
public class OnlineVotingBallotAuthTests : IntegrationTestBase
{
    public OnlineVotingBallotAuthTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Anonymous_SubmitBallot_Returns401_AndWritesNothing()
    {
        var email = $"anon_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SeedListedVoterAsync(email);

        var response = await Client.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = email,
                Votes = [new OnlineVoteDto { VoteName = "Sneaky", PositionOnBallot = 1 }]
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Sneaky", body, StringComparison.Ordinal);
        Assert.DoesNotContain(email, body, StringComparison.OrdinalIgnoreCase);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        Assert.False(await context.OnlineVotingInfos.AnyAsync(o => o.ElectionGuid == electionGuid));
    }

    [Fact]
    public async Task Anonymous_VoteStatus_Returns401_AndDoesNotReturnBallot()
    {
        var email = $"anonstatus_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SeedListedVoterAsync(email);
        const string secret = "PrivateChoice";
        var submit = await SubmitBallotAsVoterAsync(electionGuid, Ballot(electionGuid, email, secret));
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var response = await Client.GetAsync(
            $"/api/online-voting/{electionGuid}/{email}/voteStatus");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
        Assert.DoesNotContain(email, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("priorVotes", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VoterA_CannotSubmitOrRead_VoterB()
    {
        var electionGuid = await SeedOpenElectionAsync();
        var emailA = $"a_{Guid.NewGuid():N}@example.com";
        var emailB = $"b_{Guid.NewGuid():N}@example.com";
        await AddListedVoterAsync(electionGuid, emailA);
        await AddListedVoterAsync(electionGuid, emailB);

        const string secret = "BeeOnly";
        var seeded = await SubmitBallotAsVoterAsync(electionGuid, Ballot(electionGuid, emailB, secret));
        Assert.Equal(HttpStatusCode.OK, seeded.StatusCode);

        var submit = await SubmitBallotAsVoterAsync(
            electionGuid,
            Ballot(electionGuid, emailB, "Stolen"),
            sessionVoterId: emailA);
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
        await AssertGenericForbiddenAsync(submit, emailA, emailB, "Stolen", secret);

        var status = await GetVoteStatusAsVoterAsync(
            electionGuid,
            emailB,
            sessionVoterId: emailA);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        await AssertGenericForbiddenAsync(status, emailA, emailB, secret);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var rows = await context.OnlineVotingInfos
            .Where(o => o.ElectionGuid == electionGuid)
            .ToListAsync();
        var only = Assert.Single(rows);
        Assert.Contains(secret, only.ListPool);
        Assert.DoesNotContain("Stolen", only.ListPool);
    }

    [Fact]
    public async Task EmailSession_ListedOnlyOnElectionX_CannotSubmitOrReadElectionY()
    {
        var email = $"onlyx_{Guid.NewGuid():N}@example.com";
        var electionX = await SeedListedVoterAsync(email);
        var electionY = await SeedOpenElectionAsync();
        await AddListedVoterAsync(electionY, $"other_{Guid.NewGuid():N}@example.com");

        var submit = await SubmitBallotAsVoterAsync(
            electionY,
            Ballot(electionY, email, "Cross"));
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
        await AssertGenericForbiddenAsync(submit, email, "Cross");

        var status = await GetVoteStatusAsVoterAsync(electionY, email);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        await AssertGenericForbiddenAsync(status, email);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        Assert.False(await context.OnlineVotingInfos.AnyAsync(o => o.ElectionGuid == electionY));
        Assert.False(await context.OnlineVotingInfos.AnyAsync(o => o.ElectionGuid == electionX));
    }

    [Fact]
    public async Task SameEmail_ListedOnTwoElections_OneSessionSubmitsOnBoth()
    {
        var email = $"both_{Guid.NewGuid():N}@example.com";
        var electionX = await SeedListedVoterAsync(email);
        var electionY = await SeedOpenElectionAsync();
        await AddListedVoterAsync(electionY, email);

        var submitX = await SubmitBallotAsVoterAsync(electionX, Ballot(electionX, email, "Choice X"));
        var submitY = await SubmitBallotAsVoterAsync(electionY, Ballot(electionY, email, "Choice Y"));
        Assert.Equal(HttpStatusCode.OK, submitX.StatusCode);
        Assert.Equal(HttpStatusCode.OK, submitY.StatusCode);

        var statusX = await ReadStatusAsync(electionX, email);
        var statusY = await ReadStatusAsync(electionY, email);
        Assert.Contains(statusX.PriorVotes, v => v.VoteName == "Choice X");
        Assert.Contains(statusY.PriorVotes, v => v.VoteName == "Choice Y");
        Assert.DoesNotContain(statusX.PriorVotes, v => v.VoteName == "Choice Y");
        Assert.DoesNotContain(statusY.PriorVotes, v => v.VoteName == "Choice X");
    }

    [Fact]
    public async Task AuthenticatedVoter_SubmitAndStatus_ReturnsOwnPendingBallot()
    {
        var email = $"happy_{Guid.NewGuid():N}@example.com";
        var electionGuid = await SeedListedVoterAsync(email);

        var submit = await SubmitBallotAsVoterAsync(
            electionGuid,
            Ballot(electionGuid, email, "My Choice"));
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var status = await ReadStatusAsync(electionGuid, email);
        Assert.True(status.HasVoted);
        Assert.True(status.CanChangeVote);
        Assert.Contains(status.PriorVotes, v => v.VoteName == "My Choice");

        var changed = await SubmitBallotAsVoterAsync(
            electionGuid,
            Ballot(electionGuid, email, "Changed"));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var after = await ReadStatusAsync(electionGuid, email);
        Assert.Contains(after.PriorVotes, v => v.VoteName == "Changed");
        Assert.DoesNotContain(after.PriorVotes, v => v.VoteName == "My Choice");
    }

    [Fact]
    public async Task KioskSession_FromVerifyCodeCookie_CanSubmitAndReadOwnStatus()
    {
        var kioskCode = "JAUTH";
        var electionGuid = await SeedListedVoterAsync(kioskCode: kioskCode);

        var auth = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = kioskCode,
            VerifyCode = kioskCode
        });
        Assert.Equal(HttpStatusCode.OK, auth.StatusCode);
        var token = GetSetCookieValue(auth, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.False(string.IsNullOrWhiteSpace(token));
        var session = await auth.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.Equal(KioskCodeLifetime.ToVoterId(electionGuid, kioskCode), session!.VoterId);

        using var voter = Factory.CreateClient();
        voter.DefaultRequestHeaders.Add(
            "Cookie",
            $"{SecureCookieMiddleware.VoterTokenCookieName}={token}");

        var submit = await voter.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = session.VoterId,
                Votes = [new OnlineVoteDto { VoteName = "Kiosk Choice", PositionOnBallot = 1 }]
            });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var statusResponse = await voter.GetAsync(
            $"/api/online-voting/{electionGuid}/{session.VoterId}/voteStatus");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<OnlineVoteStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.CanChangeVote);
        Assert.Contains(status.PriorVotes, v => v.VoteName == "Kiosk Choice");

        using var stranger = Factory.CreateClient();
        var anonymous = await stranger.PostAsJsonAsync(
            $"/api/online-voting/{electionGuid}/submitBallot",
            new SubmitOnlineBallotDto
            {
                ElectionGuid = electionGuid,
                VoterId = session.VoterId,
                Votes = [new OnlineVoteDto { VoteName = "No Cookie", PositionOnBallot = 1 }]
            });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    private static SubmitOnlineBallotDto Ballot(Guid electionGuid, string voterId, string voteName)
    {
        return new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = voterId,
            Votes = [new OnlineVoteDto { VoteName = voteName, PositionOnBallot = 1 }]
        };
    }

    private async Task<OnlineVoteStatusDto> ReadStatusAsync(Guid electionGuid, string voterId)
    {
        var response = await GetVoteStatusAsVoterAsync(electionGuid, voterId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<OnlineVoteStatusDto>();
        Assert.NotNull(status);
        return status;
    }

    private static async Task AssertGenericForbiddenAsync(
        HttpResponseMessage response,
        params string[] absent)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("voterNotFound", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("electionNotFound", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("priorVotes", body, StringComparison.OrdinalIgnoreCase);
        foreach (var value in absent)
        {
            Assert.DoesNotContain(value, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<Guid> SeedListedVoterAsync(string? email = null, string? kioskCode = null)
    {
        var electionGuid = await SeedOpenElectionAsync();
        if (email != null)
        {
            await AddListedVoterAsync(electionGuid, email);
        }

        if (kioskCode != null)
        {
            await AddKioskVoterAsync(electionGuid, kioskCode);
        }

        return electionGuid;
    }

    private async Task<Guid> SeedOpenElectionAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var electionGuid = Guid.NewGuid();
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Ballot auth election",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            NumberToElect = 9,
            OnlineSelectionProcess = "A",
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        await context.SaveChangesAsync();
        return electionGuid;
    }

    private async Task AddListedVoterAsync(Guid electionGuid, string email)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Listed",
            LastName = "Voter",
            Email = email,
            CanVote = true,
            RowVersion = new byte[8]
        });
        if (!await context.OnlineVoters.AnyAsync(ov => ov.VoterId == email))
        {
            context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = email,
                VoterIdType = "E",
                WhenRegistered = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task AddKioskVoterAsync(Guid electionGuid, string kioskCode)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Kiosk",
            LastName = "Voter",
            KioskCode = kioskCode,
            CanVote = true,
            RowVersion = new byte[8]
        });
        context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = KioskCodeLifetime.ToVoterId(electionGuid, kioskCode),
            VoterIdType = KioskCodeLifetime.VoterIdType,
            WhenRegistered = DateTimeOffset.UtcNow,
            VerifyCode = kioskCode,
            VerifyCodeDate = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
