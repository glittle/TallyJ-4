using Microsoft.Extensions.Logging;
using Moq;
using Backend.Entities;
using Backend.DTOs.Votes;
using Backend.Services;
using Backend.Enumerations;

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
        _service = new VoteService(Context, _loggerMock.Object, _voteCountBroadcastMock.Object);

        SeedElectionGraph();
    }

    private void SeedElectionGraph(int numberToElect = 9)
    {
        var election = new Election
        {
            RowId = 1,
            ElectionGuid = ElectionGuid,
            Name = "Test Election",
            NumberToElect = numberToElect,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        };
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
            StatusCode = BallotStatus.Empty,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        Context.Elections.Add(election);
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
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.NotNull(result.Vote);
        Assert.Null(result.VotePositions);
        Assert.Equal(VoteStatus.Ok, result.Vote.VoteStatus);
        Assert.Equal(person.PersonGuid, result.Vote.PersonGuid);
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
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.NotNull(result.Vote);
        Assert.Equal(VoteStatus.Spoiled, result.Vote.VoteStatus);
        Assert.Equal("V06", result.Vote.IneligibleReasonCode);
        Assert.Equal(person.PersonGuid, result.Vote.PersonGuid);
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
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.Equal(VoteStatus.Spoiled, result.Vote.VoteStatus);
        Assert.Equal("X01", result.Vote.IneligibleReasonCode);
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
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.Equal(VoteStatus.Spoiled, result.Vote.VoteStatus);
        Assert.Null(result.Vote.IneligibleReasonCode);
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
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var result = await _service.DeleteVoteAsync(vote.RowId);

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Empty, result.BallotStatusCode);
        _voteCountBroadcastMock.Verify(s => s.QueueVoteCountUpdate(
            person.PersonGuid,
            ElectionGuid),
            Times.Once);
    }

    [Fact]
    public async Task DeleteVoteAsync_FromTooMany_ReEvaluatesBallotStatus()
    {
        var people = Enumerable.Range(0, 10).Select(_ => CreatePerson()).ToList();

        for (var i = 0; i < people.Count; i++)
        {
            await _service.CreateVoteAsync(new CreateVoteDto
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
            });
        }

        var extraVote = Context.Votes.OrderBy(v => v.PositionOnBallot).Last();
        var result = await _service.DeleteVoteAsync(extraVote.RowId);

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Ok, result.BallotStatusCode);
        Assert.Equal(BallotStatus.Ok, Context.Ballots.Single(b => b.BallotGuid == BallotGuid).StatusCode);
    }

    [Fact]
    public async Task DeleteVoteAsync_VoteWithoutPerson_DoesNotBroadcast()
    {
        var vote = new Vote
        {
            BallotGuid = BallotGuid,
            PersonGuid = null,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Spoiled,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var result = await _service.DeleteVoteAsync(vote.RowId);

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Empty, result.BallotStatusCode);
        _voteCountBroadcastMock.Verify(s => s.QueueVoteCountUpdate(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateVoteAsync_PersonLessU01_CreatesSpoiledVoteWithoutPerson()
    {
        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PositionOnBallot = 1,
            IneligibleReasonCode = "U01",
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.Null(result.Vote.PersonGuid);
        Assert.Equal(VoteStatus.Spoiled, result.Vote.VoteStatus);
        Assert.Equal("U01", result.Vote.IneligibleReasonCode);
        _voteCountBroadcastMock.Verify(
            s => s.QueueVoteCountUpdate(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateVoteAsync_PersonLessU02_CreatesSpoiledVoteWithoutPerson()
    {
        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PositionOnBallot = 1,
            IneligibleReasonCode = "U02",
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.Null(result.Vote.PersonGuid);
        Assert.Equal(VoteStatus.Spoiled, result.Vote.VoteStatus);
        Assert.Equal("U02", result.Vote.IneligibleReasonCode);
    }

    [Fact]
    public async Task CreateVoteAsync_BallotNotFound_ThrowsInvalidOperationException()
    {
        var dto = new CreateVoteDto
        {
            BallotGuid = Guid.NewGuid(),
            PersonGuid = Guid.NewGuid(),
            PositionOnBallot = 1,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateVoteAsync(dto));
    }

    [Fact]
    public async Task CreateVoteAsync_FewerThanRequiredVotes_SetsBallotStatusTooFew()
    {
        var person = CreatePerson();

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.Equal(BallotStatus.TooFew, result.BallotStatusCode);
        Assert.Equal(BallotStatus.TooFew, Context.Ballots.First(b => b.BallotGuid == BallotGuid).StatusCode);
    }

    [Fact]
    public async Task CreateVoteAsync_MoreThanRequiredVotes_SetsBallotStatusTooMany()
    {
        var people = Enumerable.Range(0, 10).Select(_ => CreatePerson()).ToList();

        for (var i = 0; i < people.Count; i++)
        {
            var dto = new CreateVoteDto
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
            };

            var result = await _service.CreateVoteAsync(dto);

            if (i < 8)
            {
                Assert.Equal(BallotStatus.TooFew, result.BallotStatusCode);
            }
            else if (i == 8)
            {
                Assert.Equal(BallotStatus.Ok, result.BallotStatusCode);
            }
            else
            {
                Assert.Equal(BallotStatus.TooMany, result.BallotStatusCode);
            }
        }
    }

    [Fact]
    public async Task CreateVoteAsync_DuplicatePersonOnBallot_CreatesVoteAndSetsBallotStatusToDup()
    {
        var election = Context.Elections.Single(e => e.ElectionGuid == ElectionGuid);
        election.NumberToElect = 2;
        await Context.SaveChangesAsync();

        var person = CreatePerson();

        var existingVote = new Vote
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(existingVote);
        await Context.SaveChangesAsync();

        var dto = new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 2,
        };

        var result = await _service.CreateVoteAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Dup, result.BallotStatusCode);
        Assert.Equal(person.PersonGuid, result.Vote.PersonGuid);

        var ballotInDb = Context.Ballots.First(b => b.BallotGuid == BallotGuid);
        Assert.Equal(BallotStatus.Dup, ballotInDb.StatusCode);

        Assert.Equal(2, Context.Votes.Count(v => v.BallotGuid == BallotGuid));
    }

    [Fact]
    public async Task CreateVoteAsync_IgnoresClientPosition_AssignsFirstAvailableSlot()
    {
        var first = CreatePerson();
        var second = CreatePerson();

        await _service.CreateVoteAsync(new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = first.PersonGuid,
            PositionOnBallot = 1,
        });

        var result = await _service.CreateVoteAsync(new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = second.PersonGuid,
            PositionOnBallot = 1,
        });

        Assert.NotNull(result.Vote);
        Assert.Equal(2, result.Vote.PositionOnBallot);
        Assert.Equal(second.PersonGuid, result.Vote.PersonGuid);
        Assert.NotNull(result.VotePositions);
        Assert.Equal(2, result.VotePositions.Count);
        AssertUniqueContiguousPositions(result.VotePositions);
        Assert.DoesNotContain(result.VotePositions, v => v.PositionOnBallot == 1 && v.RowId == result.Vote.RowId);
        Assert.Contains(result.VotePositions, v => v.PositionOnBallot == 2 && v.RowId == result.Vote.RowId);
    }

    [Fact]
    public async Task CreateVoteAsync_WithExistingGap_AssignsLowestAvailableSlot()
    {
        var people = Enumerable.Range(0, 3).Select(_ => CreatePerson()).ToList();

        Context.Votes.AddRange(new[]
        {
            new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[0].PersonGuid,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            },
            new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[1].PersonGuid,
                PositionOnBallot = 2,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            },
            new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[2].PersonGuid,
                PositionOnBallot = 5,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            },
        });
        await Context.SaveChangesAsync();

        var newPerson = CreatePerson();
        var result = await _service.CreateVoteAsync(new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = newPerson.PersonGuid,
            PositionOnBallot = 9,
        });

        Assert.NotNull(result.Vote);
        Assert.Equal(4, result.Vote.PositionOnBallot);
        Assert.Equal(newPerson.PersonGuid, result.Vote.PersonGuid);
        Assert.NotNull(result.VotePositions);
        Assert.Equal(4, result.VotePositions.Count);
        AssertUniqueContiguousPositions(result.VotePositions);
        Assert.Contains(result.VotePositions, v => v.RowId == result.Vote.RowId && v.PositionOnBallot == 4);
    }

    [Fact]
    public async Task CreateVoteAsync_WithDuplicateStoredPositions_CompactsBeforeAssigning()
    {
        var people = Enumerable.Range(0, 3).Select(_ => CreatePerson()).ToList();

        Context.Votes.AddRange(new[]
        {
            new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[0].PersonGuid,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            },
            new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[1].PersonGuid,
                PositionOnBallot = 2,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            },
            new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[2].PersonGuid,
                PositionOnBallot = 2,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            },
        });
        await Context.SaveChangesAsync();

        var newPerson = CreatePerson();
        var result = await _service.CreateVoteAsync(new CreateVoteDto
        {
            BallotGuid = BallotGuid,
            PersonGuid = newPerson.PersonGuid,
            PositionOnBallot = 2,
        });

        Assert.NotNull(result.Vote);
        Assert.Equal(4, result.Vote.PositionOnBallot);
        Assert.NotNull(result.VotePositions);
        Assert.Equal(4, result.VotePositions.Count);
        AssertUniqueContiguousPositions(result.VotePositions);
    }

    [Fact]
    public async Task DeleteVoteAsync_RenumbersRemainingVotesAndReturnsUpdatedList()
    {
        var people = Enumerable.Range(0, 4).Select(_ => CreatePerson()).ToList();

        for (var i = 0; i < people.Count; i++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            });
        }
        await Context.SaveChangesAsync();

        var voteToDelete = Context.Votes.Single(v => v.PositionOnBallot == 2);
        var result = await _service.DeleteVoteAsync(voteToDelete.RowId);

        Assert.NotNull(result);
        Assert.Null(result.Vote);
        Assert.NotNull(result.VotePositions);
        Assert.Equal(3, result.VotePositions.Count);
        AssertUniqueContiguousPositions(result.VotePositions);

        var remainingVotes = Context.Votes
            .Where(v => v.BallotGuid == BallotGuid)
            .OrderBy(v => v.PositionOnBallot)
            .ToList();
        Assert.Equal(people[0].PersonGuid, remainingVotes[0].PersonGuid);
        Assert.Equal(people[2].PersonGuid, remainingVotes[1].PersonGuid);
        Assert.Equal(people[3].PersonGuid, remainingVotes[2].PersonGuid);
        Assert.Equal(remainingVotes[0].RowId, result.VotePositions[0].RowId);
        Assert.Equal(remainingVotes[1].RowId, result.VotePositions[1].RowId);
        Assert.Equal(remainingVotes[2].RowId, result.VotePositions[2].RowId);
    }

    [Fact]
    public async Task ReorderVotesAsync_ReordersVotesToMatchSuppliedSequence()
    {
        var people = Enumerable.Range(0, 3).Select(_ => CreatePerson()).ToList();
        var voteIds = new List<int>();

        for (var i = 0; i < people.Count; i++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            });
        }
        await Context.SaveChangesAsync();

        voteIds.AddRange(Context.Votes
            .Where(v => v.BallotGuid == BallotGuid)
            .OrderBy(v => v.PositionOnBallot)
            .Select(v => v.RowId));

        var result = await _service.ReorderVotesAsync(new ReorderVotesDto
        {
            BallotGuid = BallotGuid,
            VoteRowIds = [voteIds[2], voteIds[0], voteIds[1]],
        });

        Assert.NotNull(result);
        Assert.Null(result.Vote);
        Assert.NotNull(result.VotePositions);
        Assert.Equal(3, result.VotePositions.Count);
        AssertUniqueContiguousPositions(result.VotePositions);

        var reorderedVotes = Context.Votes
            .Where(v => v.BallotGuid == BallotGuid)
            .OrderBy(v => v.PositionOnBallot)
            .ToList();
        Assert.Equal(people[2].PersonGuid, reorderedVotes[0].PersonGuid);
        Assert.Equal(people[0].PersonGuid, reorderedVotes[1].PersonGuid);
        Assert.Equal(people[1].PersonGuid, reorderedVotes[2].PersonGuid);
        Assert.Equal(reorderedVotes.Select(v => v.RowId), result.VotePositions.Select(vp => vp.RowId));
    }

    [Fact]
    public async Task ReorderVotesAsync_BallotNotFound_ReturnsNull()
    {
        var result = await _service.ReorderVotesAsync(new ReorderVotesDto
        {
            BallotGuid = Guid.NewGuid(),
            VoteRowIds = [1],
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task ReorderVotesAsync_MismatchedVoteCount_ThrowsInvalidOperationException()
    {
        var person = CreatePerson();
        Context.Votes.Add(new Vote
        {
            BallotGuid = BallotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReorderVotesAsync(new ReorderVotesDto
            {
                BallotGuid = BallotGuid,
                VoteRowIds = [1, 2],
            }));
    }

    private static void AssertUniqueContiguousPositions(IReadOnlyList<VoteDto> votes)
    {
        var positions = votes.Select(v => v.PositionOnBallot).ToList();
        Assert.Equal(positions.Count, positions.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, votes.Count), positions.OrderBy(p => p));
    }

    private static void AssertUniqueContiguousPositions(IReadOnlyList<VotePositionDto> votePositions)
    {
        var positions = votePositions.Select(v => v.PositionOnBallot).ToList();
        Assert.Equal(positions.Count, positions.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, votePositions.Count), positions.OrderBy(p => p));
    }
}
