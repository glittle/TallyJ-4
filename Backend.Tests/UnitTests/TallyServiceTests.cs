using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Backend;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class TallyServiceTests : ServiceTestBase
{
    private readonly TallyService _service;
    private readonly Mock<ILogger<TallyService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Mock<IComputerAssignmentService> _computerAssignmentMock;
    private readonly Mock<IStringLocalizer<TallyService>> _localizerMock;

    public TallyServiceTests()
    {
        _loggerMock = new Mock<ILogger<TallyService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        _computerAssignmentMock = new Mock<IComputerAssignmentService>();
        _computerAssignmentMock
            .Setup(s => s.GetActiveComputers(It.IsAny<Guid>()))
            .Returns(Array.Empty<Backend.DTOs.Computers.ActiveComputerDto>());
        _localizerMock = new Mock<IStringLocalizer<TallyService>>();
        
        // Setup localizer to return section codes
        _localizerMock.Setup(l => l["tally.section.elected"]).Returns(new LocalizedString("tally.section.elected", "E"));
        _localizerMock.Setup(l => l["tally.section.extra"]).Returns(new LocalizedString("tally.section.extra", "X"));
        _localizerMock.Setup(l => l["tally.section.other"]).Returns(new LocalizedString("tally.section.other", "O"));
        
        _service = new TallyService(
            Context,
            _loggerMock.Object,
            _signalRMock.Object,
            _computerAssignmentMock.Object,
            _localizerMock.Object);
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
    public async Task CalculateNormalElectionAsync_WithReviewBallot_ThrowsInvalidOperationException()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 1);
        ballots[0].StatusCode = BallotStatus.Review;
        await Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CalculateNormalElectionAsync(election.ElectionGuid));

        Assert.Contains("elections.stageChangeError.ballotsOutstanding", exception.Message);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithStaleVerifyStatus_RefreshesAndProceeds()
    {
        var election = await CreateTestElectionAsync(numberToElect: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 1);
        ballots[0].StatusCode = BallotStatus.Verify;
        await Context.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[0].BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[i].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(1, result.Statistics.BallotsReceived);
        Assert.Equal(0, result.Statistics.SpoiledBallots);
        Assert.Equal(BallotStatus.Ok, Context.Ballots.Single().StatusCode);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithStaleSpoiledVote_RefreshesEligibilityBeforeAnalysis()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 1);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 1);

        Context.Votes.Add(new Vote
        {
            BallotGuid = ballots[0].BallotGuid,
            PersonGuid = people[0].PersonGuid,
            PositionOnBallot = 1,
            VoteStatus = VoteStatus.Spoiled,
            IneligibleReasonCode = IneligibleReasonEnum.U01_Unidentifiable.Code,
            PersonCombinedInfo = people[0].CombinedInfo,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(VoteStatus.Ok, Context.Votes.Single().VoteStatus);
        Assert.Null(Context.Votes.Single().IneligibleReasonCode);
        Assert.Equal(1, result.Results.Single().VoteCount);
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
        var tiedPeople = result.Results.Where(r => r.IsTied).ToList();
        Assert.NotEmpty(tiedPeople);
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
    public async Task CalculateNormalElectionAsync_WithNumberExtraZero_DoesNotCreateXSection()
    {
        var election = await CreateTestElectionAsync(numberToElect: 2, numberExtra: 0);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 5);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 15);
        await CreateDescendingVoteDistributionAsync(
            ballots,
            people,
            [5, 4, 3, 2, 1],
            ineligiblePaddingCount: 1);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.DoesNotContain(results, r => r.Section == "X");
        Assert.All(results, r => Assert.Null(r.RankInExtra));
        Assert.Equal(2, results.Count(r => r.Section == "E"));
        Assert.Equal(3, results.Count(r => r.Section == "O"));
    }

    [Fact]
    public async Task CalculateSingleNameElectionAsync_WithValidElection_CalculatesCorrectly()
    {
        var election = await CreateTestElectionAsync(electionType: "Oth");
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
        Assert.True(statistics.NumEligiblePeople > 0);
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
            Assert.True(rt.TieBreakGroup > 0);
            Assert.True(rt.NumInTie > 1);
        });

        var sectionSpanningTies = resultTies.Where(rt => rt.TieBreakRequired == true).ToList();
        Assert.NotEmpty(sectionSpanningTies);
        Assert.All(sectionSpanningTies, rt => Assert.True(rt.NumToElect > 0));
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
    public async Task CalculateNormalElectionAsync_WithAllPeopleTied_MarksAllAsTied()
    {
        var election = await CreateTestElectionAsync(numberToElect: 10);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 10);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 2);

        await CreateAllPeopleTiedVotesAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(10, result.Results.Count);
        Assert.All(result.Results, r => Assert.True(r.IsTied));
        Assert.All(result.Results, r => Assert.Equal(2, r.VoteCount));
        Assert.NotEmpty(result.Ties);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_WithSinglePerson_CompletesSuccessfully()
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
                VoteStatus = VoteStatus.Ok,
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
        var election = await CreateTestElectionAsync(numberToElect: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 5);

        var ballot1 = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.TooFew,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };
        Context.Ballots.Add(ballot1);

        var ballot2 = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 2,
            RowVersion = new byte[8]
        };
        Context.Ballots.Add(ballot2);
        await Context.SaveChangesAsync();

        for (int i = 0; i < 2; i++)
        {
            var vote = new Vote
            {
                BallotGuid = ballot1.BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                VoteStatus = VoteStatus.Ok,
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
                VoteStatus = VoteStatus.Ok,
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

        var peopleWithVotes = result.Results.Where(r => r.VoteCount > 0).ToList();
        Assert.Equal(3, peopleWithVotes.Count);
        Assert.All(peopleWithVotes, c => Assert.Equal(1, c.VoteCount));
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_Recalculation_ProducesIdenticalResults()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 15);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 20);
        await CreateTestVotesAsync(ballots, people);

        var result1 = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var result2 = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Results.Count, result2.Results.Count);

        var sortedResults1 = result1.Results.OrderBy(r => r.PersonGuid).ToList();
        var sortedResults2 = result2.Results.OrderBy(r => r.PersonGuid).ToList();

        for (int i = 0; i < sortedResults1.Count; i++)
        {
            var r1 = sortedResults1[i];
            var r2 = sortedResults2[i];

            Assert.Equal(r1.PersonGuid, r2.PersonGuid);
            Assert.Equal(r1.VoteCount, r2.VoteCount);
            Assert.Equal(r1.Rank, r2.Rank);
            Assert.Equal(r1.Section, r2.Section);
            Assert.Equal(r1.IsTied, r2.IsTied);
            Assert.Equal(r1.TieBreakGroup, r2.TieBreakGroup);
        }

        Assert.Equal(result1.Statistics.BallotsReceived, result2.Statistics.BallotsReceived);
        Assert.Equal(result1.Statistics.TotalBallots, result2.Statistics.TotalBallots);
        Assert.Equal(result1.Statistics.TotalVotes, result2.Statistics.TotalVotes);

        var resultRecordsInDb = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .ToList();
        Assert.Equal(15, resultRecordsInDb.Count);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_TieSpanningElectedExtraBoundary_RequiresTieBreak()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 2);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 12);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);

        await CreateVotesWithTieAtElectedExtraBoundaryAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = result.Results.OrderBy(r => r.Rank).ToList();
        var tiedAtBoundary = results.Where(r => r.Rank == 9 || r.Rank == 10).ToList();

        Assert.True(tiedAtBoundary.All(r => r.TieBreakRequired));
        Assert.True(tiedAtBoundary.All(r => r.IsTied));
        Assert.True(tiedAtBoundary.All(r => r.TieBreakGroup > 0));

        var resultTies = Context.ResultTies
            .Where(rt => rt.ElectionGuid == election.ElectionGuid && rt.TieBreakRequired == true)
            .ToList();
        Assert.NotEmpty(resultTies);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_TieSpanningExtraOtherBoundary_RequiresTieBreak()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 2);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 13);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);

        await CreateVotesWithTieAtExtraOtherBoundaryAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = result.Results.OrderBy(r => r.Rank).ToList();
        var tiedResults = results.Where(r => r.IsTied).ToList();

        Assert.NotEmpty(tiedResults);
        Assert.True(tiedResults.All(r => r.TieBreakGroup > 0));

        var resultTies = Context.ResultTies
            .Where(rt => rt.ElectionGuid == election.ElectionGuid && rt.TieBreakRequired == true)
            .ToList();
        Assert.NotEmpty(resultTies);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_TieWithinExtraSectionOnly_RequiresTieBreak()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1, numberExtra: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);

        await CreateVotesWithTieWithinExtraSectionAsync(ballots, people.Take(3).ToList());

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(3, results.Count);

        Assert.Equal(people[0].PersonGuid, results[0].PersonGuid);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("E", results[0].Section);
        Assert.False(results[0].IsTied);

        var tiedInExtra = results.Where(r => r.Section == "X" && r.IsTied == true).ToList();
        Assert.Equal(2, tiedInExtra.Count);
        Assert.All(tiedInExtra, r => Assert.True(r.TieBreakRequired));
        Assert.All(tiedInExtra, r => Assert.Equal(tiedInExtra[0].TieBreakGroup, r.TieBreakGroup));
        Assert.Contains(people[1].PersonGuid, tiedInExtra.Select(r => r.PersonGuid));
        Assert.Contains(people[2].PersonGuid, tiedInExtra.Select(r => r.PersonGuid));
        Assert.Equal(1, tiedInExtra[0].RankInExtra);
        Assert.Equal(2, tiedInExtra[1].RankInExtra);

        var resultTies = Context.ResultTies
            .Where(rt => rt.ElectionGuid == election.ElectionGuid)
            .ToList();
        Assert.Single(resultTies);
        Assert.True(resultTies[0].TieBreakRequired);
        Assert.Equal(2, resultTies[0].NumInTie);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(false, resultTies[0].IsResolved);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_LsaScale_TieWithinExtraSectionOnly_RequiresTieBreak()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 2);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 12);
        var voteCounts = new[] { 13, 12, 11, 10, 9, 8, 7, 6, 5, 2, 2, 1 };
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, voteCounts.Sum());
        await CreateDescendingVoteDistributionAsync(
            ballots,
            people,
            voteCounts,
            ineligiblePaddingCount: 8);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(12, results.Count);

        var elected = results.Where(r => r.Section == "E").ToList();
        var extra = results.Where(r => r.Section == "X").ToList();
        var other = results.Where(r => r.Section == "O").ToList();

        Assert.Equal(9, elected.Count);
        Assert.Equal(2, extra.Count);
        Assert.Single(other);
        Assert.All(elected, r => Assert.False(r.IsTied));
        Assert.All(elected, r => Assert.False(r.TieBreakRequired));

        Assert.All(extra, r => Assert.True(r.IsTied));
        Assert.All(extra, r => Assert.True(r.TieBreakRequired));
        Assert.All(extra, r => Assert.Equal(extra[0].TieBreakGroup, r.TieBreakGroup));
        Assert.Equal(1, extra[0].RankInExtra);
        Assert.Equal(2, extra[1].RankInExtra);
        Assert.Contains(people[9].PersonGuid, extra.Select(r => r.PersonGuid));
        Assert.Contains(people[10].PersonGuid, extra.Select(r => r.PersonGuid));

        Assert.Equal(people[11].PersonGuid, other[0].PersonGuid);
        Assert.False(other[0].IsTied);
        Assert.False(other[0].TieBreakRequired);

        var resultTies = Context.ResultTies
            .Where(rt => rt.ElectionGuid == election.ElectionGuid)
            .ToList();
        Assert.Single(resultTies);
        Assert.True(resultTies[0].TieBreakRequired);
        Assert.Equal(2, resultTies[0].NumInTie);
        Assert.Equal(1, resultTies[0].NumToElect);
        Assert.Equal(false, resultTies[0].IsResolved);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_TieWithinSection_DoesNotRequireTieBreak()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 2);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 12);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 10);

        await CreateVotesWithTieWithinElectedSectionAsync(ballots, people);

        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = result.Results.OrderBy(r => r.Rank).ToList();
        var tiedWithinElected = results.Where(r => r.Rank >= 7 && r.Rank <= 8 && r.Section == "E").ToList();

        Assert.True(tiedWithinElected.All(r => !r.TieBreakRequired));
        Assert.True(tiedWithinElected.All(r => r.IsTied));

        var resultTies = Context.ResultTies
            .Where(rt => rt.ElectionGuid == election.ElectionGuid && rt.TieBreakRequired == true)
            .ToList();
        Assert.Empty(resultTies);
    }

    [Fact]
    public async Task GetTiesAsync_ReturnsTiedPeopleAndCounts()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1, numberExtra: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 3);
        await CreateThreeWayEqualVoteAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var tiedResults = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid && r.IsTied == true)
            .ToList();
        Assert.Equal(3, tiedResults.Count);

        var tieBreakGroup = tiedResults[0].TieBreakGroup!.Value;
        tiedResults.First(r => r.PersonGuid == people[0].PersonGuid).TieBreakCount = 2;
        tiedResults.First(r => r.PersonGuid == people[1].PersonGuid).TieBreakCount = 1;
        tiedResults.First(r => r.PersonGuid == people[2].PersonGuid).TieBreakCount = 5;
        await Context.SaveChangesAsync();

        var tieDetails = await _service.GetTiesAsync(election.ElectionGuid, tieBreakGroup);

        Assert.Equal(tieBreakGroup, tieDetails.TieBreakGroup);
        Assert.Equal(3, tieDetails.People.Count);

        foreach (var person in tieDetails.People)
        {
            var dbResult = tiedResults.First(r => r.PersonGuid == person.PersonGuid);
            Assert.Equal(people.First(p => p.PersonGuid == person.PersonGuid).FullNameFl, person.FullName);
            Assert.Equal(dbResult.VoteCount ?? 0, person.VoteCount);
            Assert.Equal(dbResult.TieBreakCount, person.TieBreakCount);
        }
    }

    [Fact]
    public async Task SaveTieCountsAsync_TriggersReanalysisAndReordersPeople()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1, numberExtra: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 3);
        await CreateThreeWayEqualVoteAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var before = (await _service.GetTallyResultsAsync(election.ElectionGuid)).Results
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(3, before.Count);
        Assert.Equal(people[0].PersonGuid, before[0].PersonGuid);
        Assert.Equal("E", before[0].Section);
        Assert.Equal(people[1].PersonGuid, before[1].PersonGuid);
        Assert.Equal("X", before[1].Section);
        Assert.Equal(people[2].PersonGuid, before[2].PersonGuid);
        Assert.Equal("O", before[2].Section);

        var response = await _service.SaveTieCountsAsync(election.ElectionGuid, new SaveTieCountsRequestDto
        {
            Counts =
            [
                new TieCountDto { PersonGuid = people[2].PersonGuid, TieBreakCount = 5 },
                new TieCountDto { PersonGuid = people[0].PersonGuid, TieBreakCount = 2 },
                new TieCountDto { PersonGuid = people[1].PersonGuid, TieBreakCount = 1 },
            ]
        });

        Assert.True(response.Success);
        Assert.True(response.ReAnalysisTriggered);

        var after = (await _service.GetTallyResultsAsync(election.ElectionGuid)).Results
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(people[2].PersonGuid, after[0].PersonGuid);
        Assert.Equal(1, after[0].Rank);
        Assert.Equal("E", after[0].Section);

        Assert.Equal(people[0].PersonGuid, after[1].PersonGuid);
        Assert.Equal(2, after[1].Rank);
        Assert.Equal("X", after[1].Section);

        Assert.Equal(people[1].PersonGuid, after[2].PersonGuid);
        Assert.Equal(3, after[2].Rank);
        Assert.Equal("O", after[2].Section);

        var dbResults = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();
        Assert.Equal(5, dbResults[0].TieBreakCount);
        Assert.Equal(2, dbResults[1].TieBreakCount);
        Assert.Equal(1, dbResults[2].TieBreakCount);
    }

    [Fact]
    public async Task SaveTieCountsAsync_UnresolvedCountsStillReordersButStaysUnresolved()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1, numberExtra: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 3);
        await CreateThreeWayEqualVoteAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var response = await _service.SaveTieCountsAsync(election.ElectionGuid, new SaveTieCountsRequestDto
        {
            Counts =
            [
                new TieCountDto { PersonGuid = people[0].PersonGuid, TieBreakCount = 2 },
                new TieCountDto { PersonGuid = people[1].PersonGuid, TieBreakCount = 1 },
                new TieCountDto { PersonGuid = people[2].PersonGuid, TieBreakCount = 1 },
            ]
        });

        Assert.True(response.Success);
        Assert.True(response.ReAnalysisTriggered);

        var after = (await _service.GetTallyResultsAsync(election.ElectionGuid)).Results
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(people[0].PersonGuid, after[0].PersonGuid);
        Assert.Equal("E", after[0].Section);
        Assert.Equal(people[1].PersonGuid, after[1].PersonGuid);
        Assert.Equal("X", after[1].Section);
        Assert.Equal(people[2].PersonGuid, after[2].PersonGuid);
        Assert.Equal("O", after[2].Section);

        var resultTie = Context.ResultTies.Single(rt => rt.ElectionGuid == election.ElectionGuid);
        Assert.Equal(false, resultTie.IsResolved);
    }

    [Fact]
    public async Task SaveTieCountsAsync_ResolvedTie_SetsUseOnReportsAfterReanalysis()
    {
        var election = await CreateTestElectionAsync(numberToElect: 1, numberExtra: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        foreach (var person in people)
        {
            person.VotingMethod = "P";
        }

        await Context.SaveChangesAsync();

        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 3);
        await CreateThreeWayEqualVoteAsync(ballots, people);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var summaryBefore = Context.ResultSummaries
            .First(rs => rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "F");
        var resultTieBefore = Context.ResultTies.Single(rt => rt.ElectionGuid == election.ElectionGuid);
        Assert.Equal(false, resultTieBefore.IsResolved);
        Assert.Equal(false, summaryBefore.UseOnReports);

        var response = await _service.SaveTieCountsAsync(election.ElectionGuid, new SaveTieCountsRequestDto
        {
            Counts =
            [
                new TieCountDto { PersonGuid = people[2].PersonGuid, TieBreakCount = 5 },
                new TieCountDto { PersonGuid = people[0].PersonGuid, TieBreakCount = 2 },
                new TieCountDto { PersonGuid = people[1].PersonGuid, TieBreakCount = 1 },
            ]
        });

        Assert.True(response.Success);
        Assert.True(response.ReAnalysisTriggered);

        var summaryAfter = Context.ResultSummaries
            .First(rs => rs.ElectionGuid == election.ElectionGuid && rs.ResultType == "F");
        var resultTieAfter = Context.ResultTies.Single(rt => rt.ElectionGuid == election.ElectionGuid);
        Assert.Equal(true, resultTieAfter.IsResolved);
        Assert.Equal(true, summaryAfter.UseOnReports);
    }

    [Fact]
    public async Task SaveTieCountsAsync_SingleNameElection_TriggersReanalysisAndReordersPeople()
    {
        var election = await CreateTestElectionAsync(electionType: "Oth", numberToElect: 1, numberExtra: 1);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 3);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 1);

        for (var i = 0; i < 3; i++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[0].BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = i + 1,
                VoteStatus = VoteStatus.Ok,
                SingleNameElectionCount = 10,
                PersonCombinedInfo = people[i].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
        await _service.CalculateSingleNameElectionAsync(election.ElectionGuid);

        var before = (await _service.GetTallyResultsAsync(election.ElectionGuid)).Results
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(3, before.Count);
        Assert.Equal(people[0].PersonGuid, before[0].PersonGuid);
        Assert.Equal("E", before[0].Section);
        Assert.Equal(people[1].PersonGuid, before[1].PersonGuid);
        Assert.Equal("X", before[1].Section);
        Assert.Equal(people[2].PersonGuid, before[2].PersonGuid);
        Assert.Equal("O", before[2].Section);

        var response = await _service.SaveTieCountsAsync(election.ElectionGuid, new SaveTieCountsRequestDto
        {
            Counts =
            [
                new TieCountDto { PersonGuid = people[2].PersonGuid, TieBreakCount = 5 },
                new TieCountDto { PersonGuid = people[0].PersonGuid, TieBreakCount = 2 },
                new TieCountDto { PersonGuid = people[1].PersonGuid, TieBreakCount = 1 },
            ]
        });

        Assert.True(response.Success);
        Assert.True(response.ReAnalysisTriggered);

        var after = (await _service.GetTallyResultsAsync(election.ElectionGuid)).Results
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(people[2].PersonGuid, after[0].PersonGuid);
        Assert.Equal(1, after[0].Rank);
        Assert.Equal("E", after[0].Section);

        Assert.Equal(people[0].PersonGuid, after[1].PersonGuid);
        Assert.Equal(2, after[1].Rank);
        Assert.Equal("X", after[1].Section);

        Assert.Equal(people[1].PersonGuid, after[2].PersonGuid);
        Assert.Equal(3, after[2].Rank);
        Assert.Equal("O", after[2].Section);

        var dbResults = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();
        Assert.Equal(5, dbResults[0].TieBreakCount);
        Assert.Equal(2, dbResults[1].TieBreakCount);
        Assert.Equal(1, dbResults[2].TieBreakCount);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_ExtrasAssignRankInExtraSequentially()
    {
        var election = await CreateTestElectionAsync(numberToElect: 2, numberExtra: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 5);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 15);
        await CreateDescendingVoteDistributionAsync(ballots, people, new[] { 5, 4, 3, 2, 1 }, ineligiblePaddingCount: 1);

        await _service.CalculateNormalElectionAsync(election.ElectionGuid);

        var results = Context.Results
            .Where(r => r.ElectionGuid == election.ElectionGuid)
            .OrderBy(r => r.Rank)
            .ToList();

        Assert.Equal(5, results.Count);

        Assert.Equal("E", results[0].Section);
        Assert.Null(results[0].RankInExtra);
        Assert.Equal("E", results[1].Section);
        Assert.Null(results[1].RankInExtra);

        Assert.Equal("X", results[2].Section);
        Assert.Equal(1, results[2].RankInExtra);
        Assert.Equal("X", results[3].Section);
        Assert.Equal(2, results[3].RankInExtra);
        Assert.Equal("X", results[4].Section);
        Assert.Equal(3, results[4].RankInExtra);
    }

    [Fact]
    public async Task CalculateNormalElectionAsync_PerformanceTest_CompletesWithin1Second()
    {
        var election = await CreateTestElectionAsync(numberToElect: 9, numberExtra: 3);
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 30);
        var ballots = await CreateTestBallotsAsync(location.LocationGuid, 100);
        await CreateTestVotesAsync(ballots, people);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.CalculateNormalElectionAsync(election.ElectionGuid);
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.Equal(100, result.Statistics.BallotsReceived);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Tally calculation took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
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
            ElectionStage = Backend.Enumerations.ElectionStage.SettingUp,
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
                StatusCode = BallotStatus.Ok,
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
                    VoteStatus = VoteStatus.Ok,
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
                    VoteStatus = VoteStatus.Ok,
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
                    VoteStatus = VoteStatus.Ok,
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
        foreach (var ballot in ballots)
        {
            var positionCounter = 1;
            for (int personIndex = 0; personIndex < 8; personIndex++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[personIndex].PersonGuid,
                    PositionOnBallot = positionCounter++,
                    VoteStatus = VoteStatus.Ok,
                    PersonCombinedInfo = people[personIndex].CombinedInfo,
                    RowVersion = new byte[8]
                });
            }
        }

        var halfBallots = ballots.Count / 2;
        for (int ballotIndex = 0; ballotIndex < halfBallots; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[8].PersonGuid,
                PositionOnBallot = 9,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[8].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        for (int ballotIndex = halfBallots; ballotIndex < ballots.Count; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[9].PersonGuid,
                PositionOnBallot = 9,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[9].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateAllPeopleTiedVotesAsync(List<Ballot> ballots, List<Person> people)
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
                    VoteStatus = VoteStatus.Ok,
                    PersonCombinedInfo = people[i].CombinedInfo,
                    RowVersion = new byte[8]
                };

                Context.Votes.Add(vote);
            }
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateVotesWithTieAtElectedExtraBoundaryAsync(List<Ballot> ballots, List<Person> people)
    {
        foreach (var ballot in ballots)
        {
            for (int i = 0; i < 8; i++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[i].PersonGuid,
                    PositionOnBallot = i + 1,
                    VoteStatus = VoteStatus.Ok,
                    PersonCombinedInfo = people[i].CombinedInfo,
                    RowVersion = new byte[8]
                });
            }
        }

        var halfBallots = ballots.Count / 2;
        for (int ballotIndex = 0; ballotIndex < halfBallots; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[8].PersonGuid,
                PositionOnBallot = 9,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[8].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        for (int ballotIndex = halfBallots; ballotIndex < ballots.Count; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[9].PersonGuid,
                PositionOnBallot = 9,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[9].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateVotesWithTieAtExtraOtherBoundaryAsync(List<Ballot> ballots, List<Person> people)
    {
        foreach (var ballot in ballots)
        {
            var positionCounter = 1;
            for (int i = 0; i < 8; i++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[i].PersonGuid,
                    PositionOnBallot = positionCounter++,
                    VoteStatus = VoteStatus.Ok,
                    PersonCombinedInfo = people[i].CombinedInfo,
                    RowVersion = new byte[8]
                });
            }
        }

        var halfBallots = ballots.Count / 2;
        for (int ballotIndex = 0; ballotIndex < halfBallots; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[8].PersonGuid,
                PositionOnBallot = 9,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[8].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        for (int ballotIndex = halfBallots; ballotIndex < ballots.Count; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[9].PersonGuid,
                PositionOnBallot = 9,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[9].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateVotesWithTieWithinElectedSectionAsync(List<Ballot> ballots, List<Person> people)
    {
        foreach (var ballot in ballots)
        {
            for (int i = 0; i < 6; i++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PersonGuid = people[i].PersonGuid,
                    PositionOnBallot = i + 1,
                    VoteStatus = VoteStatus.Ok,
                    PersonCombinedInfo = people[i].CombinedInfo,
                    RowVersion = new byte[8]
                });
            }
        }

        var halfBallots = ballots.Count / 2;
        for (int ballotIndex = 0; ballotIndex < halfBallots; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[6].PersonGuid,
                PositionOnBallot = 7,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[6].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        for (int ballotIndex = halfBallots; ballotIndex < ballots.Count; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[7].PersonGuid,
                PositionOnBallot = 7,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[7].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        foreach (var ballot in ballots)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballot.BallotGuid,
                PersonGuid = people[8].PersonGuid,
                PositionOnBallot = 8,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[8].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
    }

    private async Task CreateThreeWayEqualVoteAsync(List<Ballot> ballots, List<Person> people)
    {
        for (var i = 0; i < 3; i++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[i].BallotGuid,
                PersonGuid = people[i].PersonGuid,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[i].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// <paramref name="ineligiblePaddingCount"/> extra ineligible placeholder votes per ballot
    /// (null PersonGuid + ineligible reason, VoteStatus.Ok) so the ballot stays Ok.
    /// Default 0 adds no fillers.
    /// </summary>
    private async Task CreateDescendingVoteDistributionAsync(
        List<Ballot> ballots,
        List<Person> people,
        int[] voteCounts,
        int ineligiblePaddingCount = 0)
    {
        var ballotIndex = 0;
        for (var p = 0; p < voteCounts.Length; p++)
        {
            for (var v = 0; v < voteCounts[p]; v++)
            {
                Context.Votes.Add(new Vote
                {
                    BallotGuid = ballots[ballotIndex].BallotGuid,
                    PersonGuid = people[p].PersonGuid,
                    PositionOnBallot = 1,
                    VoteStatus = VoteStatus.Ok,
                    PersonCombinedInfo = people[p].CombinedInfo,
                    RowVersion = new byte[8]
                });

                for (var slot = 0; slot < ineligiblePaddingCount; slot++)
                {
                    Context.Votes.Add(new Vote
                    {
                        BallotGuid = ballots[ballotIndex].BallotGuid,
                        PersonGuid = null,
                        IneligibleReasonCode = IneligibleReasonEnum.U01_Unidentifiable.Code,
                        PositionOnBallot = slot + 2,
                        VoteStatus = VoteStatus.Ok,
                        RowVersion = new byte[8]
                    });
                }

                ballotIndex++;
            }
        }

        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Builds a tie confined to the extra section: person0 is elected, persons 1 and 2 tie within X.
    /// Requires numberToElect=1, single-vote ballots, and at least 10 ballots.
    /// Vote totals: person0=4, person1=3, person2=3 (ballot ranges do not overlap).
    /// </summary>
    private async Task CreateVotesWithTieWithinExtraSectionAsync(List<Ballot> ballots, List<Person> people)
    {
        for (var ballotIndex = 0; ballotIndex < 4; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[0].PersonGuid,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[0].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        for (var ballotIndex = 4; ballotIndex < 7; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[1].PersonGuid,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[1].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        for (var ballotIndex = 7; ballotIndex < 10; ballotIndex++)
        {
            Context.Votes.Add(new Vote
            {
                BallotGuid = ballots[ballotIndex].BallotGuid,
                PersonGuid = people[2].PersonGuid,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                PersonCombinedInfo = people[2].CombinedInfo,
                RowVersion = new byte[8]
            });
        }

        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMonitorInfoAsync_DerivesLocationAndBallotCountFromLatestBallotPerComputer()
    {
        var election = await CreateTestElectionAsync();
        var locationA = new Location
        {
            ElectionGuid = election.ElectionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Hall A",
        };
        var locationB = new Location
        {
            ElectionGuid = election.ElectionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Hall B",
        };
        Context.Locations.AddRange(locationA, locationB);
        await Context.SaveChangesAsync();

        var olderBallot = new Ballot
        {
            LocationGuid = locationA.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            DateCreated = DateTimeOffset.UtcNow.AddHours(-2),
            RowVersion = new byte[8],
        };
        var newerBallot = new Ballot
        {
            LocationGuid = locationB.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = "A",
            BallotNumAtComputer = 2,
            DateCreated = DateTimeOffset.UtcNow.AddMinutes(-10),
            RowVersion = new byte[8],
        };
        Context.Ballots.AddRange(olderBallot, newerBallot);
        await Context.SaveChangesAsync();

        var connectedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        _computerAssignmentMock
            .Setup(s => s.GetActiveComputers(election.ElectionGuid))
            .Returns(new[]
            {
                new Backend.DTOs.Computers.ActiveComputerDto
                {
                    ComputerCode = "A",
                    ConnectedAt = connectedAt,
                },
            });

        var result = await _service.GetMonitorInfoAsync(election.ElectionGuid);

        Assert.Single(result.Computers);
        var computer = result.Computers[0];
        Assert.Equal("A", computer.ComputerCode);
        Assert.Equal("Hall B", computer.LocationName);
        Assert.Equal(2, computer.BallotCount);
        Assert.Equal(connectedAt, computer.LastContact);
        Assert.Equal("Active", computer.Status);
    }

    [Fact]
    public async Task GetMonitorInfoAsync_UsesAssignedLocationWhenComputerHasNoBallots()
    {
        var election = await CreateTestElectionAsync();
        var location = await CreateTestLocationAsync(election.ElectionGuid);
        location.Name = "Assigned Hall";
        await Context.SaveChangesAsync();
        Context.Computers.Add(new Computer
        {
            ElectionGuid = election.ElectionGuid,
            LocationGuid = location.LocationGuid,
            ComputerGuid = Guid.NewGuid(),
            ComputerCode = "B",
        });
        await Context.SaveChangesAsync();

        _computerAssignmentMock
            .Setup(s => s.GetActiveComputers(election.ElectionGuid))
            .Returns(new[]
            {
                new Backend.DTOs.Computers.ActiveComputerDto
                {
                    ComputerCode = "B",
                    ConnectedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                },
            });

        var result = await _service.GetMonitorInfoAsync(election.ElectionGuid);

        Assert.Single(result.Computers);
        Assert.Equal("Assigned Hall", result.Computers[0].LocationName);
        Assert.Equal(0, result.Computers[0].BallotCount);
        Assert.Equal("Inactive", result.Computers[0].Status);
    }

    [Fact]
    public async Task GetMonitorInfoAsync_IncludesAcceptAllRuns_NewestFirst()
    {
        var election = await CreateTestElectionAsync();
        var older = new SecurityAuditLog
        {
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            EventType = SecurityEventType.OperationalActivity,
            ElectionGuid = election.ElectionGuid,
            UserId = "teller-1",
            Details = AcceptAllOnlineBallotsAudit.FormatDetails(2, 0, 0, 2),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(
                AcceptAllOnlineBallotsAudit.FormatMetadata(2, 0, 0, 2, "Jane")),
            Severity = SecurityEventSeverity.Info
        };
        var newer = new SecurityAuditLog
        {
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5),
            EventType = SecurityEventType.OperationalActivity,
            ElectionGuid = election.ElectionGuid,
            UserId = "teller-2",
            Details = AcceptAllOnlineBallotsAudit.FormatDetails(1, 2, 0, 3),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(
                AcceptAllOnlineBallotsAudit.FormatMetadata(1, 2, 0, 3, "Sam")),
            Severity = SecurityEventSeverity.Info
        };
        Context.SecurityAuditLogs.AddRange(
            older,
            newer,
            new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventType = SecurityEventType.OperationalActivity,
                ElectionGuid = election.ElectionGuid,
                Details = "Ballot entry began",
                Severity = SecurityEventSeverity.Info
            });
        await Context.SaveChangesAsync();

        var result = await _service.GetMonitorInfoAsync(election.ElectionGuid);

        Assert.Equal(2, result.OnlineVotingInfo.AcceptAllRuns.Count);
        Assert.Equal("teller-2", result.OnlineVotingInfo.AcceptAllRuns[0].AcceptedByUserId);
        Assert.Equal("Sam", result.OnlineVotingInfo.AcceptAllRuns[0].AcceptedBy);
        Assert.Equal(1, result.OnlineVotingInfo.AcceptAllRuns[0].PendingBefore);
        Assert.Equal(2, result.OnlineVotingInfo.AcceptAllRuns[0].AcceptedBefore);
        Assert.Equal(0, result.OnlineVotingInfo.AcceptAllRuns[0].PendingAfter);
        Assert.Equal(3, result.OnlineVotingInfo.AcceptAllRuns[0].AcceptedAfter);
        Assert.Equal("teller-1", result.OnlineVotingInfo.AcceptAllRuns[1].AcceptedByUserId);
        Assert.Equal("Jane", result.OnlineVotingInfo.AcceptAllRuns[1].AcceptedBy);
    }

    [Fact]
    public async Task GetMonitorInfoAsync_SplitsPendingAndAcceptedOnlineBallotsByStoredStatus()
    {
        var election = await CreateTestElectionAsync();
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 4);
        people[0].FirstName = "Ada";
        people[0].LastName = "Submitted";
        people[1].FirstName = "Bea";
        people[1].LastName = "Processing";
        people[2].FirstName = "Cara";
        people[2].LastName = "Accepted";
        people[3].FirstName = "Dee";
        people[3].LastName = "LaterSubmit";

        var submittedAt = DateTimeOffset.UtcNow.AddHours(-3);
        var processingAt = DateTimeOffset.UtcNow.AddHours(-2);
        var acceptedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var laterSubmitAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        Context.OnlineVotingInfos.AddRange(
            new OnlineVotingInfo
            {
                ElectionGuid = election.ElectionGuid,
                PersonGuid = people[0].PersonGuid,
                Status = OnlineBallotStatus.Submitted,
                WhenStatus = submittedAt,
                ListPool = "[1,2]",
                PoolLocked = true
            },
            new OnlineVotingInfo
            {
                ElectionGuid = election.ElectionGuid,
                PersonGuid = people[1].PersonGuid,
                Status = OnlineBallotStatus.Processing,
                WhenStatus = processingAt,
                ListPool = "[3]",
                PoolLocked = true
            },
            new OnlineVotingInfo
            {
                ElectionGuid = election.ElectionGuid,
                PersonGuid = people[2].PersonGuid,
                Status = OnlineBallotStatus.Processed,
                WhenStatus = acceptedAt,
                ListPool = null,
                PoolLocked = null,
                BallotGuid = null
            },
            new OnlineVotingInfo
            {
                ElectionGuid = election.ElectionGuid,
                PersonGuid = people[3].PersonGuid,
                Status = OnlineBallotStatus.Submitted,
                WhenStatus = laterSubmitAt,
                ListPool = "[9]",
                PoolLocked = true
            });
        await Context.SaveChangesAsync();

        var result = await _service.GetMonitorInfoAsync(election.ElectionGuid);
        var online = result.OnlineVotingInfo;

        Assert.Equal(4, online.TotalOnlineBallots);
        Assert.Equal(3, online.PendingOnlineBallots);
        Assert.Equal(1, online.ProcessedOnlineBallots);

        Assert.Equal(3, online.PendingBallots.Count);
        Assert.Equal(new[]
        {
            OnlineBallotStatus.Submitted,
            OnlineBallotStatus.Processing,
            OnlineBallotStatus.Submitted
        }, online.PendingBallots.Select(b => b.Status).OrderBy(s => s).ToArray());
        Assert.Contains(online.PendingBallots, b =>
            b.PersonName.Contains("Submitted") && b.Status == OnlineBallotStatus.Submitted);
        Assert.Contains(online.PendingBallots, b =>
            b.PersonName.Contains("Processing") && b.Status == OnlineBallotStatus.Processing);
        Assert.Contains(online.PendingBallots, b =>
            b.PersonName.Contains("LaterSubmit") && b.Status == OnlineBallotStatus.Submitted);
        Assert.Equal("LaterSubmit, Dee", online.PendingBallots[0].PersonName);
        Assert.DoesNotContain(online.PendingBallots, b => b.PersonName.Contains("Accepted"));

        Assert.Single(online.AcceptedBallots);
        Assert.Equal(OnlineBallotStatus.Processed, online.AcceptedBallots[0].Status);
        Assert.Equal("Accepted, Cara", online.AcceptedBallots[0].PersonName);
        Assert.Equal(acceptedAt, online.AcceptedBallots[0].WhenStatus);
        Assert.Null(Context.OnlineVotingInfos.Single(o => o.PersonGuid == people[2].PersonGuid).ListPool);
    }

    [Fact]
    public async Task GetMonitorInfoAsync_OnlineBallotLists_DoNotExposeContactOrVotePayload()
    {
        var election = await CreateTestElectionAsync();
        var people = await CreateTestPeopleAsync(election.ElectionGuid, 1);
        people[0].Email = "voter@example.test";
        people[0].Phone = "+15555550100";
        people[0].KioskCode = "Kiosk99";
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = election.ElectionGuid,
            PersonGuid = people[0].PersonGuid,
            Status = OnlineBallotStatus.Processed,
            WhenStatus = DateTimeOffset.UtcNow,
            ListPool = null
        });
        await Context.SaveChangesAsync();

        var result = await _service.GetMonitorInfoAsync(election.ElectionGuid);
        var row = Assert.Single(result.OnlineVotingInfo.AcceptedBallots);

        Assert.Equal($"{people[0].LastName}, {people[0].FirstName}", row.PersonName);
        Assert.DoesNotContain("voter@", row.PersonName);
        Assert.DoesNotContain("555", row.PersonName);
        Assert.DoesNotContain("Kiosk", row.PersonName);
        Assert.Equal(OnlineBallotStatus.Processed, row.Status);
        Assert.True(row.RowId > 0);
    }
}



