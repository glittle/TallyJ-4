using Microsoft.Extensions.Logging;
using Moq;
using TallyJ4.Domain.Entities;
using TallyJ4.Services;

namespace TallyJ4.Tests.UnitTests;

public class TallyServiceTests : ServiceTestBase
{
    private readonly TallyService _service;
    private readonly Mock<ILogger<TallyService>> _loggerMock;

    public TallyServiceTests()
    {
        _loggerMock = new Mock<ILogger<TallyService>>();
        _service = new TallyService(Context, _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithValidElection_CalculatesCorrectly()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);
        await CreateTestVotesAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(election.ElectionGuid, result.ElectionGuid);
        Assert.NotEmpty(result.Results);
        Assert.True(result.Statistics.TotalBallots > 0);
        Assert.Equal(10, result.Statistics.BallotsReceived);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithInvalidElectionGuid_ThrowsArgumentException()
    {
        var invalidGuid = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CalculateNormalElectionAsync(invalidGuid));
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_DetectsTies()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);
        
        await CreateTiedVotesAsync(ballots, people, 9);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotEmpty(result.Ties);
        var tiedCandidates = result.Results.Where(r => r.IsTied).ToList();
        Assert.NotEmpty(tiedCandidates);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_CategorizesSections()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 20);
        await CreateTestVotesAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var elected = result.Results.Where(r => r.Section == "E").ToList();
        var extra = result.Results.Where(r => r.Section == "X").ToList();
        var other = result.Results.Where(r => r.Section == "O").ToList();

        Assert.NotEmpty(elected);
        Assert.True(elected.All(r => r.Rank <= 9));
        Assert.True(extra.All(r => r.Rank > 9 && r.Rank <= 12));
        Assert.True(other.All(r => r.Rank > 12));
    }

    [Fact]
    public async Task CalculateSingleNameElectionAsync_WithValidElection_CalculatesCorrectly()
    {
        var election = await CreateTestElectionAsync(electionType: "SingleName");
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 5);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);
        await CreateSingleNameVotesAsync(ballots, people);

        var result = await _service.CalculateSingleNameElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(election.ElectionGuid, result.ElectionGuid);
        Assert.NotEmpty(result.Results);
    }

    [Fact]
    public async Task GetTallyResultsAsync_WithExistingResults_ReturnsResults()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);
        await CreateTestVotesAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var result = await _service.GetTallyResultsAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Results);
    }

    [Fact]
    public async Task GetTallyStatisticsAsync_ReturnsCorrectStatistics()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);
        await CreateTestVotesAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var statistics = await _service.GetTallyStatisticsAsync(election.ElectionGuid);

        Assert.NotNull(statistics);
        Assert.Equal(10, statistics.BallotsReceived);
        Assert.Equal(9, statistics.NumberToElect);
        Assert.True(statistics.NumEligibleCandidates > 0);
    }

    private async Task<Election> CreateTestElectionAsync(
        string? electionType = "LSA",
        int numberToElect = 9,
        int numberExtra = 0)
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election",
            ElectionType = electionType,
            NumberToElect = numberToElect,
            NumberExtra = numberExtra,
            TallyStatus = "Setup",
            DateOfElection = DateTime.UtcNow.AddDays(7),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();
        return election;
    }

    private async Task<Location> CreateTestLocationAsync(Guid electionGuid)
    {
        var location = new Location
        {
            ElectionGuid = electionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Test Location"
        };

        Context.Locations.Add(location);
        await Context.SaveChangesAsync();
        return location;
    }

    private async Task<List<Person>> CreateTestPeopleAsync(Guid electionGuid, int count)
    {
        var people = new List<Person>();

        for (int i = 0; i < count; i++)
        {
            var person = new Person
            {
                ElectionGuid = electionGuid,
                PersonGuid = Guid.NewGuid(),
                FirstName = $"Person{i}",
                LastName = $"Test{i}",
                CanReceiveVotes = true,
                CanVote = true,
                CombinedInfo = $"Test{i}, Person{i}",
                RowVersion = new byte[8]
            };

            people.Add(person);
            Context.People.Add(person);
        }

        await Context.SaveChangesAsync();
        return people;
    }

    private async Task<List<Ballot>> CreateTestBallotsAsync(Guid locationGuid, int count)
    {
        var ballots = new List<Ballot>();

        for (int i = 0; i < count; i++)
        {
            var ballot = new Ballot
            {
                LocationGuid = locationGuid,
                BallotGuid = Guid.NewGuid(),
                StatusCode = "Ok",
                ComputerCode = "A",
                BallotNumAtComputer = i + 1,
                RowVersion = new byte[8]
            };

            ballots.Add(ballot);
            Context.Ballots.Add(ballot);
        }

        await Context.SaveChangesAsync();
        return ballots;
    }

    private async Task CreateTestVotesAsync(List<Ballot> ballots, List<Person> people)
    {
        var random = new Random(42);

        foreach (var ballot in ballots)
        {
            var selectedPeople = people.OrderBy(_ => random.Next()).Take(9).ToList();

            for (int i = 0; i < selectedPeople.Count; i++)
            {
                var vote = new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = selectedPeople[i].PersonGuid,
                    PositionOnBallot = i + 1,
                    StatusCode = "Ok",
                    PersonCombinedInfo = selectedPeople[i].CombinedInfo,
                    RowVersion = new byte[8]
                };

                Context.Votes.Add(vote);
            }
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateTiedVotesAsync(List<Ballot> ballots, List<Person> people, int tiePosition)
    {
        for (int ballotIndex = 0; ballotIndex < ballots.Count; ballotIndex++)
        {
            var ballot = ballots[ballotIndex];
            var selectedPeople = people.Take(9).ToList();

            for (int i = 0; i < selectedPeople.Count; i++)
            {
                var vote = new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = selectedPeople[i].PersonGuid,
                    PositionOnBallot = i + 1,
                    StatusCode = "Ok",
                    PersonCombinedInfo = selectedPeople[i].CombinedInfo,
                    RowVersion = new byte[8]
                };

                Context.Votes.Add(vote);
            }
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateSingleNameVotesAsync(List<Ballot> ballots, List<Person> people)
    {
        var random = new Random(42);

        foreach (var ballot in ballots)
        {
            for (int i = 0; i < people.Count; i++)
            {
                var vote = new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[i].PersonGuid,
                    PositionOnBallot = i + 1,
                    StatusCode = "Ok",
                    SingleNameElectionCount = random.Next(1, 6),
                    PersonCombinedInfo = people[i].CombinedInfo,
                    RowVersion = new byte[8]
                };

                Context.Votes.Add(vote);
            }
        }

        await Context.SaveChangesAsync();
    }
}
