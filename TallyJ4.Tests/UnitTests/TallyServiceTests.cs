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
        Assert.Equal(9, elected.Count);
        Assert.Equal(3, extra.Count);
        Assert.NotEmpty(other);
        Assert.Equal(15, result.Results.Count);
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

    [Fact]
    public async Task CalculateNormalElectionAsync_CreateResultTieRecords_ForTiesSpanningSections()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 2);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 12);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 4);
        
        await CreateVotesWithTieSpanningSectionsAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();
        
        var resultTies = Context.ResultTies
            .Where(rt => rt.ElectionGuid == election.ElectionGuid)
            .ToList();

        var tiesRequiringBreak = results.Where(r => r.TieBreakRequired == true).ToList();
        Assert.NotEmpty(tiesRequiringBreak);

        Assert.NotEmpty(resultTies);
        Assert.All(resultTies, rt =>
        {
            Assert.Equal(election.ElectionGuid, rt.ElectionGuid);
            Assert.True(rt.TieBreakRequired);
            Assert.True(rt.TieBreakGroup > 0);
            Assert.True(rt.NumInTie > 1);
            Assert.Equal(9, rt.NumToElect);
        });
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithZeroBallots_ReturnsEmptyResults()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(election.ElectionGuid, result.ElectionGuid);
        Assert.Empty(result.Results);
        Assert.Equal(0, result.Statistics.BallotsReceived);
        Assert.Equal(0, result.Statistics.TotalBallots);
        Assert.Equal(0, result.Statistics.TotalVotes);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithAllCandidatesTied_MarksAllAsTied()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 10);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 2);

        await CreateAllCandidatesTiedVotesAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(10, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.IsTied));
        Assert.All(result.Results, r => Assert.Equal(2, r.VoteCount));
        Assert.NotEmpty(result.Ties);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithSingleCandidate_CompletesSuccessfully()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 1);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 5);
        
        foreach (var ballot in ballots)
        {
            var vote = new Vote
            {
                BallotGuid = ballot.BallotGuid,
                PersonGuid = people[0].PersonGuid,
                PositionOnBallot = 1,
                StatusCode = "Ok",
                PersonCombinedInfo = people[0].CombinedInfo,
                RowVersion = new byte[8]
            };
            Context.Votes.Add(vote);
        }
        await Context.SaveChangesAsync();

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Single(result.Results);
        Assert.Equal(5, result.Results[0].VoteCount);
        Assert.Equal("E", result.Results[0].Section);
        Assert.Equal(1, result.Results[0].Rank);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithBallotsHavingZeroValidVotes_CountsBallotsButNotVotes()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 5);
        
        var ballot1 = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = "Spoiled",
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        Context.Ballots.Add(ballot1);

        var ballot2 = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = "Ok",
            ComputerCode = "A",
            BallotNumAtComputer = 2,
            RowVersion = new byte[8]
        };
        Context.Ballots.Add(ballot2);
        await Context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            var vote = new Vote
            {
                BallotGuid = ballot1.BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                StatusCode = "Ok",
                PersonCombinedInfo = people[i].CombinedInfo,
                RowVersion = new byte[8]
            };
            Context.Votes.Add(vote);
        }

        for (int i = 0; i < 3; i++)
        {
            var vote = new Vote
            {
                BallotGuid = ballot2.BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                StatusCode = "Ok",
                PersonCombinedInfo = people[i].CombinedInfo,
                RowVersion = new byte[8]
            };
            Context.Votes.Add(vote);
        }
        await Context.SaveChangesAsync();

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(1, result.Statistics.BallotsReceived);
        Assert.Equal(1, result.Statistics.SpoiledBallots);
        Assert.Equal(2, result.Statistics.TotalBallots);
        
        var candidatesWithVotes = result.Results.Where(r => r.VoteCount > 0).ToList();
        Assert.Equal(3, candidatesWithVotes.Count);
        Assert.All(candidatesWithVotes, c => Assert.Equal(1, c.VoteCount));
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

    private async Task CreateVotesWithTieSpanningSectionsAsync(List<Ballot> ballots, List<Person> people)
    {
        for (int ballotIndex = 0; ballotIndex < ballots.Count; ballotIndex++)
        {
            var ballot = ballots[ballotIndex];
            var positionCounter = 1;
            
            for (int personIndex = 0; personIndex < 6; personIndex++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[personIndex].PersonGuid,
                    PositionOnBallot = positionCounter++,
                    StatusCode = "Ok",
                    PersonCombinedInfo = people[personIndex].CombinedInfo,
                    RowVersion = new byte[8]
                });
            }
        }
        
        for (int ballotIndex = 0; ballotIndex < 3; ballotIndex++)
        {
            var ballot = ballots[ballotIndex];
            var positionCounter = 7;
            
            for (int personIndex = 6; personIndex < 11; personIndex++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[personIndex].PersonGuid,
                    PositionOnBallot = positionCounter++,
                    StatusCode = "Ok",
                    PersonCombinedInfo = people[personIndex].CombinedInfo,
                    RowVersion = new byte[8]
                });
            }
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateAllCandidatesTiedVotesAsync(List<Ballot> ballots, List<Person> people)
    {
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
                    PersonCombinedInfo = people[i].CombinedInfo,
                    RowVersion = new byte[8]
                };

                Context.Votes.Add(vote);
            }
        }

        await Context.SaveChangesAsync();
    }
}
