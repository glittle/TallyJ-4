using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Entities;
using Backend.DTOs.People;
using Backend.Services;
using Backend.Enumerations;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Services;

public class PeopleServiceTests : ServiceTestBase
{
    private readonly PeopleService _service;
    private readonly Mock<ILogger<PeopleService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;

    public PeopleServiceTests()
    {
        _loggerMock = new Mock<ILogger<PeopleService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        _service = new PeopleService(Context, _loggerMock.Object, _signalRMock.Object);
    }

    [Fact]
    public async Task GetPeopleByElectionAsync_ReturnsOnlyEligiblePeopleWhenFiltered()
    {
        var electionGuid = Guid.NewGuid();

        var eligiblePerson1 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Alice",
            LastName = "Johnson", CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };

        var eligiblePerson2 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Bob",
            LastName = "Smith", CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };

        var ineligiblePerson = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Charlie",
            LastName = "Brown", CanReceiveVotes = false,
            CanVote = true,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(eligiblePerson1, eligiblePerson2, ineligiblePerson);
        await Context.SaveChangesAsync();

        var result = await _service.GetPeopleByElectionAsync(electionGuid, canReceiveVotes: true, pageSize: 200);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, p => p.PersonGuid == eligiblePerson1.PersonGuid);
        Assert.Contains(result.Items, p => p.PersonGuid == eligiblePerson2.PersonGuid);
        Assert.DoesNotContain(result.Items, p => p.PersonGuid == ineligiblePerson.PersonGuid);
    }

    [Fact]
    public async Task GetPeopleByElectionAsync_IncludesSoundCodes()
    {
        var electionGuid = Guid.NewGuid();

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "David",
            LastName = "Wilson", CanReceiveVotes = true,
            CanVote = true,
            CombinedSoundCodes = "W425|D130",
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var result = await _service.GetPeopleByElectionAsync(electionGuid, canReceiveVotes: true, pageSize: 200);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("W425|D130", result.Items[0].CombinedSoundCodes);
    }

    [Fact]
    public async Task GetPeopleByElectionAsync_OrdersByLastNameFirstName()
    {
        var electionGuid = Guid.NewGuid();

        var person1 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Zoe",
            LastName = "Anderson", CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        var person2 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Alice",
            LastName = "Anderson", CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        var person3 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Bob",
            LastName = "Baker", CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(person1, person2, person3);
        await Context.SaveChangesAsync();

        var result = await _service.GetPeopleByElectionAsync(electionGuid, canReceiveVotes: true, pageSize: 200);

        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(person2.PersonGuid, result.Items[0].PersonGuid);
        Assert.Equal(person1.PersonGuid, result.Items[1].PersonGuid);
        Assert.Equal(person3.PersonGuid, result.Items[2].PersonGuid);
    }

    [Fact]
    public async Task GetPeopleByElectionAsync_FiltersMultipleElections()
    {
        var electionGuid1 = Guid.NewGuid();
        var electionGuid2 = Guid.NewGuid();

        var person1 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid1,
            FirstName = "John",
            LastName = "Doe", CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        var person2 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid2,
            FirstName = "Jane",
            LastName = "Doe", CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(person1, person2);
        await Context.SaveChangesAsync();

        var result = await _service.GetPeopleByElectionAsync(electionGuid1, canReceiveVotes: true, pageSize: 200);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(person1.PersonGuid, result.Items[0].PersonGuid);
    }

    [Fact]
    public async Task CreatePersonAsync_WithNullIneligibleReasonCode_SetsFullEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var createDto = new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            IneligibleReasonCode = null
        };

        var result = await _service.CreatePersonAsync(createDto);

        Assert.NotNull(result);
        Assert.True(result.CanVote);
        Assert.True(result.CanReceiveVotes);
        Assert.Null(result.IneligibleReasonCode);
    }

    [Fact]
    public async Task CreatePersonAsync_WithX01Guid_SetsNoEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var createDto = new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code
        };

        var result = await _service.CreatePersonAsync(createDto);

        Assert.NotNull(result);
        Assert.False(result.CanVote);
        Assert.False(result.CanReceiveVotes);
        Assert.Equal("X01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task CreatePersonAsync_WithV01Guid_SetsVoteOnlyEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var createDto = new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            IneligibleReasonCode = IneligibleReasonEnum.V01_YouthAged181920.Code
        };

        var result = await _service.CreatePersonAsync(createDto);

        Assert.NotNull(result);
        Assert.True(result.CanVote);
        Assert.False(result.CanReceiveVotes);
        Assert.Equal("V01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task CreatePersonAsync_WithR01Guid_SetsReceiveOnlyEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var createDto = new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            IneligibleReasonCode = IneligibleReasonEnum.R01_NotADelegateInThisElection.Code
        };

        var result = await _service.CreatePersonAsync(createDto);

        Assert.NotNull(result);
        Assert.False(result.CanVote);
        Assert.True(result.CanReceiveVotes);
        Assert.Equal("R01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ChangingIneligibleReasonCode_UpdatesEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John", CanVote = true,
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var updateDto = new UpdatePersonDto
        {
            IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code
        };

        var result = await _service.UpdatePersonAsync(person.PersonGuid, updateDto);

        Assert.NotNull(result);
        Assert.False(result.CanVote);
        Assert.False(result.CanReceiveVotes);
        Assert.Equal("X01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ChangingIneligibleReasonCode_SpoilsExistingVotes()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();
        var ballotGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            RowId = 1,
            ElectionGuid = electionGuid,
            Name = "Test Election",
            NumberToElect = 3,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            RowId = 1,
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Test Location"
        });
        Context.Ballots.Add(new Ballot
        {
            RowId = 1,
            BallotGuid = ballotGuid,
            LocationGuid = locationGuid,
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        });

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John", CanVote = true,
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var vote = new Vote
        {
            BallotGuid = ballotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var updateDto = new UpdatePersonDto
        {
            IneligibleReasonCode = IneligibleReasonEnum.V01_YouthAged181920.Code
        };

        var result = await _service.UpdatePersonAsync(person.PersonGuid, updateDto);

        Assert.NotNull(result);
        Assert.False(result.CanReceiveVotes);

        var updatedVote = await Context.Votes.SingleAsync(v => v.RowId == vote.RowId);
        Assert.Equal(VoteStatus.Spoiled, updatedVote.VoteStatus);
        Assert.Equal("V01", updatedVote.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ClearingIneligibleReasonCode_UnspoilsExistingVotes()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();
        var ballotGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            RowId = 1,
            ElectionGuid = electionGuid,
            Name = "Test Election",
            NumberToElect = 3,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            RowId = 1,
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Test Location"
        });
        Context.Ballots.Add(new Ballot
        {
            RowId = 1,
            BallotGuid = ballotGuid,
            LocationGuid = locationGuid,
            StatusCode = BallotStatus.TooFew,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        });

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John", CanVote = true,
            CanReceiveVotes = false,
            IneligibleReasonCode = IneligibleReasonEnum.V01_YouthAged181920.Code,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var vote = new Vote
        {
            BallotGuid = ballotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Spoiled,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var updateDto = new UpdatePersonDto
        {
            IneligibleReasonCode = null
        };

        var result = await _service.UpdatePersonAsync(person.PersonGuid, updateDto);

        Assert.NotNull(result);
        Assert.True(result.CanReceiveVotes);

        var updatedVote = await Context.Votes.SingleAsync(v => v.RowId == vote.RowId);
        Assert.Equal(VoteStatus.Ok, updatedVote.VoteStatus);
        Assert.Null(updatedVote.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ClearingIneligibleReasonCode_RestoresFullEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John", CanVote = false,
            CanReceiveVotes = false,
            IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var updateDto = new UpdatePersonDto
        {
            IneligibleReasonCode = null
        };

        var result = await _service.UpdatePersonAsync(person.PersonGuid, updateDto);

        Assert.NotNull(result);
        Assert.True(result.CanVote);
        Assert.True(result.CanReceiveVotes);
        Assert.Null(result.IneligibleReasonCode);
    }

    [Fact]
    public async Task GetAllForBallotEntryAsync_ReturnsAllPeople_IncludingIneligible()
    {
        var electionGuid = Guid.NewGuid();

        var eligible = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Alpha",
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };
        var ineligible = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Beta",
            CanReceiveVotes = false,
            CanVote = true,
            IneligibleReasonCode = IneligibleReasonEnum.V06_OtherCanVoteButNotBeVotedFor.Code,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(eligible, ineligible);
        await Context.SaveChangesAsync();

        var result = await _service.GetAllForBallotEntryAsync(electionGuid);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.PersonGuid == eligible.PersonGuid);
        Assert.Contains(result, p => p.PersonGuid == ineligible.PersonGuid);
    }

    [Fact]
    public async Task GetAllForBallotEntryAsync_VoteCount_IsLiveFromVoteTable()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();
        var ballotGuid = Guid.NewGuid();

        var location = new Backend.Entities.Location
        {
            RowId = 900,
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Test Location"
        };
        var ballot = new Backend.Entities.Ballot
        {
            RowId = 900,
            BallotGuid = ballotGuid,
            LocationGuid = locationGuid,
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Voter",
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };
        Context.Locations.Add(location);
        Context.Ballots.Add(ballot);
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var vote = new Backend.Entities.Vote
        {
            BallotGuid = ballotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();

        var result = await _service.GetAllForBallotEntryAsync(electionGuid);

        var personDto = result.Single(p => p.PersonGuid == person.PersonGuid);
        Assert.Equal(1, personDto.VoteCount);
    }

    [Fact]
    public async Task GetAllForBallotEntryAsync_VoteCount_ZeroWhenNoVotes()
    {
        var electionGuid = Guid.NewGuid();

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "NoVotes",
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var result = await _service.GetAllForBallotEntryAsync(electionGuid);

        Assert.Single(result);
        Assert.Equal(0, result[0].VoteCount);
    }

    [Fact]
    public async Task GetAllForBallotEntryAsync_ExcludesPeopleFromOtherElections()
    {
        var election1 = Guid.NewGuid();
        var election2 = Guid.NewGuid();

        Context.People.AddRange(
            new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = election1, LastName = "InElection1", RowVersion = new byte[8] },
            new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = election2, LastName = "InElection2", RowVersion = new byte[8] }
        );
        await Context.SaveChangesAsync();

        var result = await _service.GetAllForBallotEntryAsync(election1);

        Assert.Single(result);
        Assert.Equal("InElection1", result[0].LastName);
    }

    [Fact]
    public async Task DeletePersonAsync_ThrowsWhenPersonHasVotingMethod()
    {
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Voter",
            VotingMethod = "I",
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeletePersonAsync(person.PersonGuid));
    }

    [Fact]
    public async Task DeletePersonAsync_ThrowsWhenPersonHasBeenVotedFor()
    {
        var electionGuid = Guid.NewGuid();
        var locationGuid = Guid.NewGuid();
        var ballotGuid = Guid.NewGuid();

        Context.Elections.Add(new Election
        {
            RowId = 1,
            ElectionGuid = electionGuid,
            Name = "Test Election",
            NumberToElect = 3,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            RowId = 1,
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Main"
        });
        Context.Ballots.Add(new Ballot
        {
            RowId = 1,
            BallotGuid = ballotGuid,
            LocationGuid = locationGuid,
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        });

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Person",
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.Votes.Add(new Vote
        {
            BallotGuid = ballotGuid,
            PersonGuid = person.PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Ok,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeletePersonAsync(person.PersonGuid));
    }

    [Fact]
    public async Task GetPersonDetailsAsync_DoesNotMintKioskCodeOnRead()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            RowId = 1,
            ElectionGuid = electionGuid,
            Name = "Kiosk Election",
            NumberToElect = 3,
            ElectionType = "Loc",
            VotingMethods = "K",
            RowVersion = new byte[8]
        });

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Nguyen",
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.True(string.IsNullOrWhiteSpace(details.KioskCode));
        Assert.False(details.KioskCodeConsumed);
        Assert.Null(details.KioskCodeExpiresAt);
        Assert.True(string.IsNullOrWhiteSpace(Context.People.Single().KioskCode));
        Assert.True(details.CanDelete);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_MintsCodeAndOpensFifteenMinuteWindow()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);

        var before = DateTimeOffset.UtcNow;
        var code = await _service.GenerateKioskCodeAsync(person.PersonGuid);
        var after = DateTimeOffset.UtcNow;

        Assert.NotNull(code);
        Assert.Equal(5, code.Length);
        Assert.StartsWith("S", code);

        var stored = Context.People.Single();
        Assert.Equal(code, stored.KioskCode);

        var onlineVoter = Assert.Single(Context.OnlineVoters);
        Assert.Equal(KioskCodeLifetime.ToVoterId(electionGuid, code!), onlineVoter.VoterId);
        Assert.Equal(KioskCodeLifetime.VoterIdType, onlineVoter.VoterIdType);
        Assert.Equal(code, onlineVoter.VerifyCode);
        Assert.NotNull(onlineVoter.VerifyCodeDate);
        Assert.InRange(onlineVoter.VerifyCodeDate.Value, before, after);

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);
        Assert.Equal(code, details!.KioskCode);
        Assert.NotNull(details.KioskCodeExpiresAt);
        Assert.Equal(
            onlineVoter.VerifyCodeDate.Value.AddMinutes(KioskCodeLifetime.LifetimeMinutes),
            details.KioskCodeExpiresAt);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_RenewsSameCodeAndLoginWindow()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);
        var first = await _service.GenerateKioskCodeAsync(person.PersonGuid);
        var firstWindow = Context.OnlineVoters.Single().VerifyCodeDate;

        Context.OnlineVoters.Single().VerifyCodeDate = DateTimeOffset.UtcNow.AddMinutes(-10);
        await Context.SaveChangesAsync();

        var renewed = await _service.GenerateKioskCodeAsync(person.PersonGuid);

        Assert.Equal(first, renewed);
        var onlineVoter = Assert.Single(Context.OnlineVoters);
        Assert.Equal(KioskCodeLifetime.ToVoterId(electionGuid, first!), onlineVoter.VoterId);
        Assert.True(onlineVoter.VerifyCodeDate > firstWindow);
        Assert.True(KioskCodeLifetime.IsLoginWindowOpen(onlineVoter.VerifyCodeDate));
    }

    [Theory]
    [InlineData("E")]
    [InlineData("P")]
    public async Task GenerateKioskCodeAsync_DoesNotRetagEmailOrPhoneOnlineVoter(string occupantType)
    {
        const string code = "COLLX";
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);
        person.KioskCode = code;
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = KioskCodeLifetime.ToVoterId(electionGuid, code),
            VoterIdType = occupantType,
            VerifyCode = "KEEPME",
            VerifyCodeDate = DateTimeOffset.UtcNow.AddMinutes(-2),
            WhenRegistered = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskCodeAsync(person.PersonGuid));

        Assert.Equal("This kiosk code is already used as another voter identity.", ex.Message);

        var row = Assert.Single(Context.OnlineVoters);
        Assert.Equal(KioskCodeLifetime.ToVoterId(electionGuid, code), row.VoterId);
        Assert.Equal(occupantType, row.VoterIdType);
        Assert.Equal("KEEPME", row.VerifyCode);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("P")]
    public async Task GetPersonDetailsAsync_DoesNotUseEmailOrPhoneRowForKioskExpiry(string occupantType)
    {
        const string code = "DETLX";
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);
        person.KioskCode = code;
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = KioskCodeLifetime.ToVoterId(electionGuid, code),
            VoterIdType = occupantType,
            VerifyCodeDate = DateTimeOffset.UtcNow
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.Equal(code, details!.KioskCode);
        Assert.False(details.KioskCodeConsumed);
        Assert.Null(details.KioskCodeExpiresAt);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_UsedEmptyCode_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);
        person.KioskCode = string.Empty;
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskCodeAsync(person.PersonGuid));

        Assert.Equal("This kiosk code has already been used.", ex.Message);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_ElectionWithoutKiosk_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "P");
        var person = SeedPerson(electionGuid);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskCodeAsync(person.PersonGuid));

        Assert.Equal("This election does not support kiosk voting.", ex.Message);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_FinalizedElection_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.Finalized, votingMethods: "K");
        var person = SeedPerson(electionGuid);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskCodeAsync(person.PersonGuid));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_FrontDeskVotingMethod_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);
        person.VotingMethod = "P";
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskCodeAsync(person.PersonGuid));

        Assert.Equal("Cannot generate a kiosk code for a person who has already registered.", ex.Message);
    }

    [Fact]
    public async Task GenerateKioskCodeAsync_ProcessedOnlineBallot_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots, votingMethods: "K");
        var person = SeedPerson(electionGuid);
        await SeedOnlineVotingInfoAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Processed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskCodeAsync(person.PersonGuid));

        Assert.Equal("Cannot generate a kiosk code for a person who has already voted.", ex.Message);
    }

    [Fact]
    public async Task CreatePersonAsync_WithPhone_CreatesOnlineVoterPhoneRow_WhenRegisteredNull()
    {
        const string phone = "+14168972671";
        var result = await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });

        Assert.NotNull(result);
        var rows = await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal("P", row.VoterIdType);
        Assert.Null(row.WhenRegistered);
        Assert.Null(row.WhenLastLogin);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task CreatePersonAsync_WithoutPhone_DoesNotCreatePhoneOnlineVoter()
    {
        var before = await Context.OnlineVoters.CountAsync();
        await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Email = "pat@example.com"
        });

        Assert.Equal(before, await Context.OnlineVoters.CountAsync());
        Assert.False(await Context.OnlineVoters.AnyAsync(ov => ov.VoterIdType == "P"));
    }

    [Fact]
    public async Task CreatePersonAsync_ExistingPhoneOnlineVoter_DoesNotDuplicateOrWipeFields()
    {
        const string phone = "+14168972671";
        var registered = DateTimeOffset.Parse("2026-01-15T12:00:00Z");
        var lastLogin = DateTimeOffset.Parse("2026-02-01T08:30:00Z");
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = "undeliverable",
            WhenRegistered = registered,
            WhenLastLogin = lastLogin
        });
        await Context.SaveChangesAsync();

        await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });

        var rows = await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal("undeliverable", row.SmsStatus);
        Assert.Equal(registered, row.WhenRegistered);
        Assert.Equal(lastLogin, row.WhenLastLogin);
    }

    [Fact]
    public async Task UpdatePersonAsync_AddingPhone_CreatesOnlineVoterPhoneRow_WhenRegisteredNull()
    {
        const string phone = "+14168972672";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });

        var row = Assert.Single(await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync());
        Assert.Equal("P", row.VoterIdType);
        Assert.Null(row.WhenRegistered);
        Assert.Null(row.WhenLastLogin);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task UpdatePersonAsync_ExistingPhoneOnlineVoter_DoesNotDuplicateOrWipeFields()
    {
        const string phone = "+14168972673";
        var registered = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
        var lastLogin = DateTimeOffset.Parse("2026-03-02T00:00:00Z");
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = "OK",
            WhenRegistered = registered,
            WhenLastLogin = lastLogin
        });
        await Context.SaveChangesAsync();

        await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });

        var row = Assert.Single(await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync());
        Assert.Equal("OK", row.SmsStatus);
        Assert.Equal(registered, row.WhenRegistered);
        Assert.Equal(lastLogin, row.WhenLastLogin);
    }

    [Fact]
    public async Task UpdatePersonAsync_WithoutPhone_DoesNotCreatePhoneOnlineVoter()
    {
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Email = "pat@example.com",
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Smith",
            FirstName = "Pat",
            Email = "pat@example.com"
        });

        Assert.False(await Context.OnlineVoters.AnyAsync(ov => ov.VoterIdType == "P"));
    }

    [Fact]
    public async Task GetPersonDetailsAsync_NoPhone_PhoneOnlineVoterIsNull()
    {
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Email = "pat@example.com",
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.Null(details.PhoneOnlineVoter);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_PhoneWithNoOnlineVoter_HasPhoneRowFalse()
    {
        const string phone = "+14168972680";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.False(details.PhoneOnlineVoter.HasPhoneRow);
        Assert.Null(details.PhoneOnlineVoter.WhenRegistered);
        Assert.Null(details.PhoneOnlineVoter.WhenLastLogin);
        Assert.Null(details.PhoneOnlineVoter.SmsStatus);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_PhoneRow_NullWhenRegistered_NullSmsStatus()
    {
        const string phone = "+14168972681";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = null,
            WhenRegistered = null,
            WhenLastLogin = null
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.True(details.PhoneOnlineVoter.HasPhoneRow);
        Assert.Null(details.PhoneOnlineVoter.WhenRegistered);
        Assert.Null(details.PhoneOnlineVoter.WhenLastLogin);
        Assert.Null(details.PhoneOnlineVoter.SmsStatus);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_PhoneRow_SmsStatusOk()
    {
        const string phone = "+14168972682";
        var registered = DateTimeOffset.Parse("2026-04-01T12:00:00Z");
        var lastLogin = DateTimeOffset.Parse("2026-04-02T08:00:00Z");
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = "OK",
            WhenRegistered = registered,
            WhenLastLogin = lastLogin
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.True(details.PhoneOnlineVoter.HasPhoneRow);
        Assert.Equal(registered, details.PhoneOnlineVoter.WhenRegistered);
        Assert.Equal(lastLogin, details.PhoneOnlineVoter.WhenLastLogin);
        Assert.Equal("OK", details.PhoneOnlineVoter.SmsStatus);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_PhoneRow_SmsStatusBlockReason()
    {
        const string phone = "+14168972683";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = "landline",
            WhenRegistered = DateTimeOffset.Parse("2026-03-01T00:00:00Z")
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.True(details.PhoneOnlineVoter.HasPhoneRow);
        Assert.Equal("landline", details.PhoneOnlineVoter.SmsStatus);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task GetPersonDetailsAsync_NonPOccupancy_NotReturnedAsPhoneStatus(string existingType)
    {
        const string phone = "+14168972684";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = existingType,
            SmsStatus = "admin",
            WhenRegistered = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            WhenLastLogin = DateTimeOffset.Parse("2026-01-02T00:00:00Z")
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.False(details.PhoneOnlineVoter.HasPhoneRow);
        Assert.Null(details.PhoneOnlineVoter.WhenRegistered);
        Assert.Null(details.PhoneOnlineVoter.WhenLastLogin);
        Assert.Null(details.PhoneOnlineVoter.SmsStatus);
    }

    [Fact]
    public async Task CreatePersonAsync_FinalizedElection_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.Finalized);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreatePersonAsync(new CreatePersonDto
            {
                ElectionGuid = electionGuid,
                LastName = "Smith",
                FirstName = "Ada"
            }));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
        Assert.Empty(Context.People);
    }

    [Fact]
    public async Task CreatePersonAsync_ProcessingBallots_Succeeds()
    {
        var electionGuid = SeedElection(ElectionStage.ProcessingBallots);

        var result = await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "Ada"
        });

        Assert.Equal("Ada", result.FirstName);
        Assert.Single(Context.People);
    }

    [Fact]
    public async Task CreatePersonAsync_GatheringBallots_Succeeds()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);

        var result = await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Hopper",
            FirstName = "Grace"
        });

        Assert.Equal("Grace", result.FirstName);
        Assert.Equal("Hopper", result.LastName);
        Assert.Single(Context.People);
    }

    [Fact]
    public async Task CreatePersonAsync_SettingUp_Succeeds()
    {
        var electionGuid = SeedElection(ElectionStage.SettingUp);

        var result = await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Lovelace",
            FirstName = "Ada"
        });

        Assert.Equal("Ada", result.FirstName);
        Assert.Single(Context.People);
    }

    [Theory]
    [InlineData(ElectionStage.SettingUp)]
    [InlineData(ElectionStage.GatheringBallots)]
    [InlineData(ElectionStage.ProcessingBallots)]
    public async Task UpdatePersonAsync_NonFinalizedStages_Succeeds(ElectionStage stage)
    {
        var electionGuid = SeedElection(stage);
        var person = SeedPerson(electionGuid);

        var result = await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Changed",
            FirstName = "Ada",
            OtherInfo = "Added during election"
        });

        Assert.NotNull(result);
        Assert.Equal("Changed", result.LastName);
        Assert.Equal("Ada", result.FirstName);
        Assert.Equal("Added during election", result.OtherInfo);
        Assert.Equal("Changed", Context.People.Single().LastName);
    }

    [Fact]
    public async Task UpdatePersonAsync_VotingMethodSet_CannotMarkCannotVote()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.VotingMethod = "P";
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
            {
                LastName = person.LastName,
                FirstName = person.FirstName,
                IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code
            }));

        Assert.Equal(PeopleMessageKeys.CannotMarkCannotVoteAfterVoted, ex.Message);
        Assert.Null(Context.People.Single().IneligibleReasonCode);
        Assert.True(Context.People.Single().CanVote);
    }

    [Fact]
    public async Task UpdatePersonAsync_SubmittedOnlineBallot_CanStillMarkCannotVote()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.HasOnlineBallot = true;
        await SeedOnlineVotingInfoAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Submitted);

        var result = await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = person.LastName,
            FirstName = person.FirstName,
            IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code
        });

        Assert.NotNull(result);
        Assert.False(result.CanVote);
        Assert.Equal("X01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ProcessedOnlineBallot_CannotMarkCannotVote()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.HasOnlineBallot = true;
        await SeedOnlineVotingInfoAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Processed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
            {
                LastName = person.LastName,
                FirstName = person.FirstName,
                IneligibleReasonCode = IneligibleReasonEnum.R02_RightsRemovedCannotVote.Code
            }));

        Assert.Equal(PeopleMessageKeys.CannotMarkCannotVoteAfterVoted, ex.Message);
        Assert.True(Context.People.Single().CanVote);
    }

    [Fact]
    public async Task UpdatePersonAsync_VotingMethodSet_CanStillMarkCanVoteButNotReceive()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.VotingMethod = "O";
        await Context.SaveChangesAsync();

        var result = await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = person.LastName,
            FirstName = person.FirstName,
            IneligibleReasonCode = IneligibleReasonEnum.V01_YouthAged181920.Code
        });

        Assert.NotNull(result);
        Assert.True(result.CanVote);
        Assert.False(result.CanReceiveVotes);
        Assert.Equal("V01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_VotingMethodSet_CanClearEligibility()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.VotingMethod = "P";
        person.IneligibleReasonCode = IneligibleReasonEnum.V01_YouthAged181920.Code;
        person.CanVote = true;
        person.CanReceiveVotes = false;
        await Context.SaveChangesAsync();

        var result = await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = person.LastName,
            FirstName = person.FirstName,
            IneligibleReasonCode = null
        });

        Assert.NotNull(result);
        Assert.True(result.CanVote);
        Assert.True(result.CanReceiveVotes);
        Assert.Null(result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_AlreadyCannotVote_KeepsReasonWhenNotChangingEligibility()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.VotingMethod = "P";
        person.IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code;
        person.CanVote = false;
        person.CanReceiveVotes = false;
        await Context.SaveChangesAsync();

        var result = await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Renamed",
            FirstName = person.FirstName,
            IneligibleReasonCode = IneligibleReasonEnum.X01_Deceased.Code
        });

        Assert.NotNull(result);
        Assert.Equal("Renamed", result.LastName);
        Assert.Equal("X01", result.IneligibleReasonCode);
        Assert.False(result.CanVote);
    }

    [Fact]
    public async Task UpdatePersonAsync_FinalizedElection_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.Finalized);
        var person = SeedPerson(electionGuid);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
            {
                LastName = "Changed",
                FirstName = "Ada"
            }));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
        Assert.Equal("Smith", Context.People.Single().LastName);
    }

    [Fact]
    public async Task DeletePersonAsync_FinalizedElection_Throws()
    {
        var electionGuid = SeedElection(ElectionStage.Finalized);
        var person = SeedPerson(electionGuid);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeletePersonAsync(person.PersonGuid));

        Assert.Equal(ElectionStageMessageKeys.FinalizedWriteBlocked, ex.Message);
        Assert.Single(Context.People);
    }

    [Fact]
    public async Task GetPeopleByElectionAsync_FinalizedElection_StillReads()
    {
        var electionGuid = SeedElection(ElectionStage.Finalized);
        SeedPerson(electionGuid);

        var result = await _service.GetPeopleByElectionAsync(electionGuid, pageSize: 50);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_FinalizedElection_DoesNotGenerateKioskCode()
    {
        var electionGuid = SeedElection(ElectionStage.Finalized, votingMethods: "K");
        var person = SeedPerson(electionGuid);

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.True(string.IsNullOrWhiteSpace(details.KioskCode));
        Assert.True(string.IsNullOrWhiteSpace(Context.People.Single().KioskCode));
    }

    [Fact]
    public async Task GetPersonDetailsAsync_SubmittedOnlineBallot_HasAcceptedBallotFalse()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.HasOnlineBallot = true;
        await SeedOnlineVotingInfoAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Submitted);

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.True(details.HasOnlineBallot);
        Assert.False(details.HasAcceptedBallot);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_ProcessedOnlineBallot_HasAcceptedBallotTrue()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.HasOnlineBallot = true;
        await SeedOnlineVotingInfoAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Processed);

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.True(details.HasAcceptedBallot);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_VotingMethod_HasAcceptedBallotTrue()
    {
        var electionGuid = SeedElection(ElectionStage.GatheringBallots);
        var person = SeedPerson(electionGuid);
        person.VotingMethod = "P";
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.True(details.HasAcceptedBallot);
    }

    private Guid SeedElection(ElectionStage stage, string? votingMethods = null)
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "People lock test",
            NumberToElect = 3,
            ElectionType = "LSA",
            ElectionStage = stage,
            VotingMethods = votingMethods,
            RowVersion = new byte[8]
        });
        Context.SaveChanges();
        return electionGuid;
    }

    private async Task SeedOnlineVotingInfoAsync(Guid electionGuid, Guid personGuid, string status)
    {
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            Status = status,
            WhenStatus = DateTimeOffset.UtcNow
        });
        await Context.SaveChangesAsync();
    }

    private Person SeedPerson(Guid electionGuid)
    {
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
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



