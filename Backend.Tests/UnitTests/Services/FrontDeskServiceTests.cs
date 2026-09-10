using Microsoft.Extensions.Logging;
using Moq;
using Backend.DTOs.FrontDesk;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;

namespace Backend.Tests.UnitTests.Services;

public class FrontDeskServiceTests : ServiceTestBase
{
    private readonly FrontDeskService _service;
    private readonly Guid _electionGuid = Guid.NewGuid();

    public FrontDeskServiceTests()
    {
        _service = new FrontDeskService(
            Context,
            new Mock<ILogger<FrontDeskService>>().Object,
            new Mock<ISignalRNotificationService>().Object);
    }

    [Fact]
    public async Task CheckInVoterAsync_FinalizedElection_Throws()
    {
        SeedElection(ElectionStage.Finalized);
        var person = SeedEligiblePerson();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CheckInVoterAsync(_electionGuid, new CheckInVoterDto
            {
                PersonGuid = person.PersonGuid,
                VotingMethod = "P",
                Teller1 = "Ada"
            }));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
        Assert.Null(Context.People.Single().RegistrationTime);
    }

    [Fact]
    public async Task CheckInVoterAsync_GatheringBallots_Succeeds()
    {
        SeedElection(ElectionStage.GatheringBallots);
        var person = SeedEligiblePerson();

        var result = await _service.CheckInVoterAsync(_electionGuid, new CheckInVoterDto
        {
            PersonGuid = person.PersonGuid,
            VotingMethod = "P",
            Teller1 = "Ada"
        });

        Assert.Equal("P", result.VotingMethod);
        Assert.NotNull(Context.People.Single().RegistrationTime);
    }

    [Fact]
    public async Task GetEligibleVotersAsync_FinalizedElection_StillReads()
    {
        SeedElection(ElectionStage.Finalized);
        SeedEligiblePerson();

        var voters = await _service.GetEligibleVotersAsync(_electionGuid);

        Assert.Single(voters);
    }

    private void SeedElection(ElectionStage stage)
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionGuid,
            Name = "Front desk lock test",
            NumberToElect = 3,
            ElectionType = "LSA",
            ElectionStage = stage,
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
