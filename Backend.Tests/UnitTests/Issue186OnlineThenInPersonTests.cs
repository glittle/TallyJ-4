using Backend.DTOs.FrontDesk;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests;

/// <summary>
/// #186: a voter who started or finished online, then appears at Front Desk.
/// Product is already v3-aligned (withdraw pending; refuse accepted/claimed).
/// </summary>
public class Issue186OnlineThenInPersonTests : ServiceTestBase
{
    private readonly FrontDeskService _service;
    private readonly Guid _electionGuid = Guid.NewGuid();

    public Issue186OnlineThenInPersonTests()
    {
        _service = new FrontDeskService(
            Context,
            new Mock<ILogger<FrontDeskService>>().Object,
            new Mock<ISignalRNotificationService>().Object);
    }

    [Fact]
    public async Task DraftOnline_ThenInPerson_WithdrawsPendingAndRecordsMethod()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        person.HasOnlineBallot = true;
        Context.OnlineVotingInfos.Add(OnlineRow(person, OnlineBallotStatus.Draft));
        await Context.SaveChangesAsync();

        var result = await CheckInInPersonAsync(person);

        Assert.Equal(VotingMethodCodes.InPerson, result.VotingMethod);
        Assert.Null(result.OnlineBallotStatus);
        Assert.False(Context.People.Single().HasOnlineBallot);
        Assert.Empty(Context.OnlineVotingInfos);
    }

    [Fact]
    public async Task SubmittedOnline_ThenInPerson_WithdrawsPendingAndRecordsMethod()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        person.HasOnlineBallot = true;
        Context.OnlineVotingInfos.Add(OnlineRow(person, OnlineBallotStatus.Submitted));
        await Context.SaveChangesAsync();

        var result = await CheckInInPersonAsync(person);

        Assert.Equal(VotingMethodCodes.InPerson, result.VotingMethod);
        Assert.Null(result.OnlineBallotStatus);
        Assert.False(Context.People.Single().HasOnlineBallot);
        Assert.Empty(Context.OnlineVotingInfos);
    }

    [Fact]
    public async Task ProcessingOnline_ThenInPerson_IsRefused()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        Context.OnlineVotingInfos.Add(OnlineRow(person, OnlineBallotStatus.Processing));
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInInPersonAsync(person));

        Assert.Equal(FrontDeskMessageKeys.AlreadyProcessingOnline, ex.Message);
        Assert.Null(Context.People.Single().RegistrationTime);
        Assert.Null(Context.People.Single().VotingMethod);
    }

    [Fact]
    public async Task ProcessedOnline_ThenInPerson_IsRefused()
    {
        SeedElection();
        var person = SeedEligiblePerson();
        Context.OnlineVotingInfos.Add(OnlineRow(person, OnlineBallotStatus.Processed));
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CheckInInPersonAsync(person));

        Assert.Equal(FrontDeskMessageKeys.AlreadyAcceptedOnline, ex.Message);
        Assert.Null(Context.People.Single().RegistrationTime);
        Assert.Null(Context.People.Single().VotingMethod);
    }

    private Task<FrontDeskVoterDto> CheckInInPersonAsync(Person person) =>
        _service.CheckInVoterAsync(_electionGuid, new CheckInVoterDto
        {
            PersonGuid = person.PersonGuid,
            VotingMethod = VotingMethodCodes.InPerson,
            Teller1 = "Ada"
        });

    private OnlineVotingInfo OnlineRow(Person person, string status) =>
        new()
        {
            ElectionGuid = _electionGuid,
            PersonGuid = person.PersonGuid,
            Status = status,
            WhenStatus = DateTimeOffset.UtcNow
        };

    private void SeedElection()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionGuid,
            Name = "Online then in person",
            NumberToElect = 3,
            ElectionType = "LSA",
            ElectionStage = ElectionStage.GatheringBallots,
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
