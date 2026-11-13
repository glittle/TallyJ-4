using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.DTOs.Computers;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Tests.UnitTests;

/// <summary>
/// Issue #427: one accepted online ballot (8 names) and two paper ballots
/// (8 names, and 9 lines including one Unreadable) while electing 9.
/// </summary>
public class Issue427MixedBallotCountTests : ServiceTestBase
{
    private readonly TallyService _tally;
    private readonly ReportService _reports;
    private readonly Mock<IComputerAssignmentService> _computers = new();

    public Issue427MixedBallotCountTests()
    {
        var tallyLocalizer = new Mock<IStringLocalizer<TallyService>>();
        tallyLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _computers
            .Setup(s => s.GetActiveComputers(It.IsAny<Guid>()))
            .Returns(Array.Empty<ActiveComputerDto>());

        _tally = new TallyService(
            Context,
            new Mock<ILogger<TallyService>>().Object,
            new Mock<ISignalRNotificationService>().Object,
            _computers.Object,
            new Mock<IOnlineVoterPresenceService>().Object,
            tallyLocalizer.Object);

        var reportLocalizer = new Mock<IStringLocalizer<ReportService>>();
        reportLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _reports = new ReportService(Context, reportLocalizer.Object);
    }

    [Fact]
    public async Task MixedOnlineAndPaperBallots_KeepEnteredCountsAndAwardOnlyOkLines()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Bot QA run",
            ElectionType = "LSA",
            NumberToElect = 9,
            NumberExtra = 0,
            ElectionStage = ElectionStage.ProcessingBallots,
            RowVersion = new byte[8]
        };
        var paperLocation = new Location
        {
            ElectionGuid = election.ElectionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "QA Test Location"
        };
        var onlineLocation = new Location
        {
            ElectionGuid = election.ElectionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Online",
            LocationTypeCode = nameof(LocationType.Online)
        };
        Context.Elections.Add(election);
        Context.Locations.AddRange(paperLocation, onlineLocation);

        var candidates = new List<Person>();
        for (var i = 0; i < 8; i++)
        {
            var person = new Person
            {
                ElectionGuid = election.ElectionGuid,
                PersonGuid = Guid.NewGuid(),
                FirstName = $"Candidate{i}",
                LastName = "Name",
                CanReceiveVotes = true,
                CanVote = false,
                CombinedInfo = $"Name, Candidate{i}",
                RowVersion = new byte[8]
            };
            candidates.Add(person);
            Context.People.Add(person);
        }

        var onlineVoter = AddVoter(election.ElectionGuid, "Online", votingMethod: null);
        AddVoter(election.ElectionGuid, "Paper1", "P");
        AddVoter(election.ElectionGuid, "Paper2", "P");
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = election.ElectionGuid,
            PersonGuid = onlineVoter.PersonGuid,
            Status = OnlineBallotStatus.Processed,
            WhenStatus = DateTimeOffset.UtcNow
        });

        var onlineBallot = AddBallot(onlineLocation.LocationGuid, ComputerCodeHelper.Online, 1);
        var shortPaper = AddBallot(paperLocation.LocationGuid, "A", 1);
        var fullPaper = AddBallot(paperLocation.LocationGuid, "A", 2);
        AddNamedVotes(onlineBallot, candidates);
        AddNamedVotes(shortPaper, candidates);
        AddNamedVotes(fullPaper, candidates);
        Context.Votes.Add(new Vote
        {
            BallotGuid = fullPaper.BallotGuid,
            PositionOnBallot = 9,
            VoteStatus = VoteStatus.Ok,
            IneligibleReasonCode = IneligibleReasonEnum.U02_Unreadable.Code,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        _computers
            .Setup(s => s.GetActiveComputers(election.ElectionGuid))
            .Returns(new[]
            {
                new ActiveComputerDto
                {
                    ComputerCode = "A",
                    ConnectedAt = DateTimeOffset.UtcNow
                }
            });

        var tally = await _tally.CalculateNormalElectionAsync(election.ElectionGuid);

        // 3 ballots × 9 seats. The two unfilled lines on the Too Few ballots
        // are part of that potential total. They are not lines that were typed.
        Assert.Equal(27, tally.Statistics.TotalVotes);
        // One Unreadable line on the only Ok ballot.
        Assert.Equal(1, tally.Statistics.InvalidVotes);
        // Eight readable names on that ballot. Too Few ballots are spoiled.
        Assert.Equal(8, tally.Statistics.ValidVotes);
        Assert.Equal(8, tally.Results.Sum(r => r.VoteCount));
        Assert.Equal(3, tally.Statistics.TotalBallots);
        Assert.Equal(1, tally.Statistics.BallotsReceived);
        Assert.Equal(2, tally.Statistics.SpoiledBallots);

        var monitor = await _tally.GetMonitorInfoAsync(election.ElectionGuid);
        var paperRow = Assert.Single(monitor.Locations, l => l.LocationName == "QA Test Location");
        Assert.Equal(2, paperRow.BallotCount);
        Assert.Equal(17, paperRow.VoteCount);
        var computer = Assert.Single(monitor.Computers);
        Assert.Equal("A", computer.ComputerCode);
        Assert.Equal(2, computer.BallotCount);
        Assert.Equal(25, monitor.TotalVotes);

        var locationReport = await _tally.GetReportDataAsync(election.ElectionGuid, "locations");
        var locations = Assert.IsAssignableFrom<IEnumerable<LocationReportDto>>(locationReport.Data);
        var paperReport = Assert.Single(locations, l => l.LocationName == "QA Test Location");
        Assert.Equal(2, paperReport.BallotsEntered);
        Assert.Equal(17, paperReport.TotalVotes);

        var presentation = await _tally.GetPresentationDataAsync(election.ElectionGuid);
        // Received ballots (valid + spoiled), matching v3 "Number who Voted".
        Assert.Equal(3, presentation.TotalBallots);
        Assert.Equal(8, presentation.TotalVotes);

        var main = await _reports.GetMainReportAsync(election.ElectionGuid);
        Assert.Equal(3, main.SumOfEnvelopesCollected);
        Assert.Equal(3, main.NumBallotsWithManual);
        Assert.Equal(1, main.OnlineBallots);
        Assert.Equal(2, main.InPersonBallots);
        Assert.Equal(1, main.SpoiledVotes);
        Assert.Contains(main.SpoiledBallotReasons, r => r.Reason == "Too Few" && r.BallotCount == 2);
        Assert.Contains(main.SpoiledVoteReasons, r => r.Reason == "Unreadable" && r.VoteCount == 1);
    }

    private Person AddVoter(Guid electionGuid, string firstName, string? votingMethod)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = firstName,
            LastName = "Voter",
            CanVote = true,
            CanReceiveVotes = false,
            VotingMethod = votingMethod,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        return person;
    }

    private Ballot AddBallot(Guid locationGuid, string computerCode, int number)
    {
        var ballot = new Ballot
        {
            LocationGuid = locationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = computerCode,
            BallotNumAtComputer = number,
            RowVersion = new byte[8]
        };
        Context.Ballots.Add(ballot);
        return ballot;
    }

    private void AddNamedVotes(Ballot ballot, IReadOnlyList<Person> people)
    {
        for (var i = 0; i < people.Count; i++)
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
}
