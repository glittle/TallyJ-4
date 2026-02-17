using Microsoft.Extensions.Logging;
using Moq;
using Backend.Domain.Entities;
using Backend.DTOs.People;
using Backend.Services;
using Backend.Domain.Enumerations;

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
        _service = new PeopleService(Context, Mapper, _loggerMock.Object, _signalRMock.Object);
    }

    [Fact]
    public async Task GetCandidatesAsync_ReturnsOnlyEligiblePeople()
    {
        var electionGuid = Guid.NewGuid();

        var eligiblePerson1 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Alice",
            LastName = "Johnson",
            FullName = "Johnson, Alice",
            FullNameFl = "Alice Johnson",
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };

        var eligiblePerson2 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Bob",
            LastName = "Smith",
            FullName = "Smith, Bob",
            FullNameFl = "Bob Smith",
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        };

        var ineligiblePerson = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Charlie",
            LastName = "Brown",
            FullName = "Brown, Charlie",
            FullNameFl = "Charlie Brown",
            CanReceiveVotes = false,
            CanVote = true,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(eligiblePerson1, eligiblePerson2, ineligiblePerson);
        await Context.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(electionGuid);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.PersonGuid == eligiblePerson1.PersonGuid);
        Assert.Contains(result, p => p.PersonGuid == eligiblePerson2.PersonGuid);
        Assert.DoesNotContain(result, p => p.PersonGuid == ineligiblePerson.PersonGuid);
    }

    [Fact]
    public async Task GetCandidatesAsync_IncludesSoundCodes()
    {
        var electionGuid = Guid.NewGuid();

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "David",
            LastName = "Wilson",
            FullName = "Wilson, David",
            FullNameFl = "David Wilson",
            CanReceiveVotes = true,
            CanVote = true,
            CombinedSoundCodes = "W425|D130",
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(electionGuid);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("W425|D130", result[0].CombinedSoundCodes);
    }

    [Fact]
    public async Task GetCandidatesAsync_OrdersByLastNameFirstName()
    {
        var electionGuid = Guid.NewGuid();

        var person1 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Zoe",
            LastName = "Anderson",
            FullName = "Anderson, Zoe",
            FullNameFl = "Zoe Anderson",
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        var person2 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Alice",
            LastName = "Anderson",
            FullName = "Anderson, Alice",
            FullNameFl = "Alice Anderson",
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        var person3 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Bob",
            LastName = "Baker",
            FullName = "Baker, Bob",
            FullNameFl = "Bob Baker",
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(person1, person2, person3);
        await Context.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(electionGuid);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(person2.PersonGuid, result[0].PersonGuid);
        Assert.Equal(person1.PersonGuid, result[1].PersonGuid);
        Assert.Equal(person3.PersonGuid, result[2].PersonGuid);
    }

    [Fact]
    public async Task GetCandidatesAsync_FiltersMultipleElections()
    {
        var electionGuid1 = Guid.NewGuid();
        var electionGuid2 = Guid.NewGuid();

        var person1 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid1,
            FirstName = "John",
            LastName = "Doe",
            FullName = "Doe, John",
            FullNameFl = "John Doe",
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        var person2 = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid2,
            FirstName = "Jane",
            LastName = "Doe",
            FullName = "Doe, Jane",
            FullNameFl = "Jane Doe",
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.AddRange(person1, person2);
        await Context.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(electionGuid1);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(person1.PersonGuid, result[0].PersonGuid);
    }

    [Fact]
    public async Task GetCandidatesAsync_ReturnsEmptyListWhenNoCandidates()
    {
        var electionGuid = Guid.NewGuid();

        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "Test",
            LastName = "Person",
            FullName = "Person, Test",
            FullNameFl = "Test Person",
            CanReceiveVotes = false,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var result = await _service.GetCandidatesAsync(electionGuid);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreatePersonAsync_WithNullIneligibleReasonGuid_SetsFullEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var createDto = new CreatePersonDto
        {
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            IneligibleReasonGuid = null
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
            IneligibleReasonGuid = IneligibleReasonEnum.X01_Deceased.ReasonGuid
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
            IneligibleReasonGuid = IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid
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
            IneligibleReasonGuid = IneligibleReasonEnum.R01_NotADelegateInThisElection.ReasonGuid
        };

        var result = await _service.CreatePersonAsync(createDto);

        Assert.NotNull(result);
        Assert.False(result.CanVote);
        Assert.True(result.CanReceiveVotes);
        Assert.Equal("R01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ChangingIneligibleReasonGuid_UpdatesEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            FullName = "Smith, John",
            FullNameFl = "John Smith",
            CanVote = true,
            CanReceiveVotes = true,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var updateDto = new UpdatePersonDto
        {
            IneligibleReasonGuid = IneligibleReasonEnum.X01_Deceased.ReasonGuid
        };

        var result = await _service.UpdatePersonAsync(person.PersonGuid, updateDto);

        Assert.NotNull(result);
        Assert.False(result.CanVote);
        Assert.False(result.CanReceiveVotes);
        Assert.Equal("X01", result.IneligibleReasonCode);
    }

    [Fact]
    public async Task UpdatePersonAsync_ClearingIneligibleReasonGuid_RestoresFullEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            LastName = "Smith",
            FirstName = "John",
            FullName = "Smith, John",
            FullNameFl = "John Smith",
            CanVote = false,
            CanReceiveVotes = false,
            IneligibleReasonGuid = IneligibleReasonEnum.X01_Deceased.ReasonGuid,
            RowVersion = new byte[8]
        };

        Context.People.Add(person);
        await Context.SaveChangesAsync();

        var updateDto = new UpdatePersonDto
        {
            IneligibleReasonGuid = null
        };

        var result = await _service.UpdatePersonAsync(person.PersonGuid, updateDto);

        Assert.NotNull(result);
        Assert.True(result.CanVote);
        Assert.True(result.CanReceiveVotes);
        Assert.Null(result.IneligibleReasonCode);
    }
}



