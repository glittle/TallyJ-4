using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Services;

using LocationTypeEnum = Backend.Domain.Enumerations.LocationType;

namespace Backend.Tests.UnitTests;

public class ReportServiceTests : ServiceTestBase, IAsyncLifetime
{
    private readonly ReportService _service;
    private readonly Guid _electionGuid = Guid.NewGuid();

    public ReportServiceTests()
    {
        _service = new ReportService(Context);
    }

    public async Task InitializeAsync()
    {
        await SeedElection();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedElection(
        string name = "Test Election",
        string electionType = "LSA",
        int numberToElect = 3,
        int numberExtra = 1,
        DateTime? onlineWhenOpen = null,
        string? votingMethods = null,
        string? customMethods = null,
        string? flags = null)
    {
        var existing = Context.Elections.FirstOrDefault(e => e.ElectionGuid == _electionGuid);
        if (existing != null)
        {
            Context.Elections.Remove(existing);
        }

        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionGuid,
            Name = name,
            ElectionType = electionType,
            NumberToElect = numberToElect,
            NumberExtra = numberExtra,
            DateOfElection = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc),
            OnlineWhenOpen = onlineWhenOpen,
            VotingMethods = votingMethods,
            CustomMethods = customMethods,
            Flags = flags,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
    }

    private async Task<Location> AddLocation(string name = "Main Hall", LocationTypeEnum locType = LocationTypeEnum.Manual)
    {
        var loc = new Location
        {
            ElectionGuid = _electionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = name,
            LocationTypeCode = locType.ToString()
        };
        Context.Locations.Add(loc);
        await Context.SaveChangesAsync();
        return loc;
    }

