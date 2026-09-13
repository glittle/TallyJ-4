using Backend.DTOs.FrontDesk;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests;

/// <summary>
/// #336: after a desk method withdraws pending online, later Unregister /
/// re-check-in / Accept-all must not revive that row. Processing / Processed
/// stay locked. Registration-done is the existing Finalized write lock.
/// </summary>
public class Issue336FrontDeskPendingOnlineTests : ServiceTestBase
{
    private readonly FrontDeskService _frontDesk;
    private readonly OnlineVotingService _online;
    private readonly Guid _electionGuid = Guid.NewGuid();

    public Issue336FrontDeskPendingOnlineTests()
    {
        _frontDesk = new FrontDeskService(
            Context,
            new Mock<ILogger<FrontDeskService>>().Object,
            new Mock<ISignalRNotificationService>().Object);

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(e => e.EnvironmentName).Returns("Testing");
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>()))
            .Returns(Task.CompletedTask);

        _online = new OnlineVotingService(
            Context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            hostEnvironment.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            emailSender.Object,
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            new OnlineBallotAcceptLock());
    }

    [Fact]
    public async Task Submitted_ThenInPerson_ThenUnregister_ThenMailed_OnlineRowStaysGone()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Submitted);

        await CheckInAsync(person, VotingMethodCodes.InPerson);
        Assert.Empty(Context.OnlineVotingInfos);
        Assert.False(Context.People.Single().HasOnlineBallot);

        await UnregisterAsync(person);
        Assert.Empty(Context.OnlineVotingInfos);
        Assert.False(Context.People.Single().HasOnlineBallot);
        Assert.Null(Context.People.Single().VotingMethod);
        Assert.Null(Context.People.Single().RegistrationTime);

        var second = await CheckInAsync(person, VotingMethodCodes.Mailed);
        Assert.Equal(VotingMethodCodes.Mailed, second.VotingMethod);
        Assert.Null(second.OnlineBallotStatus);
        Assert.Empty(Context.OnlineVotingInfos);
        Assert.False(Context.People.Single().HasOnlineBallot);
    }

    [Fact]
    public async Task WithdrawnThenSecondMethod_AcceptAllSkipsAndCreatesNoBallot()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Submitted);

        await CheckInAsync(person, VotingMethodCodes.InPerson);
        await UnregisterAsync(person);
        await CheckInAsync(person, VotingMethodCodes.DroppedOff);

        var summary = await _online.GetAcceptAllSummaryAsync(_electionGuid);
        Assert.Equal(0, summary!.PendingCount);

        var accept = await _online.AcceptAllPendingAsync(_electionGuid);
        Assert.True(accept.Success);
        Assert.Equal(0, accept.AcceptedCount);
        Assert.Equal(0, Context.Ballots.Count());
        Assert.Empty(Context.OnlineVotingInfos);
        Assert.False(Context.People.Single().HasOnlineBallot);
    }

    [Fact]
    public async Task WithdrawnThenUnregister_AcceptAllSkipsWhileUnregistered()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Submitted);

        await CheckInAsync(person, VotingMethodCodes.InPerson);
        await UnregisterAsync(person);

        var accept = await _online.AcceptAllPendingAsync(_electionGuid);
        Assert.True(accept.Success);
        Assert.Equal(0, accept.AcceptedCount);
        Assert.Equal(0, Context.Ballots.Count());
        Assert.Empty(Context.OnlineVotingInfos);
    }

    [Fact]
    public async Task ProcessingOnline_ThenDeskMethod_IsStillRefused()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Processing);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInAsync(person, VotingMethodCodes.InPerson));

        Assert.Equal(FrontDeskMessageKeys.AlreadyProcessingOnline, ex.Message);
        Assert.Null(Context.People.Single().RegistrationTime);
        Assert.Null(Context.People.Single().VotingMethod);
        Assert.Single(Context.OnlineVotingInfos);
    }

    [Fact]
    public async Task ProcessedOnline_ThenDeskMethod_IsStillRefused()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Processed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInAsync(person, VotingMethodCodes.Mailed));

        Assert.Equal(FrontDeskMessageKeys.AlreadyAcceptedOnline, ex.Message);
        Assert.Null(Context.People.Single().RegistrationTime);
        Assert.Null(Context.People.Single().VotingMethod);
        Assert.Single(Context.OnlineVotingInfos);
    }

    [Fact]
    public async Task ProcessedOnline_UnregisterWithoutRegistrationTime_IsRefused()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Processed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UnregisterAsync(person));

        Assert.Equal("Person is not currently checked in", ex.Message);
        Assert.Equal(OnlineBallotStatus.Processed, Context.OnlineVotingInfos.Single().Status);
    }

    [Fact]
    public async Task Finalized_RefusesUnregisterAndFurtherCheckIn()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        AddOnlineRow(person, OnlineBallotStatus.Draft);
        await CheckInAsync(person, VotingMethodCodes.InPerson);

        Context.Elections.Single().ElectionStage = ElectionStage.Finalized;
        await Context.SaveChangesAsync();

        var unregister = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UnregisterAsync(person));
        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, unregister.Message);
        Assert.Equal(VotingMethodCodes.InPerson, Context.People.Single().VotingMethod);

        Context.People.Single().RegistrationTime = null;
        Context.People.Single().VotingMethod = null;
        await Context.SaveChangesAsync();

        var checkIn = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInAsync(person, VotingMethodCodes.Mailed));
        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, checkIn.Message);
        Assert.Empty(Context.OnlineVotingInfos);
    }

    private Task<FrontDeskVoterDto> CheckInAsync(Person person, string method) =>
        _frontDesk.CheckInVoterAsync(_electionGuid, new CheckInVoterDto
        {
            PersonGuid = person.PersonGuid,
            VotingMethod = method,
            Teller1 = "Ada"
        });

    private Task<FrontDeskVoterDto> UnregisterAsync(Person person) =>
        _frontDesk.UnregisterVoterAsync(_electionGuid, new UnregisterVoterDto
        {
            PersonGuid = person.PersonGuid,
            Reason = "Change method"
        });

    private void AddOnlineRow(Person person, string status)
    {
        person.HasOnlineBallot = true;
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = _electionGuid,
            PersonGuid = person.PersonGuid,
            Status = status,
            WhenStatus = DateTimeOffset.UtcNow,
            ListPool = """{"votes":[{"voteName":"Ada","positionOnBallot":1}],"pool":[]}"""
        });
        Context.SaveChanges();
    }

    private void SeedElection()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionGuid,
            Name = "Pending online then desk",
            NumberToElect = 3,
            ElectionType = "LSA",
            ElectionStage = ElectionStage.GatheringBallots,
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            RowVersion = new byte[8]
        });
        Context.SaveChanges();
    }

    private Person SeedEligiblePerson()
    {
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = _electionGuid,
            FirstName = "Ada",
            LastName = "Smith",
            CanVote = true,
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.SaveChanges();
        return person;
    }
}
