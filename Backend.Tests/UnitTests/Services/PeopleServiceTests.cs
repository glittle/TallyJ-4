using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Entities;
using Backend.DTOs.People;
using Backend.Services;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services.Auth;

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
        Assert.Null(row.WhatsAppStatus);
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

        var created = await _service.CreatePersonAsync(new CreatePersonDto
        {
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });

        Assert.NotNull(created.PhoneOnlineVoter);
        Assert.True(created.PhoneOnlineVoter.HasPhoneRow);
        Assert.Equal("undeliverable", created.PhoneOnlineVoter.SmsStatus);
        Assert.Equal(registered, created.PhoneOnlineVoter.WhenRegistered);

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
        Assert.Null(details.PhoneOnlineVoter.WhatsAppStatus);
        Assert.Empty(details.PhoneOnlineVoter.RecentSmsLogs);
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
        Assert.Null(details.PhoneOnlineVoter.WhatsAppStatus);
        Assert.Empty(details.PhoneOnlineVoter.RecentSmsLogs);
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
        Assert.Null(details.PhoneOnlineVoter.WhatsAppStatus);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_PhoneRow_WhatsAppStatusFromPRowOnly()
    {
        const string phone = "+14168972685";
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
            WhatsAppStatus = "no-wa"
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.True(details.PhoneOnlineVoter.HasPhoneRow);
        Assert.Equal("OK", details.PhoneOnlineVoter.SmsStatus);
        Assert.Equal("no-wa", details.PhoneOnlineVoter.WhatsAppStatus);
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
            WhatsAppStatus = "OK",
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
        Assert.Null(details.PhoneOnlineVoter.WhatsAppStatus);
        Assert.Empty(details.PhoneOnlineVoter.RecentSmsLogs);
    }

    [Fact]
    public async Task GetAllPeopleForListAsync_PhoneSmsHint_MatchesPersonDetailContract()
    {
        var electionGuid = Guid.NewGuid();
        const string neverSeenPhone = "+14168972700";
        const string importedPhone = "+14168972701";
        const string okPhone = "+14168972702";
        const string blockedPhone = "+14168972703";
        const string nonPPhone = "+14168972704";

        Context.People.AddRange(
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                LastName = "NoPhone",
                FirstName = "Pat",
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                LastName = "NeverSeen",
                FirstName = "Pat",
                Phone = neverSeenPhone,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                LastName = "Imported",
                FirstName = "Pat",
                Phone = importedPhone,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                LastName = "Ok",
                FirstName = "Pat",
                Phone = okPhone,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                LastName = "Blocked",
                FirstName = "Pat",
                Phone = blockedPhone,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                LastName = "NonP",
                FirstName = "Pat",
                Phone = nonPPhone,
                RowVersion = new byte[8]
            });
        Context.OnlineVoters.AddRange(
            new OnlineVoter { VoterId = importedPhone, VoterIdType = "P", SmsStatus = null },
            new OnlineVoter { VoterId = okPhone, VoterIdType = "P", SmsStatus = "OK" },
            new OnlineVoter
            {
                VoterId = blockedPhone,
                VoterIdType = "P",
                SmsStatus = "landline"
            },
            new OnlineVoter
            {
                VoterId = nonPPhone,
                VoterIdType = "E",
                SmsStatus = "admin"
            });
        await Context.SaveChangesAsync();

        var list = await _service.GetAllPeopleForListAsync(electionGuid);

        Assert.Null(list.Single(p => p.Phone == null).PhoneOnlineVoter);

        var neverSeen = list.Single(p => p.Phone == neverSeenPhone).PhoneOnlineVoter;
        Assert.NotNull(neverSeen);
        Assert.False(neverSeen.HasPhoneRow);
        Assert.Null(neverSeen.SmsStatus);

        var imported = list.Single(p => p.Phone == importedPhone).PhoneOnlineVoter;
        Assert.NotNull(imported);
        Assert.True(imported.HasPhoneRow);
        Assert.Null(imported.WhenRegistered);
        Assert.Null(imported.SmsStatus);

        Assert.Equal("OK", list.Single(p => p.Phone == okPhone).PhoneOnlineVoter?.SmsStatus);
        Assert.Equal("landline", list.Single(p => p.Phone == blockedPhone).PhoneOnlineVoter?.SmsStatus);

        var nonP = list.Single(p => p.Phone == nonPPhone).PhoneOnlineVoter;
        Assert.NotNull(nonP);
        Assert.False(nonP.HasPhoneRow);
        Assert.Null(nonP.SmsStatus);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_RecentSmsLogs_ByPhonePlusMinus_NotElectionScoped()
    {
        const string storedPhone = "+14168972685";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = storedPhone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = storedPhone,
            VoterIdType = "P",
            SmsStatus = "OK"
        });
        Context.SmsLogs.AddRange(
            new SmsLog
            {
                SmsSid = "SM-other-phone",
                Phone = "+14165550100",
                SentDate = DateTimeOffset.Parse("2026-05-01T12:00:00Z"),
                LastStatus = "delivered"
            },
            new SmsLog
            {
                SmsSid = "SM-variant",
                Phone = "14168972685",
                SentDate = DateTimeOffset.Parse("2026-05-03T08:00:00Z"),
                LastDate = DateTimeOffset.Parse("2026-05-03T08:02:00Z"),
                LastStatus = "undelivered",
                ErrorCode = 30003,
                ElectionGuid = Guid.NewGuid(),
                PersonGuid = Guid.NewGuid()
            },
            new SmsLog
            {
                SmsSid = "SM-older",
                Phone = storedPhone,
                SentDate = DateTimeOffset.Parse("2026-05-02T08:00:00Z"),
                LastStatus = "sent"
            });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.Equal(2, details.PhoneOnlineVoter.RecentSmsLogs.Count);
        Assert.Equal(
            DateTimeOffset.Parse("2026-05-03T08:00:00Z"),
            details.PhoneOnlineVoter.RecentSmsLogs[0].SentDate);
        Assert.Equal("undelivered", details.PhoneOnlineVoter.RecentSmsLogs[0].LastStatus);
        Assert.Equal(30003, details.PhoneOnlineVoter.RecentSmsLogs[0].ErrorCode);
        Assert.Equal("sent", details.PhoneOnlineVoter.RecentSmsLogs[1].LastStatus);
    }

    [Fact]
    public async Task GetPersonDetailsAsync_RecentSmsLogs_WithoutPRow()
    {
        const string phone = "+14168972686";
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
        Context.SmsLogs.Add(new SmsLog
        {
            SmsSid = "SM-no-prow",
            Phone = phone,
            SentDate = DateTimeOffset.Parse("2026-05-04T10:00:00Z"),
            LastStatus = "queued"
        });
        await Context.SaveChangesAsync();

        var details = await _service.GetPersonDetailsAsync(person.PersonGuid);

        Assert.NotNull(details);
        Assert.NotNull(details.PhoneOnlineVoter);
        Assert.False(details.PhoneOnlineVoter.HasPhoneRow);
        var log = Assert.Single(details.PhoneOnlineVoter.RecentSmsLogs);
        Assert.Equal("queued", log.LastStatus);
        Assert.Equal(DateTimeOffset.Parse("2026-05-04T10:00:00Z"), log.SentDate);
    }

    [Fact]
    public async Task SetPersonPhoneSmsStatusAsync_Ok_OnExistingPRow()
    {
        const string phone = "+14168972690";
        var registered = DateTimeOffset.Parse("2026-04-01T12:00:00Z");
        var person = await SeedPersonWithPhoneRow(phone, smsStatus: "twilio-30003", whenRegistered: registered);

        var result = await _service.SetPersonPhoneSmsStatusAsync(
            person.PersonGuid,
            new SetPersonPhoneSmsStatusDto { SmsStatus = "ok" });

        Assert.NotNull(result);
        Assert.True(result.HasPhoneRow);
        Assert.Equal(OnlineVoterSmsStatus.Ok, result.SmsStatus);
        Assert.Equal(registered, result.WhenRegistered);

        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal(OnlineVoterSmsStatus.Ok, row.SmsStatus);
        Assert.Equal(registered, row.WhenRegistered);
    }

    [Fact]
    public async Task SetPersonPhoneSmsStatusAsync_Reason_OnExistingPRow()
    {
        const string phone = "+14168972691";
        var person = await SeedPersonWithPhoneRow(phone, smsStatus: OnlineVoterSmsStatus.Ok);

        var result = await _service.SetPersonPhoneSmsStatusAsync(
            person.PersonGuid,
            new SetPersonPhoneSmsStatusDto { SmsStatus = " landline " });

        Assert.NotNull(result);
        Assert.Equal("landline", result.SmsStatus);

        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("landline", row.SmsStatus);
        Assert.True(OnlineVoterSmsStatus.AllowsPaidSend(OnlineVoterSmsStatus.Ok));
        Assert.False(OnlineVoterSmsStatus.AllowsPaidSend(row.SmsStatus));
    }

    [Fact]
    public async Task SetPersonPhoneSmsStatusAsync_NoPRow_EnsuresThenSets()
    {
        const string phone = "+14168972692";
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

        var result = await _service.SetPersonPhoneSmsStatusAsync(
            person.PersonGuid,
            new SetPersonPhoneSmsStatusDto { SmsStatus = "admin" });

        Assert.NotNull(result);
        Assert.True(result.HasPhoneRow);
        Assert.Equal("admin", result.SmsStatus);
        Assert.Null(result.WhenRegistered);

        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal("admin", row.SmsStatus);
        Assert.Null(row.WhenRegistered);
    }

    [Fact]
    public async Task SetPersonPhoneSmsStatusAsync_NonPOccupant_DoesNotConvert()
    {
        const string phone = "+14168972693";
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
            VoterIdType = "E",
            SmsStatus = null
        });
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetPersonPhoneSmsStatusAsync(
                person.PersonGuid,
                new SetPersonPhoneSmsStatusDto { SmsStatus = "OK" }));

        Assert.Equal(PeopleMessageKeys.PhoneSmsStatusNoPhoneRow, ex.Message);
        var occupant = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("E", occupant.VoterIdType);
        Assert.Null(occupant.SmsStatus);
    }

    [Fact]
    public async Task SetPersonPhoneSmsStatusAsync_NoPhone_Throws()
    {
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

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetPersonPhoneSmsStatusAsync(
                person.PersonGuid,
                new SetPersonPhoneSmsStatusDto { SmsStatus = "OK" }));

        Assert.Equal(PeopleMessageKeys.PhoneSmsStatusNoPhone, ex.Message);
    }

    [Fact]
    public async Task SetPersonPhoneSmsStatusAsync_UnknownPerson_ReturnsNull()
    {
        var result = await _service.SetPersonPhoneSmsStatusAsync(
            Guid.NewGuid(),
            new SetPersonPhoneSmsStatusDto { SmsStatus = "OK" });

        Assert.Null(result);
    }

    [Theory]
    [InlineData("OK")]
    [InlineData("no-wa")]
    public async Task CheckPersonPhoneWhatsAppAsync_PersistsProviderResultOnPRow(
        string expectedStatus)
    {
        const string phone = "+14168972710";
        var person = await SeedPersonWithPhoneRow(phone, smsStatus: "OK", whatsAppStatus: null);
        var client = MockWhatsAppClient(GreenApiWhatsAppCheckResult.FromProvider(expectedStatus));
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckPersonPhoneWhatsAppAsync(person.PersonGuid);

        Assert.NotNull(result);
        Assert.True(result.HasPhoneRow);
        Assert.Equal(expectedStatus, result.WhatsAppStatus);
        Assert.Equal("OK", result.SmsStatus);

        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal(expectedStatus, row.WhatsAppStatus);
        Assert.Equal("OK", row.SmsStatus);
        client.Verify(c => c.CheckWhatsAppAsync(phone, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckPersonPhoneWhatsAppAsync_NoPRow_EnsuresThenPersists()
    {
        const string phone = "+14168972711";
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
        var client = MockWhatsAppClient(
            GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.Ok));
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckPersonPhoneWhatsAppAsync(person.PersonGuid);

        Assert.NotNull(result);
        Assert.True(result.HasPhoneRow);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok, result.WhatsAppStatus);
        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok, row.WhatsAppStatus);
        Assert.Null(row.WhenRegistered);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task CheckPersonPhoneWhatsAppAsync_NonPOccupant_DoesNotConvert(string existingType)
    {
        const string phone = "+14168972712";
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
            WhatsAppStatus = null
        });
        await Context.SaveChangesAsync();
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        var service = CreateServiceWithWhatsApp(client.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckPersonPhoneWhatsAppAsync(person.PersonGuid));

        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppNoPhoneRow, ex.Message);
        var occupant = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal(existingType, occupant.VoterIdType);
        Assert.Null(occupant.WhatsAppStatus);
        client.Verify(c => c.CheckWhatsAppAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckPersonPhoneWhatsAppAsync_NotConfigured_DoesNotPersist()
    {
        const string phone = "+14168972713";
        var person = await SeedPersonWithPhoneRow(phone, smsStatus: null, whatsAppStatus: null);
        var client = MockWhatsAppClient(GreenApiWhatsAppCheckResult.NotConfigured());
        var service = CreateServiceWithWhatsApp(client.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckPersonPhoneWhatsAppAsync(person.PersonGuid));

        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppNotConfigured, ex.Message);
        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Null(row.WhatsAppStatus);
    }

    [Fact]
    public async Task UpdatePersonAsync_PhoneChange_ClearsWhatsAppStatusOnNewNumber_DoesNotCopyOld()
    {
        const string oldPhone = "+14168972714";
        const string newPhone = "+14168972715";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = oldPhone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.AddRange(
            new OnlineVoter
            {
                VoterId = oldPhone,
                VoterIdType = "P",
                WhatsAppStatus = OnlineVoterWhatsAppStatus.Ok,
                SmsStatus = "OK"
            },
            new OnlineVoter
            {
                VoterId = newPhone,
                VoterIdType = "P",
                WhatsAppStatus = "no-wa",
                SmsStatus = "OK"
            });
        await Context.SaveChangesAsync();

        await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Smith",
            FirstName = "Pat",
            Phone = newPhone
        });

        var oldRow = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == oldPhone);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok, oldRow.WhatsAppStatus);
        Assert.Equal("OK", oldRow.SmsStatus);

        var newRow = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == newPhone);
        Assert.Equal("P", newRow.VoterIdType);
        Assert.Null(newRow.WhatsAppStatus);
        Assert.Equal("OK", newRow.SmsStatus);
    }

    [Fact]
    public async Task UpdatePersonAsync_PhoneUnchanged_DoesNotClearWhatsAppStatus()
    {
        const string phone = "+14168972716";
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
            WhatsAppStatus = OnlineVoterWhatsAppStatus.Ok,
            SmsStatus = "OK"
        });
        await Context.SaveChangesAsync();

        await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });

        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok, row.WhatsAppStatus);
        Assert.Equal("OK", row.SmsStatus);
    }

    [Fact]
    public async Task UpdatePersonAsync_PhoneChange_NonPOccupantOfNewNumber_NotConverted()
    {
        const string oldPhone = "+14168972717";
        const string newPhone = "+14168972718";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = Guid.NewGuid(),
            LastName = "Smith",
            FirstName = "Pat",
            Phone = oldPhone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        Context.OnlineVoters.AddRange(
            new OnlineVoter
            {
                VoterId = oldPhone,
                VoterIdType = "P",
                WhatsAppStatus = OnlineVoterWhatsAppStatus.Ok
            },
            new OnlineVoter
            {
                VoterId = newPhone,
                VoterIdType = "E",
                WhatsAppStatus = "OK"
            });
        await Context.SaveChangesAsync();

        await _service.UpdatePersonAsync(person.PersonGuid, new UpdatePersonDto
        {
            LastName = "Smith",
            FirstName = "Pat",
            Phone = newPhone
        });

        var occupant = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == newPhone);
        Assert.Equal("E", occupant.VoterIdType);
        Assert.Equal("OK", occupant.WhatsAppStatus);
        Assert.Equal(1, await Context.OnlineVoters.CountAsync(ov => ov.VoterId == newPhone));
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

    [Theory]
    [InlineData("OK")]
    [InlineData("no-wa")]
    public async Task CheckMultipleWhatsAppAsync_SelectedPeople_PersistOkAndNoWa(
        string expectedStatus)
    {
        var electionGuid = Guid.NewGuid();
        var okPerson = await SeedPersonWithPhoneRow(
            "+14168972801", smsStatus: "OK", whatsAppStatus: null, electionGuid: electionGuid);
        var noWaPerson = await SeedPersonWithPhoneRow(
            "+14168972802", smsStatus: "OK", whatsAppStatus: null, electionGuid: electionGuid);
        var persistPerson = expectedStatus == "OK" ? okPerson : noWaPerson;
        var otherPerson = expectedStatus == "OK" ? noWaPerson : okPerson;

        var client = new Mock<IGreenApiWhatsAppClient>();
        client.Setup(c => c.CheckWhatsAppAsync(okPerson.Phone!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.Ok));
        client.Setup(c => c.CheckWhatsAppAsync(noWaPerson.Phone!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.NoWa));
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckMultipleWhatsAppAsync(
            electionGuid,
            [okPerson.PersonGuid, noWaPerson.PersonGuid]);

        Assert.False(result.Cancelled);
        Assert.Equal(2, result.Checked);
        Assert.Equal(1, result.Ok);
        Assert.Equal(1, result.NoWa);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(expectedStatus == "OK" ? WhatsAppCheckOutcome.Ok : WhatsAppCheckOutcome.NoWa,
            result.Results.Single(r => r.PersonGuid == persistPerson.PersonGuid).Outcome);
        Assert.Equal("OK", (await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == okPerson.Phone)).WhatsAppStatus);
        Assert.Equal("no-wa", (await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == noWaPerson.Phone)).WhatsAppStatus);
        Assert.Equal("OK", (await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == otherPerson.Phone)).SmsStatus);
        client.Verify(c => c.CheckWhatsAppAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task CheckMultipleWhatsAppAsync_NonPOccupant_SkippedNotConverted(string existingType)
    {
        var electionGuid = Guid.NewGuid();
        const string phone = "+14168972803";
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
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
            WhatsAppStatus = null,
            SmsStatus = "OK"
        });
        await Context.SaveChangesAsync();
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckMultipleWhatsAppAsync(electionGuid, [person.PersonGuid]);

        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Checked);
        Assert.Equal(WhatsAppCheckOutcome.SkippedNonP, result.Results[0].Outcome);
        var occupant = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal(existingType, occupant.VoterIdType);
        Assert.Null(occupant.WhatsAppStatus);
        Assert.Equal("OK", occupant.SmsStatus);
        client.Verify(c => c.CheckWhatsAppAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_NoPhone_Skipped()
    {
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "Pat",
            Phone = null,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckMultipleWhatsAppAsync(electionGuid, [person.PersonGuid]);

        Assert.Equal(1, result.Skipped);
        Assert.Equal(WhatsAppCheckOutcome.SkippedNoPhone, result.Results[0].Outcome);
        client.Verify(c => c.CheckWhatsAppAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_NotConfigured_DoesNotPersist()
    {
        var electionGuid = Guid.NewGuid();
        var person = await SeedPersonWithPhoneRow(
            "+14168972804", smsStatus: "OK", whatsAppStatus: null, electionGuid: electionGuid);
        var client = MockWhatsAppClient(GreenApiWhatsAppCheckResult.NotConfigured());
        var service = CreateServiceWithWhatsApp(client.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckMultipleWhatsAppAsync(electionGuid, [person.PersonGuid]));

        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppNotConfigured, ex.Message);
        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == person.Phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Null(row.WhatsAppStatus);
        Assert.Equal("OK", row.SmsStatus);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_OverMax_DoesNotCallProvider()
    {
        var tooMany = Enumerable.Range(0, CheckSelectedWhatsAppDto.MaxSelectedPeople + 1)
            .Select(_ => Guid.NewGuid())
            .ToList();
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        var service = CreateServiceWithWhatsApp(client.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckMultipleWhatsAppAsync(Guid.NewGuid(), tooMany));

        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppTooMany, ex.Message);
        client.Verify(c => c.CheckWhatsAppAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_OtherElectionPerson_Ignored()
    {
        var electionGuid = Guid.NewGuid();
        var inElection = await SeedPersonWithPhoneRow(
            "+14168972805", smsStatus: null, whatsAppStatus: null, electionGuid: electionGuid);
        var otherElection = await SeedPersonWithPhoneRow(
            "+14168972806", smsStatus: null, whatsAppStatus: null, electionGuid: Guid.NewGuid());
        var client = MockWhatsAppClient(
            GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.Ok));
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckMultipleWhatsAppAsync(
            electionGuid,
            [inElection.PersonGuid, otherElection.PersonGuid]);

        Assert.Equal(1, result.Checked);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(WhatsAppCheckOutcome.Ok, result.Results[0].Outcome);
        Assert.Equal(WhatsAppCheckOutcome.SkippedOtherElection, result.Results[1].Outcome);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok,
            (await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == inElection.Phone)).WhatsAppStatus);
        Assert.Null((await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == otherElection.Phone)).WhatsAppStatus);
        client.Verify(c => c.CheckWhatsAppAsync(inElection.Phone!, It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.CheckWhatsAppAsync(otherElection.Phone!, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_Cancel_StopsRemainingCalls()
    {
        var electionGuid = Guid.NewGuid();
        var first = await SeedPersonWithPhoneRow(
            "+14168972807", smsStatus: null, whatsAppStatus: null, electionGuid: electionGuid);
        var second = await SeedPersonWithPhoneRow(
            "+14168972808", smsStatus: null, whatsAppStatus: null, electionGuid: electionGuid);
        var cts = new CancellationTokenSource();
        var client = new Mock<IGreenApiWhatsAppClient>();
        client
            .Setup(c => c.CheckWhatsAppAsync(first.Phone!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.Ok))
            .Callback(() => cts.Cancel());
        client
            .Setup(c => c.CheckWhatsAppAsync(second.Phone!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.NoWa));
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckMultipleWhatsAppAsync(
            electionGuid,
            [first.PersonGuid, second.PersonGuid],
            cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(1, result.Checked);
        Assert.Equal(WhatsAppCheckOutcome.Ok, result.Results[0].Outcome);
        Assert.Equal(WhatsAppCheckOutcome.Cancelled, result.Results[1].Outcome);
        Assert.Equal(OnlineVoterWhatsAppStatus.Ok,
            (await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == first.Phone)).WhatsAppStatus);
        Assert.Null((await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == second.Phone)).WhatsAppStatus);
        client.Verify(c => c.CheckWhatsAppAsync(first.Phone!, It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.CheckWhatsAppAsync(second.Phone!, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_SpacesProviderCalls()
    {
        var electionGuid = Guid.NewGuid();
        var first = await SeedPersonWithPhoneRow(
            "+14168972809", smsStatus: null, whatsAppStatus: null, electionGuid: electionGuid);
        var second = await SeedPersonWithPhoneRow(
            "+14168972810", smsStatus: null, whatsAppStatus: null, electionGuid: electionGuid);
        var delays = 0;
        var client = MockWhatsAppClient(
            GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.Ok));
        var service = CreateServiceWithWhatsApp(client.Object, _ =>
        {
            delays++;
            return Task.CompletedTask;
        });

        await service.CheckMultipleWhatsAppAsync(
            electionGuid,
            [first.PersonGuid, second.PersonGuid]);

        Assert.Equal(1, delays);
    }

    [Fact]
    public async Task CheckMultipleWhatsAppAsync_PersistedNoWa_DoesNotClearSmsStatus()
    {
        var electionGuid = Guid.NewGuid();
        var person = await SeedPersonWithPhoneRow(
            "+14168972811", smsStatus: "OK", whatsAppStatus: null, electionGuid: electionGuid);
        var client = MockWhatsAppClient(
            GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.NoWa));
        var service = CreateServiceWithWhatsApp(client.Object);

        var result = await service.CheckMultipleWhatsAppAsync(electionGuid, [person.PersonGuid]);

        Assert.Equal(WhatsAppCheckOutcome.NoWa, result.Results[0].Outcome);
        var row = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == person.Phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal(OnlineVoterWhatsAppStatus.NoWa, row.WhatsAppStatus);
        Assert.Equal("OK", row.SmsStatus);
    }

    private PeopleService CreateServiceWithWhatsApp(
        IGreenApiWhatsAppClient client,
        Func<CancellationToken, Task>? delayBetweenProviderCalls = null) =>
        new(
            Context,
            _loggerMock.Object,
            _signalRMock.Object,
            greenApiWhatsAppClient: client,
            delayBetweenProviderCalls: delayBetweenProviderCalls ?? (_ => Task.CompletedTask));

    private static Mock<IGreenApiWhatsAppClient> MockWhatsAppClient(GreenApiWhatsAppCheckResult result)
    {
        var client = new Mock<IGreenApiWhatsAppClient>();
        client
            .Setup(c => c.CheckWhatsAppAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return client;
    }

    private async Task<Person> SeedPersonWithPhoneRow(
        string phone,
        string? smsStatus,
        DateTimeOffset? whenRegistered = null,
        string? whatsAppStatus = null,
        Guid? electionGuid = null)
    {
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid ?? Guid.NewGuid(),
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
            SmsStatus = smsStatus,
            WhatsAppStatus = whatsAppStatus,
            WhenRegistered = whenRegistered
        });
        await Context.SaveChangesAsync();
        return person;
    }
}