    private async Task<Person> AddPerson(
        string lastName = "Doe",
        string? firstName = "John",
        bool canVote = true,
        bool canReceiveVotes = true,
        string? votingMethod = null,
        string? area = null,
        Guid? votingLocationGuid = null,
        string? email = null,
        string? phone = null,
        Guid? ineligibleReasonGuid = null,
        string? combinedInfo = null,
        string? combinedInfoAtStart = null,
        string? flags = null)
    {
        var person = new Person
        {
            ElectionGuid = _electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = lastName,
            FirstName = firstName,
            CanVote = canVote,
            CanReceiveVotes = canReceiveVotes,
            VotingMethod = votingMethod,
            Area = area,
            VotingLocationGuid = votingLocationGuid,
            Email = email,
            Phone = phone,
            IneligibleReasonGuid = ineligibleReasonGuid,
            CombinedInfo = combinedInfo,
            CombinedInfoAtStart = combinedInfoAtStart,
            Flags = flags,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();
        return person;
    }

    private async Task AddResult(Guid personGuid, int rank, int voteCount, string section = "T",
        bool? tieBreakRequired = null, int? tieBreakCount = null, int? rankInExtra = null)
    {
        var result = new Result
        {
            ElectionGuid = _electionGuid,
            PersonGuid = personGuid,
            Rank = rank,
            VoteCount = voteCount,
            Section = section,
            TieBreakRequired = tieBreakRequired,
            TieBreakCount = tieBreakCount,
            RankInExtra = rankInExtra
        };
        Context.Results.Add(result);
        await Context.SaveChangesAsync();
    }

    private async Task<Ballot> AddBallot(Guid locationGuid, BallotStatus status = BallotStatus.Ok,
        string computerCode = "A", int ballotNum = 1, string? teller1 = null, string? teller2 = null)
    {
        var ballot = new Ballot
        {
            LocationGuid = locationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = status,
            ComputerCode = computerCode,
            BallotNumAtComputer = ballotNum,
            Teller1 = teller1,
            Teller2 = teller2,
            RowVersion = new byte[8]
        };
        Context.Ballots.Add(ballot);
        await Context.SaveChangesAsync();
        return ballot;
    }

    private async Task AddVote(Guid ballotGuid, Guid? personGuid = null, int position = 1,
        VoteStatus voteStatus = VoteStatus.Ok, string? ineligibleReasonCode = null)
    {
        var vote = new Vote
        {
            BallotGuid = ballotGuid,
            PersonGuid = personGuid,
            PositionOnBallot = position,
            VoteStatus = voteStatus,
            IneligibleReasonCode = ineligibleReasonCode,
            RowVersion = new byte[8]
        };
        Context.Votes.Add(vote);
        await Context.SaveChangesAsync();
    }

    private async Task AddResultSummary(int numEligible = 100, int numVoters = 80, int ballotsReceived = 80,
        int spoiledBallots = 2, int spoiledVotes = 3, int inPerson = 60, int mailedIn = 10,
        int online = 5, int imported = 5, int? spoiledManual = null)
    {
        var summary = new ResultSummary
        {
            ElectionGuid = _electionGuid,
            ResultType = "F",
            NumEligibleToVote = numEligible,
            NumVoters = numVoters,
            BallotsReceived = ballotsReceived,
            SpoiledBallots = spoiledBallots,
            SpoiledVotes = spoiledVotes,
            InPersonBallots = inPerson,
            MailedInBallots = mailedIn,
            OnlineBallots = online,
            ImportedBallots = imported,
            SpoiledManualBallots = spoiledManual
        };
        Context.ResultSummaries.Add(summary);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAvailableReports_BaseSet_ReturnsMinimumReports()
    {
        await AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "Main");
        Assert.Contains(reports, r => r.Code == "VotesByNum");
        Assert.Contains(reports, r => r.Code == "VotesByName");
        Assert.Contains(reports, r => r.Code == "Ballots");
        Assert.Contains(reports, r => r.Code == "AllCanReceive");
        Assert.Contains(reports, r => r.Code == "Voters");
        Assert.DoesNotContain(reports, r => r.Code == "BallotsOnline");
        Assert.DoesNotContain(reports, r => r.Code == "BallotsImported");
        Assert.DoesNotContain(reports, r => r.Code == "VotersOnline");
        Assert.DoesNotContain(reports, r => r.Code == "VoterEmails");
    }

    [Fact]
    public async Task GetAvailableReports_OnlineEnabled_IncludesOnlineReports()
    {
        await SeedElection(onlineWhenOpen: DateTime.UtcNow);
        await AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "BallotsOnline");
        Assert.Contains(reports, r => r.Code == "VotersOnline");
        Assert.Contains(reports, r => r.Code == "VoterEmails");
    }

