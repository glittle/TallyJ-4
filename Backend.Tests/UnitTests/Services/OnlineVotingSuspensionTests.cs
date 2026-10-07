using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// A flagged election cannot open online voting. Teller ballot entry still works.
/// </summary>
public class OnlineVotingSuspensionTests : ServiceTestBase
{
    private readonly Guid _electionId = Guid.NewGuid();

    public OnlineVotingSuspensionTests()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Flagged",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(2),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        Context.ElectionSendControls.Add(new ElectionSendControl
        {
            ElectionGuid = _electionId,
            Flagged = true,
            FlaggedAt = DateTimeOffset.UtcNow
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task TurningOnlineVotingOn_IsRefused()
    {
        var election = Context.Elections.Single();
        election.UseOnlineVoting = false;
        await Context.SaveChangesAsync();

        var service = CreateElectionService();
        await Assert.ThrowsAsync<OnlineVotingSuspendedException>(() =>
            service.UpdateElectionAsync(_electionId, new UpdateElectionDto { UseOnlineVoting = true }));
    }

    [Fact]
    public async Task OpeningTheWindow_IsRefused_ClosingIt_IsAllowed()
    {
        var service = CreateElectionService();
        await Assert.ThrowsAsync<OnlineVotingSuspendedException>(() =>
            service.UpdateOnlineVotingWindowAsync(_electionId, new UpdateOnlineVotingWindowDto
            {
                OnlineWhenOpen = DateTimeOffset.UtcNow.AddMinutes(-5),
                OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(3)
            }));

        var closed = await service.UpdateOnlineVotingWindowAsync(_electionId, new UpdateOnlineVotingWindowDto
        {
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-3),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        Assert.NotNull(closed);
    }

    [Fact]
    public async Task Submit_IsRefused_TellerBallot_IsKept()
    {
        var personId = Guid.NewGuid();
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = personId,
            FirstName = "Ada",
            LastName = "Voter",
            Email = "ada@example.com",
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = "ada@example.com",
            VoterIdType = "E"
        });
        var locationId = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            ElectionGuid = _electionId,
            LocationGuid = locationId,
            Name = "Hall",
            LocationTypeCode = "Manual"
        });
        await Context.SaveChangesAsync();

        var voting = CreateVotingService();
        var submit = await voting.SubmitBallotAsync(new SubmitOnlineBallotDto
        {
            ElectionGuid = _electionId,
            VoterId = "ada@example.com",
            Votes = new List<OnlineVoteDto>()
        });
        Assert.False(submit.Success);
        Assert.Equal("voting.submit.notOpen", submit.Error);

        var ballots = new BallotService(Context, Mock.Of<ILogger<BallotService>>());
        var ballot = await ballots.CreateBallotAsync(new CreateBallotDto
        {
            ElectionGuid = _electionId,
            LocationGuid = locationId,
            ComputerCode = "A"
        });
        Assert.NotEqual(Guid.Empty, ballot.BallotGuid);
        Assert.Single(Context.Ballots);
    }

    [Fact]
    public async Task VerifyCode_IsRefused_WhenEveryOpenElectionIsFlagged()
    {
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Email = "ada@example.com",
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = "ada@example.com",
            VoterIdType = "E",
            VerifyCode = "123456",
            VerifyCodeDate = DateTimeOffset.UtcNow,
            VerifyAttempts = 0
        });
        await Context.SaveChangesAsync();

        var result = await CreateVotingService().VerifyCodeAsync(new VerifyCodeDto
        {
            VoterId = "ada@example.com",
            VerifyCode = "123456"
        });

        Assert.False(result.Success);
        Assert.Equal("voting.auth.noOpenElections", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task VerifyCode_IgnoresAPhoneMatch_WhenTheVoterIdIsAnEmail()
    {
        var openId = AddUnflaggedOpenElection();
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Email = "ada@example.com",
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.People.Add(new Person
        {
            ElectionGuid = openId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Phone",
            LastName = "Only",
            Phone = "ada@example.com",
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = "ada@example.com",
            VoterIdType = "E",
            VerifyCode = "123456",
            VerifyCodeDate = DateTimeOffset.UtcNow,
            VerifyAttempts = 0
        });
        await Context.SaveChangesAsync();

        var result = await CreateVotingService().VerifyCodeAsync(new VerifyCodeDto
        {
            VoterId = "ada@example.com",
            VerifyCode = "123456"
        });

        Assert.False(result.Success);
        Assert.Equal("voting.auth.noOpenElections", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task VerifyCode_IgnoresAnEmailMatch_WhenTheVoterIdIsAPhone()
    {
        const string phone = "+14165550100";
        var openId = AddUnflaggedOpenElection();
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Phone = phone,
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.People.Add(new Person
        {
            ElectionGuid = openId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Email",
            LastName = "Only",
            Email = phone,
            CanVote = true,
            RowVersion = new byte[8]
        });
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            VerifyCode = "123456",
            VerifyCodeDate = DateTimeOffset.UtcNow,
            VerifyAttempts = 0
        });
        await Context.SaveChangesAsync();

        var result = await CreateVotingService().VerifyCodeAsync(new VerifyCodeDto
        {
            VoterId = phone,
            VerifyCode = "123456"
        });

        Assert.False(result.Success);
        Assert.Equal("voting.auth.noOpenElections", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task VerifyCode_DoesNotTreatAMissingOpenTime_AsOpen()
    {
        var unsetId = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = unsetId,
            Name = "No open time",
            UseOnlineVoting = true,
            OnlineWhenOpen = null,
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        Context.People.AddRange(
            new Person
            {
                ElectionGuid = _electionId,
                PersonGuid = Guid.NewGuid(),
                FirstName = "Ada",
                LastName = "Voter",
                Email = "ada@example.com",
                CanVote = true,
                RowVersion = new byte[8]
            },
            new Person
            {
                ElectionGuid = unsetId,
                PersonGuid = Guid.NewGuid(),
                FirstName = "Ada",
                LastName = "Other",
                Email = "ada@example.com",
                CanVote = true,
                RowVersion = new byte[8]
            });
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = "ada@example.com",
            VoterIdType = "E",
            VerifyCode = "123456",
            VerifyCodeDate = DateTimeOffset.UtcNow,
            VerifyAttempts = 0
        });
        await Context.SaveChangesAsync();

        var result = await CreateVotingService().VerifyCodeAsync(new VerifyCodeDto
        {
            VoterId = "ada@example.com",
            VerifyCode = "123456"
        });

        Assert.False(result.Success);
        Assert.Equal("voting.auth.noOpenElections", result.Error);
    }

    private Guid AddUnflaggedOpenElection()
    {
        var openId = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = openId,
            Name = "Open",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(2),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        return openId;
    }

    private ElectionService CreateElectionService()
    {
        var accessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        return new ElectionService(
            Context,
            Mock.Of<ILogger<ElectionService>>(),
            Mock.Of<ISignalRNotificationService>(),
            accessor.Object);
    }

    private OnlineVotingService CreateVotingService()
    {
        var host = new Mock<IHostEnvironment>();
        host.Setup(item => item.EnvironmentName).Returns("Testing");
        return new OnlineVotingService(
            Context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            host.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<IEmailSender>(),
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            Mock.Of<IOnlineBallotAcceptLock>());
    }
}
