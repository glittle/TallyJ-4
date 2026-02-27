using Microsoft.Extensions.Logging;
using Moq;
using Backend.Domain.Entities;
using Backend.DTOs.Votes;
using Backend.Services;
using Backend.Domain.Enumerations;

namespace Backend.Tests.UnitTests.Services;

public class VoteServiceTests : ServiceTestBase
{
    private readonly VoteService _service;
    private readonly Mock<ILogger<VoteService>> _loggerMock;
    private readonly Mock<IVoteCountBroadcastService> _voteCountBroadcastMock;

    private static readonly Guid ElectionGuid = Guid.NewGuid();
    private static readonly Guid LocationGuid = Guid.NewGuid();
    private static readonly Guid BallotGuid = Guid.NewGuid();

    public VoteServiceTests()
    {
        _loggerMock = new Mock<ILogger<VoteService>>();
        _voteCountBroadcastMock = new Mock<IVoteCountBroadcastService>();
        _service = new VoteService(Context, Mapper, _loggerMock.Object, _voteCountBroadcastMock.Object);

        SeedElectionGraph();
    }

    private void SeedElectionGraph()
    {
        var location = new Location
        {
            RowId = 1,
            LocationGuid = LocationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Test Location"
        };
        var ballot = new Ballot
        {
            RowId = 1,
            BallotGuid = BallotGuid,
            LocationGuid = LocationGuid,
            StatusCode = "Ok",
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        Context.Locations.Add(location);
        Context.Ballots.Add(ballot);
        Context.SaveChanges();
    }

    private Person CreatePerson(Guid? ineligibleReasonGuid = null, bool canReceiveVotes = true)
    {
        var person = new Person
        {
            RowId = Context.People.Count() + 1,
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = ElectionGuid,
            LastName = "Smith",
            FirstName = "John",
            FullName = "Smith, John",
            FullNameFl = "John Smith",
            CanReceiveVotes = canReceiveVotes,
            CanVote = true,
            IneligibleReasonGuid = ineligibleReasonGuid,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.SaveChanges();
        return person;
    }

    [Fact]
    public async Task CreateVoteAsync_EligiblePerson_CreatesVoteWithOkStatus()
    {
        var person = CreatePerson();

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("ok", result.StatusCode);
        Assert.Equal(person.PersonGuid, result.PersonGuid);
    }

    [Fact]
    public async Task CreateVoteAsync_IneligiblePerson_CreatesSpoiledVoteWithReasonCode()
    {
        var person = CreatePerson(
            ineligibleReasonGuid: IneligibleReasonEnum.V06_OtherCanVoteButNotBeVotedFor.ReasonGuid,
            canReceiveVotes: false);

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("V06", result.StatusCode);
        Assert.Equal(person.PersonGuid, result.PersonGuid);
    }

    [Fact]
    public async Task CreateVoteAsync_IneligiblePersonX01_CreatesSpoiledWithX01Code()
    {
        var person = CreatePerson(
            ineligibleReasonGuid: IneligibleReasonEnum.X01_Deceased.ReasonGuid,
            canReceiveVotes: false);

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.Equal("X01", result.StatusCode);
    }

    [Fact]
    public async Task CreateVoteAsync_IneligiblePersonNoReason_CreatesSpoiledWithFallback()
    {
        var person = CreatePerson(ineligibleReasonGuid: null, canReceiveVotes: false);

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.Equal("spoiled", result.StatusCode);
    }

    [Fact]
    public async Task CreateVoteAsync_QueuesVoteCountUpdate()
    {
        var person = CreatePerson();

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        await _service.CreateVoteAsync(dto);

        _voteCountBroadcastMock.Verify(s => s.QueueVoteCountUpdate(
            person.PersonGuid,
            ElectionGuid),
            Times.Once);
    }

    [Fact]
    public async Task CreateVoteAsync_IneligiblePerson_QueuesVoteCountUpdate()
    {
        var person = CreatePerson(
            ineligibleReasonGuid: IneligibleReasonEnum.V03_OnOtherInstitutionCounsellor.ReasonGuid,
            canReceiveVotes: false);

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        await _service.CreateVoteAsync(dto);

        _voteCountBroadcastMock.Verify(s => s.QueueVoteCountUpdate(
            person.PersonGuid,
            ElectionGuid),
            Times.Once);
    }

    [Fact]
    public async Task DeleteVoteAsync_BroadcastsVoteCountUpdate()
    {
        var person = CreatePerson();

        var vote = new Vote
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok",
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var result = await _service.DeleteVoteAsync(vote.RowId);

        Assert.True(result);
        _voteCountBroadcastMock.Verify(s => s.QueueVoteCountUpdate(
            person.PersonGuid,
            ElectionGuid),
            Times.Once);
    }

    [Fact]
    public async Task DeleteVoteAsync_VoteWithoutPerson_DoesNotBroadcast()
    {
        var vote = new Vote
        {
            BallotGuid = BallotGuid,
            PersonGuid = null,
            PositionOnBallot = 1,
            StatusCode = "spoiled",
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var result = await _service.DeleteVoteAsync(vote.RowId);

        Assert.True(result);
        _voteCountBroadcastMock.Verify(s => s.QueueVoteCountUpdate(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateVoteAsync_BallotNotFound_ThrowsInvalidOperationException()
    {
        var dto = new CreateVoteDto
        {
            BallotGuid = Guid.NewGuid(),
            PersonGuid = Guid.NewGuid(),
            PositionOnBallot = 1,
            StatusCode = "ok"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateVoteAsync(dto));
    }

    [Fact]
    public async Task CreateVoteAsync_DuplicatePersonOnBallot_ThrowsInvalidOperationException()
    {
        var person = CreatePerson();

        var existingVote = new Vote
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            StatusCode = "ok",
            RowVersion = new byte[8]
        };
        Context.Votes.Add(existingVote);
        await Context.SaveChangesAsync();

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 2,
            StatusCode = "ok"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateVoteAsync(dto));
    }
}