    [Fact]
    public async Task GetAvailableReports_ImportedEnabled_IncludesImportedReport()
    {
        await SeedElection(votingMethods: "P,IM");
        await AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "BallotsImported");
    }

    [Fact]
    public async Task GetAvailableReports_MultipleLocations_IncludesLocationReports()
    {
        await AddLocation("Hall A");
        await AddLocation("Hall B");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "VotersByLocation");
        Assert.Contains(reports, r => r.Code == "VotersByLocationArea");
    }

    [Fact]
    public async Task GetAvailableReports_SingleLocation_ExcludesLocationReports()
    {
        await AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.DoesNotContain(reports, r => r.Code == "VotersByLocation");
        Assert.DoesNotContain(reports, r => r.Code == "VotersByLocationArea");
    }

    [Fact]
    public async Task GetMainReport_ReturnsElectedAndStatistics()
    {
        await AddLocation("Hall");
        await AddResultSummary();
        var p1 = await AddPerson("Alpha", "A", canReceiveVotes: true);
        var p2 = await AddPerson("Beta", "B", canReceiveVotes: true);
        var p3 = await AddPerson("Gamma", "G", canReceiveVotes: true);
        await AddResult(p1.PersonGuid, 1, 50, "T");
        await AddResult(p2.PersonGuid, 2, 40, "T");
        await AddResult(p3.PersonGuid, 3, 30, "T");

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Equal("Test Election", report.ElectionName);
        Assert.Equal(100, report.NumEligibleToVote);
        Assert.Equal(80, report.SumOfEnvelopesCollected);
        Assert.Equal(80.0, report.PercentParticipation);
        Assert.Equal(3, report.Elected.Count);
        Assert.Equal("50", report.Elected[0].VoteCountDisplay);
    }

    [Fact]
    public async Task GetMainReport_WithTies_SetsHasTies()
    {
        await AddLocation("Hall");
        await AddResultSummary();
        var p1 = await AddPerson("Alpha", "A");
        var p2 = await AddPerson("Beta", "B");
        await AddResult(p1.PersonGuid, 1, 50, "T", tieBreakRequired: true, tieBreakCount: 3);
        await AddResult(p2.PersonGuid, 2, 40, "T");

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.True(report.HasTies);
        Assert.Contains("/", report.Elected[0].VoteCountDisplay);
    }

    [Fact]
    public async Task GetMainReport_ExtraResults_ShowsNextRank()
    {
        await AddLocation("Hall");
        await AddResultSummary();
        var p1 = await AddPerson("Alpha", "A");
        var p2 = await AddPerson("Beta", "B");
        var p3 = await AddPerson("Gamma", "G");
        var p4 = await AddPerson("Delta", "D");
        await AddResult(p1.PersonGuid, 1, 50, "T");
        await AddResult(p2.PersonGuid, 2, 40, "T");
        await AddResult(p3.PersonGuid, 3, 30, "T");
        await AddResult(p4.PersonGuid, 4, 20, "X", rankInExtra: 1);

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Equal(4, report.Elected.Count);
        Assert.Equal("Next 1", report.Elected[3].Rank);
    }

    [Fact]
    public async Task GetMainReport_SpoiledBallots_GroupedByReason()
    {
        var loc = await AddLocation("Hall");
        await AddResultSummary(spoiledBallots: 3);
        await AddBallot(loc.LocationGuid, BallotStatus.TooMany);
        await AddBallot(loc.LocationGuid, BallotStatus.TooMany);
        await AddBallot(loc.LocationGuid, BallotStatus.TooFew);

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Equal(2, report.SpoiledBallotReasons.Count);
        Assert.Contains(report.SpoiledBallotReasons, r => r.BallotCount == 2);
        Assert.Contains(report.SpoiledBallotReasons, r => r.BallotCount == 1);
    }

    [Fact]
    public async Task GetMainReport_ManualSpoiledBallots_IncludedInReasons()
    {
        await AddResultSummary(spoiledManual: 5);

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Contains(report.SpoiledBallotReasons, r => r.Reason == "Unknown (Manual Count)" && r.BallotCount == 5);
    }

    [Fact]
    public async Task GetVotesByNum_OrderedByRank()
    {
        var p1 = await AddPerson("Zulu", "Z");
        var p2 = await AddPerson("Alpha", "A");
        await AddResult(p1.PersonGuid, 1, 50, "T");
        await AddResult(p2.PersonGuid, 2, 30, "T");

        var report = await _service.GetVotesByNumAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Equal(50, report.People[0].VoteCount);
        Assert.Equal(30, report.People[1].VoteCount);
    }

    [Fact]
    public async Task GetVotesByNum_ShowBreak_SetOnSectionChange()
    {
        var p1 = await AddPerson("Alpha", "A");
        var p2 = await AddPerson("Beta", "B");
        var p3 = await AddPerson("Gamma", "G");
        await AddResult(p1.PersonGuid, 1, 50, "T");
        await AddResult(p2.PersonGuid, 2, 40, "T");
        await AddResult(p3.PersonGuid, 3, 30, "O");

        var report = await _service.GetVotesByNumAsync(_electionGuid);

        Assert.True(report.People[0].ShowBreak);
        Assert.False(report.People[1].ShowBreak);
        Assert.True(report.People[2].ShowBreak);
    }

    [Fact]
    public async Task GetVotesByName_OrderedAlphabetically()
    {
        var p1 = await AddPerson("Zulu", "Z");
        var p2 = await AddPerson("Alpha", "A");
        await AddResult(p1.PersonGuid, 1, 50, "T");
        await AddResult(p2.PersonGuid, 2, 30, "T");

        var report = await _service.GetVotesByNameAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
    }

    [Fact]
    public async Task GetBallotsReport_NoFilter_ReturnsAllBallots()
    {
        var loc = await AddLocation("Hall");
        var person = await AddPerson("Test", "T");
        var b1 = await AddBallot(loc.LocationGuid);
        var b2 = await AddBallot(loc.LocationGuid, ballotNum: 2);
        await AddVote(b1.BallotGuid, person.PersonGuid, 1);
        await AddVote(b2.BallotGuid, person.PersonGuid, 1);

        var report = await _service.GetBallotsReportAsync(_electionGuid);

        Assert.Equal(2, report.Ballots.Count);
        Assert.Equal("Test Election", report.ElectionName);
    }

    [Fact]
    public async Task GetBallotsReport_OnlineFilter_ReturnsOnlyOnlineBallots()
    {
        var manualLoc = await AddLocation("Hall", LocationTypeEnum.Manual);
        var onlineLoc = await AddLocation("Online", LocationTypeEnum.Online);
        await AddBallot(manualLoc.LocationGuid);
        await AddBallot(onlineLoc.LocationGuid);

        var report = await _service.GetBallotsReportAsync(_electionGuid, "Online");

        Assert.Single(report.Ballots);
        Assert.True(report.Ballots[0].IsOnline);
    }

    [Fact]
    public async Task GetBallotsReport_ImportedFilter_ReturnsOnlyImportedBallots()
    {
        var manualLoc = await AddLocation("Hall", LocationTypeEnum.Manual);
        var importedLoc = await AddLocation("Imported", LocationTypeEnum.Imported);
        await AddBallot(manualLoc.LocationGuid);
        await AddBallot(importedLoc.LocationGuid);

        var report = await _service.GetBallotsReportAsync(_electionGuid, "Imported");

        Assert.Single(report.Ballots);
        Assert.True(report.Ballots[0].IsImported);
    }

    [Fact]
    public async Task GetBallotsReport_TiedFilter_ReturnsBallotsWithTiedCandidates()
    {
        var loc = await AddLocation("Hall");
        var tiedPerson = await AddPerson("Tied", "T");
        var normalPerson = await AddPerson("Normal", "N");
        await AddResult(tiedPerson.PersonGuid, 1, 50, "T", tieBreakRequired: true);
        await AddResult(normalPerson.PersonGuid, 2, 30, "T");

        var b1 = await AddBallot(loc.LocationGuid);
        await AddVote(b1.BallotGuid, tiedPerson.PersonGuid, 1);

        var b2 = await AddBallot(loc.LocationGuid, ballotNum: 2);
        await AddVote(b2.BallotGuid, normalPerson.PersonGuid, 1);

        var report = await _service.GetBallotsReportAsync(_electionGuid, "Tied");

        Assert.Single(report.Ballots);
        Assert.True(report.Ballots[0].Votes[0].TieBreakRequired);
    }

    [Fact]
    public async Task GetBallotsReport_SingleNameElection_SetsFlag()
    {
        await SeedElection(electionType: "Con");
        var loc = await AddLocation("Hall");
        await AddBallot(loc.LocationGuid);

        var report = await _service.GetBallotsReportAsync(_electionGuid);

        Assert.True(report.IsSingleNameElection);
    }

    [Fact]
    public async Task GetBallotAlignment_CountsMatchingVotes()
    {
        var loc = await AddLocation("Hall");
        var p1 = await AddPerson("A", "A");
        var p2 = await AddPerson("B", "B");
        var p3 = await AddPerson("C", "C");
        await AddResult(p1.PersonGuid, 1, 50, "T");
        await AddResult(p2.PersonGuid, 2, 40, "T");
        await AddResult(p3.PersonGuid, 3, 30, "T");

        var b1 = await AddBallot(loc.LocationGuid);
        await AddVote(b1.BallotGuid, p1.PersonGuid, 1);
        await AddVote(b1.BallotGuid, p2.PersonGuid, 2);
        await AddVote(b1.BallotGuid, p3.PersonGuid, 3);

        var b2 = await AddBallot(loc.LocationGuid, ballotNum: 2);
        await AddVote(b2.BallotGuid, p1.PersonGuid, 1);

        var report = await _service.GetBallotAlignmentAsync(_electionGuid);

        Assert.Equal(3, report.NumToElect);
        Assert.Contains(report.Rows, r => r.MatchingNames == 3 && r.BallotCount == 1);
        Assert.Contains(report.Rows, r => r.MatchingNames == 1 && r.BallotCount == 1);
    }

    [Fact]
    public async Task GetBallotsSame_FindsDuplicateBallots()
    {
        var loc = await AddLocation("Hall");
        var person = await AddPerson("Test", "T");

        var b1 = await AddBallot(loc.LocationGuid);
        await AddVote(b1.BallotGuid, person.PersonGuid, 1);
        var b2 = await AddBallot(loc.LocationGuid, ballotNum: 2);
        await AddVote(b2.BallotGuid, person.PersonGuid, 1);

        var report = await _service.GetBallotsSameAsync(_electionGuid);

        Assert.Single(report.Groups);
        Assert.Equal(2, report.Groups[0].Ballots.Count);
    }

    [Fact]
    public async Task GetBallotsSame_NoDuplicates_ReturnsEmpty()
    {
        var loc = await AddLocation("Hall");
        var p1 = await AddPerson("Alpha", "A");
        var p2 = await AddPerson("Beta", "B");

        var b1 = await AddBallot(loc.LocationGuid);
        await AddVote(b1.BallotGuid, p1.PersonGuid, 1);
        var b2 = await AddBallot(loc.LocationGuid, ballotNum: 2);
        await AddVote(b2.BallotGuid, p2.PersonGuid, 1);

        var report = await _service.GetBallotsSameAsync(_electionGuid);

        Assert.Empty(report.Groups);
    }

    [Fact]
    public async Task GetBallotsSummary_ReturnsBallotInfo()
    {
        var loc = await AddLocation("Hall");
        var b = await AddBallot(loc.LocationGuid, teller1: "Alice", teller2: "Bob");
        await AddVote(b.BallotGuid, position: 1, voteStatus: VoteStatus.Spoiled);
        await AddVote(b.BallotGuid, position: 2);

        var report = await _service.GetBallotsSummaryAsync(_electionGuid);

        Assert.Single(report.Ballots);
        Assert.Equal("Alice", report.Ballots[0].Teller1);
        Assert.Equal("Bob", report.Ballots[0].Teller2);
        Assert.Equal(1, report.Ballots[0].SpoiledVotes);
    }

    [Fact]
    public async Task GetBallotsSummary_SpoiledBallot_ZeroSpoiledVoteCount()
    {
        var loc = await AddLocation("Hall");
        var b = await AddBallot(loc.LocationGuid, status: BallotStatus.TooMany);
        await AddVote(b.BallotGuid, position: 1, voteStatus: VoteStatus.Spoiled);

        var report = await _service.GetBallotsSummaryAsync(_electionGuid);

        Assert.True(report.Ballots[0].Spoiled);
        Assert.Equal(0, report.Ballots[0].SpoiledVotes);
    }

    [Fact]
    public async Task GetAllCanReceive_ReturnsEligiblePeople()
    {
        await AddPerson("Eligible", "E", canReceiveVotes: true);
        await AddPerson("Ineligible", "I", canReceiveVotes: false);

        var report = await _service.GetAllCanReceiveAsync(_electionGuid);

        Assert.Single(report.People);
    }

    [Fact]
    public async Task GetVoters_ReturnsVoterParticipation()
    {
        var loc = await AddLocation("Hall");
        await AddPerson("Voted", "V", canVote: true, votingMethod: "P", votingLocationGuid: loc.LocationGuid);
        await AddPerson("NotVoted", "N", canVote: true);
        await AddPerson("CantVote", "C", canVote: false);

        var report = await _service.GetVotersAsync(_electionGuid);

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => p.VotingMethod == "In Person");
        Assert.Contains(report.People, p => p.VotingMethod == "-");
    }

    [Fact]
    public async Task GetVoters_MultipleLocations_ShowsLocationName()
    {
        var loc1 = await AddLocation("Hall A");
        await AddLocation("Hall B");

        await AddPerson("Person1", "P", canVote: true, votingLocationGuid: loc1.LocationGuid);

        var report = await _service.GetVotersAsync(_electionGuid);

        Assert.True(report.HasMultipleLocations);
        Assert.Equal("Hall A", report.People[0].Location);
    }

    [Fact]
    public async Task GetFlagsReport_ReturnsFlags()
    {
        await SeedElection(flags: "Arrived|Has Badge");
        await AddLocation("Hall");
        await AddPerson("Person1", "P", flags: "Arrived|Has Badge");
        await AddPerson("Person2", "Q", flags: "Arrived");

        var report = await _service.GetFlagsReportAsync(_electionGuid);

        Assert.Equal(2, report.FlagNames.Count);
        Assert.Equal("Arrived", report.FlagNames[0]);
        Assert.Equal("Has Badge", report.FlagNames[1]);
        Assert.Equal(2, report.People[0].Flags.Count);
        Assert.Single(report.People[1].Flags);
    }

    [Fact]
    public async Task GetVotersByArea_GroupsByArea()
    {
        await AddPerson("P1", "A", canVote: true, area: "North", votingMethod: "P");
        await AddPerson("P2", "B", canVote: true, area: "North", votingMethod: "M");
        await AddPerson("P3", "C", canVote: true, area: "South", votingMethod: "P");

        var report = await _service.GetVotersByAreaAsync(_electionGuid);

        Assert.Equal(2, report.Areas.Count);
        var north = report.Areas.First(a => a.AreaName == "North");
        Assert.Equal(2, north.TotalEligible);
        Assert.Equal(2, north.Voted);
        Assert.Equal(1, north.InPerson);
        Assert.Equal(1, north.MailedIn);

        Assert.Equal(3, report.Total.TotalEligible);
    }

    [Fact]
    public async Task GetVotersByLocation_GroupsByLocation()
    {
        var loc1 = await AddLocation("Hall A");
        var loc2 = await AddLocation("Hall B");
        await AddPerson("P1", "A", canVote: true, votingMethod: "P", votingLocationGuid: loc1.LocationGuid);
        await AddPerson("P2", "B", canVote: true, votingMethod: "M", votingLocationGuid: loc2.LocationGuid);

        var report = await _service.GetVotersByLocationAsync(_electionGuid);

        Assert.Equal(2, report.Locations.Count);
        Assert.Equal(2, report.Total.TotalVoters);
    }

    [Fact]
    public async Task GetVotersByLocationArea_GroupsByLocationThenArea()
    {
        var loc = await AddLocation("Hall A");
        await AddPerson("P1", "A", canVote: true, area: "North", votingLocationGuid: loc.LocationGuid, votingMethod: "P");
        await AddPerson("P2", "B", canVote: true, area: "South", votingLocationGuid: loc.LocationGuid, votingMethod: "P");

        var report = await _service.GetVotersByLocationAreaAsync(_electionGuid);

        Assert.Single(report.Locations);
        Assert.Equal(2, report.Locations[0].Areas.Count);
        Assert.Equal(2, report.Locations[0].TotalCount);
    }

    [Fact]
    public async Task GetChangedPeople_ReturnsChangedAndNew()
    {
        await AddPerson("Changed", "C", combinedInfo: "new-info", combinedInfoAtStart: "old-info");
        await AddPerson("New", "N", combinedInfo: "some-info", combinedInfoAtStart: "");
        await AddPerson("Same", "S", combinedInfo: "same", combinedInfoAtStart: "same");

        var report = await _service.GetChangedPeopleAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => p.Change == "Changed");
        Assert.Contains(report.People, p => p.Change == "New");
    }

    [Fact]
    public async Task GetAllNonEligible_ReturnsIneligiblePeople()
    {
        await AddPerson("FullEligible", "F", canVote: true, canReceiveVotes: true);
        await AddPerson("CantVote", "C", canVote: false, canReceiveVotes: true,
            ineligibleReasonGuid: IneligibleReasonEnum.R01_NotADelegateInThisElection.ReasonGuid);
        await AddPerson("CantReceive", "R", canVote: true, canReceiveVotes: false,
            ineligibleReasonGuid: IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid);

        var report = await _service.GetAllNonEligibleAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => !p.CanVote);
        Assert.Contains(report.People, p => !p.CanReceiveVotes);
    }

    [Fact]
    public async Task GetVoterEmails_ReturnsEmailsAndPhones()
    {
        await AddPerson("WithEmail", "E", email: "test@example.com");
        await AddPerson("WithPhone", "P", phone: "555-1234");
        await AddPerson("Neither", "N");

        var report = await _service.GetVoterEmailsAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => p.Email == "test@example.com");
        Assert.Contains(report.People, p => p.Phone == "555-1234");
    }

    [Fact]
    public async Task GetVoterEmails_WithOnlineVoter_SetsSignedInFlags()
    {
        await AddPerson("WithEmail", "E", email: "voter@test.com");
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = "voter@test.com",
            VoterIdType = "E",
            RowId = 0
        });
        await Context.SaveChangesAsync();

        var report = await _service.GetVoterEmailsAsync(_electionGuid);

        Assert.Single(report.People);
        Assert.True(report.People[0].HasSignedInEmail);
        Assert.False(report.People[0].HasSignedInPhone);
    }

    [Fact]
    public async Task GetSpoiledVotes_ReturnsSpoiledVotesByPerson()
    {
        var loc = await AddLocation("Hall");
        var person = await AddPerson("Ineligible", "I", canReceiveVotes: false,
            ineligibleReasonGuid: IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid);
        var ballot = await AddBallot(loc.LocationGuid);
        await AddVote(ballot.BallotGuid, person.PersonGuid, 1, VoteStatus.Ok, ineligibleReasonCode: "V01");

        var report = await _service.GetSpoiledVotesAsync(_electionGuid);

        Assert.NotEmpty(report.People);
    }

    [Fact]
    public async Task GetElection_NonExistent_Throws()
    {
        var fakeGuid = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMainReportAsync(fakeGuid));
    }

    [Fact]
    public async Task GetVotersOnline_ReturnsOnlineVoterInfo()
    {
        await SeedElection(onlineWhenOpen: DateTime.UtcNow);
        var person = await AddPerson("Online", "O", votingMethod: "O", email: "test@example.com");
        var now = DateTime.UtcNow;
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = _electionGuid,
            PersonGuid = person.PersonGuid,
            Status = "Voted",
            WhenStatus = now
        });
        await Context.SaveChangesAsync();

        var report = await _service.GetVotersOnlineAsync(_electionGuid);

        Assert.Single(report.People);
        Assert.Equal("Online", report.People[0].VotingMethodDisplay);
        Assert.Equal("Voted", report.People[0].Status);
    }

    [Fact]
    public async Task GetVotersByArea_NoArea_GroupsAsUnknown()
    {
        await AddPerson("NoArea", "N", canVote: true, area: null, votingMethod: "P");

        var report = await _service.GetVotersByAreaAsync(_electionGuid);

        Assert.Single(report.Areas);
        Assert.Equal("(unknown)", report.Areas[0].AreaName);
    }

    [Fact]
    public async Task GetVotersByArea_CustomMethods_PassedThrough()
    {
        await SeedElection(customMethods: "Special1|Special2|Special3");
        await AddPerson("P1", "A", canVote: true, area: "North", votingMethod: "1");

        var report = await _service.GetVotersByAreaAsync(_electionGuid);

        Assert.Equal("Special1", report.Custom1Name);
        Assert.Equal("Special2", report.Custom2Name);
        Assert.Equal("Special3", report.Custom3Name);
        Assert.Equal(1, report.Areas[0].Custom1);
    }
}
