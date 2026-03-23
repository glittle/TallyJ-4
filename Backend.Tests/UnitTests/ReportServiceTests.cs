using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Services;

using LocationTypeEnum = Backend.Domain.Enumerations.LocationType;

namespace Backend.Tests.UnitTests;

public class ReportServiceTests : ServiceTestBase
{
    private readonly ReportService _service;
    private readonly Guid _electionGuid = Guid.NewGuid();

    public ReportServiceTests()
    {
        _service = new ReportService(Context);
        SeedElection();
    }

    private void SeedElection(
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
            Context.SaveChangesAsync().GetAwaiter().GetResult();
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
        Context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private Location AddLocation(string name = "Main Hall", LocationTypeEnum locType = LocationTypeEnum.Manual)
    {
        var loc = new Location
        {
            ElectionGuid = _electionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = name,
            LocationTypeCode = locType.ToString()
        };
        Context.Locations.Add(loc);
        Context.SaveChangesAsync().GetAwaiter().GetResult();
        return loc;
    }

    private Person AddPerson(
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
        Context.SaveChangesAsync().GetAwaiter().GetResult();
        return person;
    }

    private void AddResult(Guid personGuid, int rank, int voteCount, string section = "T",
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
        Context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private Ballot AddBallot(Guid locationGuid, BallotStatus status = BallotStatus.Ok,
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
        Context.SaveChangesAsync().GetAwaiter().GetResult();
        return ballot;
    }

    private void AddVote(Guid ballotGuid, Guid? personGuid = null, int position = 1,
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
        Context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private void AddResultSummary(int numEligible = 100, int numVoters = 80, int ballotsReceived = 80,
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
        Context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetAvailableReports_BaseSet_ReturnsMinimumReports()
    {
        AddLocation("Main Hall");

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
        SeedElection(onlineWhenOpen: DateTime.UtcNow);
        AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "BallotsOnline");
        Assert.Contains(reports, r => r.Code == "VotersOnline");
        Assert.Contains(reports, r => r.Code == "VoterEmails");
    }

    [Fact]
    public async Task GetAvailableReports_ImportedEnabled_IncludesImportedReport()
    {
        SeedElection(votingMethods: "P,IM");
        AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "BallotsImported");
    }

    [Fact]
    public async Task GetAvailableReports_MultipleLocations_IncludesLocationReports()
    {
        AddLocation("Hall A");
        AddLocation("Hall B");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.Contains(reports, r => r.Code == "VotersByLocation");
        Assert.Contains(reports, r => r.Code == "VotersByLocationArea");
    }

    [Fact]
    public async Task GetAvailableReports_SingleLocation_ExcludesLocationReports()
    {
        AddLocation("Main Hall");

        var reports = await _service.GetAvailableReportsAsync(_electionGuid);

        Assert.DoesNotContain(reports, r => r.Code == "VotersByLocation");
        Assert.DoesNotContain(reports, r => r.Code == "VotersByLocationArea");
    }

    [Fact]
    public async Task GetMainReport_ReturnsElectedAndStatistics()
    {
        AddLocation("Hall");
        AddResultSummary();
        var p1 = AddPerson("Alpha", "A", canReceiveVotes: true);
        var p2 = AddPerson("Beta", "B", canReceiveVotes: true);
        var p3 = AddPerson("Gamma", "G", canReceiveVotes: true);
        AddResult(p1.PersonGuid, 1, 50, "T");
        AddResult(p2.PersonGuid, 2, 40, "T");
        AddResult(p3.PersonGuid, 3, 30, "T");

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
        AddLocation("Hall");
        AddResultSummary();
        var p1 = AddPerson("Alpha", "A");
        var p2 = AddPerson("Beta", "B");
        AddResult(p1.PersonGuid, 1, 50, "T", tieBreakRequired: true, tieBreakCount: 3);
        AddResult(p2.PersonGuid, 2, 40, "T");

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.True(report.HasTies);
        Assert.Contains("/", report.Elected[0].VoteCountDisplay);
    }

    [Fact]
    public async Task GetMainReport_ExtraResults_ShowsNextRank()
    {
        AddLocation("Hall");
        AddResultSummary();
        var p1 = AddPerson("Alpha", "A");
        var p2 = AddPerson("Beta", "B");
        var p3 = AddPerson("Gamma", "G");
        var p4 = AddPerson("Delta", "D");
        AddResult(p1.PersonGuid, 1, 50, "T");
        AddResult(p2.PersonGuid, 2, 40, "T");
        AddResult(p3.PersonGuid, 3, 30, "T");
        AddResult(p4.PersonGuid, 4, 20, "X", rankInExtra: 1);

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Equal(4, report.Elected.Count);
        Assert.Equal("Next 1", report.Elected[3].Rank);
    }

    [Fact]
    public async Task GetMainReport_SpoiledBallots_GroupedByReason()
    {
        var loc = AddLocation("Hall");
        AddResultSummary(spoiledBallots: 3);
        AddBallot(loc.LocationGuid, BallotStatus.TooMany);
        AddBallot(loc.LocationGuid, BallotStatus.TooMany);
        AddBallot(loc.LocationGuid, BallotStatus.TooFew);

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Equal(2, report.SpoiledBallotReasons.Count);
        Assert.Contains(report.SpoiledBallotReasons, r => r.BallotCount == 2);
        Assert.Contains(report.SpoiledBallotReasons, r => r.BallotCount == 1);
    }

    [Fact]
    public async Task GetMainReport_ManualSpoiledBallots_IncludedInReasons()
    {
        AddResultSummary(spoiledManual: 5);

        var report = await _service.GetMainReportAsync(_electionGuid);

        Assert.Contains(report.SpoiledBallotReasons, r => r.Reason == "Unknown (Manual Count)" && r.BallotCount == 5);
    }

    [Fact]
    public async Task GetVotesByNum_OrderedByRank()
    {
        var p1 = AddPerson("Zulu", "Z");
        var p2 = AddPerson("Alpha", "A");
        AddResult(p1.PersonGuid, 1, 50, "T");
        AddResult(p2.PersonGuid, 2, 30, "T");

        var report = await _service.GetVotesByNumAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Equal(50, report.People[0].VoteCount);
        Assert.Equal(30, report.People[1].VoteCount);
    }

    [Fact]
    public async Task GetVotesByNum_ShowBreak_SetOnSectionChange()
    {
        var p1 = AddPerson("Alpha", "A");
        var p2 = AddPerson("Beta", "B");
        var p3 = AddPerson("Gamma", "G");
        AddResult(p1.PersonGuid, 1, 50, "T");
        AddResult(p2.PersonGuid, 2, 40, "T");
        AddResult(p3.PersonGuid, 3, 30, "O");

        var report = await _service.GetVotesByNumAsync(_electionGuid);

        Assert.True(report.People[0].ShowBreak);
        Assert.False(report.People[1].ShowBreak);
        Assert.True(report.People[2].ShowBreak);
    }

    [Fact]
    public async Task GetVotesByName_OrderedAlphabetically()
    {
        var p1 = AddPerson("Zulu", "Z");
        var p2 = AddPerson("Alpha", "A");
        AddResult(p1.PersonGuid, 1, 50, "T");
        AddResult(p2.PersonGuid, 2, 30, "T");

        var report = await _service.GetVotesByNameAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
    }

    [Fact]
    public async Task GetBallotsReport_NoFilter_ReturnsAllBallots()
    {
        var loc = AddLocation("Hall");
        var person = AddPerson("Test", "T");
        var b1 = AddBallot(loc.LocationGuid);
        var b2 = AddBallot(loc.LocationGuid, ballotNum: 2);
        AddVote(b1.BallotGuid, person.PersonGuid, 1);
        AddVote(b2.BallotGuid, person.PersonGuid, 1);

        var report = await _service.GetBallotsReportAsync(_electionGuid);

        Assert.Equal(2, report.Ballots.Count);
        Assert.Equal("Test Election", report.ElectionName);
    }

    [Fact]
    public async Task GetBallotsReport_OnlineFilter_ReturnsOnlyOnlineBallots()
    {
        var manualLoc = AddLocation("Hall", LocationTypeEnum.Manual);
        var onlineLoc = AddLocation("Online", LocationTypeEnum.Online);
        AddBallot(manualLoc.LocationGuid);
        AddBallot(onlineLoc.LocationGuid);

        var report = await _service.GetBallotsReportAsync(_electionGuid, "Online");

        Assert.Single(report.Ballots);
        Assert.True(report.Ballots[0].IsOnline);
    }

    [Fact]
    public async Task GetBallotsReport_ImportedFilter_ReturnsOnlyImportedBallots()
    {
        var manualLoc = AddLocation("Hall", LocationTypeEnum.Manual);
        var importedLoc = AddLocation("Imported", LocationTypeEnum.Imported);
        AddBallot(manualLoc.LocationGuid);
        AddBallot(importedLoc.LocationGuid);

        var report = await _service.GetBallotsReportAsync(_electionGuid, "Imported");

        Assert.Single(report.Ballots);
        Assert.True(report.Ballots[0].IsImported);
    }

    [Fact]
    public async Task GetBallotsReport_TiedFilter_ReturnsBallotsWithTiedCandidates()
    {
        var loc = AddLocation("Hall");
        var tiedPerson = AddPerson("Tied", "T");
        var normalPerson = AddPerson("Normal", "N");
        AddResult(tiedPerson.PersonGuid, 1, 50, "T", tieBreakRequired: true);
        AddResult(normalPerson.PersonGuid, 2, 30, "T");

        var b1 = AddBallot(loc.LocationGuid);
        AddVote(b1.BallotGuid, tiedPerson.PersonGuid, 1);

        var b2 = AddBallot(loc.LocationGuid, ballotNum: 2);
        AddVote(b2.BallotGuid, normalPerson.PersonGuid, 1);

        var report = await _service.GetBallotsReportAsync(_electionGuid, "Tied");

        Assert.Single(report.Ballots);
        Assert.True(report.Ballots[0].Votes[0].TieBreakRequired);
    }

    [Fact]
    public async Task GetBallotsReport_SingleNameElection_SetsFlag()
    {
        SeedElection(electionType: "Con");
        var loc = AddLocation("Hall");
        AddBallot(loc.LocationGuid);

        var report = await _service.GetBallotsReportAsync(_electionGuid);

        Assert.True(report.IsSingleNameElection);
    }

    [Fact]
    public async Task GetBallotAlignment_CountsMatchingVotes()
    {
        var loc = AddLocation("Hall");
        var p1 = AddPerson("A", "A");
        var p2 = AddPerson("B", "B");
        var p3 = AddPerson("C", "C");
        AddResult(p1.PersonGuid, 1, 50, "T");
        AddResult(p2.PersonGuid, 2, 40, "T");
        AddResult(p3.PersonGuid, 3, 30, "T");

        var b1 = AddBallot(loc.LocationGuid);
        AddVote(b1.BallotGuid, p1.PersonGuid, 1);
        AddVote(b1.BallotGuid, p2.PersonGuid, 2);
        AddVote(b1.BallotGuid, p3.PersonGuid, 3);

        var b2 = AddBallot(loc.LocationGuid, ballotNum: 2);
        AddVote(b2.BallotGuid, p1.PersonGuid, 1);

        var report = await _service.GetBallotAlignmentAsync(_electionGuid);

        Assert.Equal(3, report.NumToElect);
        Assert.Contains(report.Rows, r => r.MatchingNames == 3 && r.BallotCount == 1);
        Assert.Contains(report.Rows, r => r.MatchingNames == 1 && r.BallotCount == 1);
    }

    [Fact]
    public async Task GetBallotsSame_FindsDuplicateBallots()
    {
        var loc = AddLocation("Hall");
        var person = AddPerson("Test", "T");

        var b1 = AddBallot(loc.LocationGuid);
        AddVote(b1.BallotGuid, person.PersonGuid, 1);
        var b2 = AddBallot(loc.LocationGuid, ballotNum: 2);
        AddVote(b2.BallotGuid, person.PersonGuid, 1);

        var report = await _service.GetBallotsSameAsync(_electionGuid);

        Assert.Single(report.Groups);
        Assert.Equal(2, report.Groups[0].Ballots.Count);
    }

    [Fact]
    public async Task GetBallotsSame_NoDuplicates_ReturnsEmpty()
    {
        var loc = AddLocation("Hall");
        var p1 = AddPerson("Alpha", "A");
        var p2 = AddPerson("Beta", "B");

        var b1 = AddBallot(loc.LocationGuid);
        AddVote(b1.BallotGuid, p1.PersonGuid, 1);
        var b2 = AddBallot(loc.LocationGuid, ballotNum: 2);
        AddVote(b2.BallotGuid, p2.PersonGuid, 1);

        var report = await _service.GetBallotsSameAsync(_electionGuid);

        Assert.Empty(report.Groups);
    }

    [Fact]
    public async Task GetBallotsSummary_ReturnsBallotInfo()
    {
        var loc = AddLocation("Hall");
        var b = AddBallot(loc.LocationGuid, teller1: "Alice", teller2: "Bob");
        AddVote(b.BallotGuid, position: 1, voteStatus: VoteStatus.Spoiled);
        AddVote(b.BallotGuid, position: 2);

        var report = await _service.GetBallotsSummaryAsync(_electionGuid);

        Assert.Single(report.Ballots);
        Assert.Equal("Alice", report.Ballots[0].Teller1);
        Assert.Equal("Bob", report.Ballots[0].Teller2);
        Assert.Equal(1, report.Ballots[0].SpoiledVotes);
    }

    [Fact]
    public async Task GetBallotsSummary_SpoiledBallot_ZeroSpoiledVoteCount()
    {
        var loc = AddLocation("Hall");
        var b = AddBallot(loc.LocationGuid, status: BallotStatus.TooMany);
        AddVote(b.BallotGuid, position: 1, voteStatus: VoteStatus.Spoiled);

        var report = await _service.GetBallotsSummaryAsync(_electionGuid);

        Assert.True(report.Ballots[0].Spoiled);
        Assert.Equal(0, report.Ballots[0].SpoiledVotes);
    }

    [Fact]
    public async Task GetAllCanReceive_ReturnsEligiblePeople()
    {
        AddPerson("Eligible", "E", canReceiveVotes: true);
        AddPerson("Ineligible", "I", canReceiveVotes: false);

        var report = await _service.GetAllCanReceiveAsync(_electionGuid);

        Assert.Single(report.People);
    }

    [Fact]
    public async Task GetVoters_ReturnsVoterParticipation()
    {
        var loc = AddLocation("Hall");
        AddPerson("Voted", "V", canVote: true, votingMethod: "P", votingLocationGuid: loc.LocationGuid);
        AddPerson("NotVoted", "N", canVote: true);
        AddPerson("CantVote", "C", canVote: false);

        var report = await _service.GetVotersAsync(_electionGuid);

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => p.VotingMethod == "In Person");
        Assert.Contains(report.People, p => p.VotingMethod == "-");
    }

    [Fact]
    public async Task GetVoters_MultipleLocations_ShowsLocationName()
    {
        var loc1 = AddLocation("Hall A");
        AddLocation("Hall B");

        AddPerson("Person1", "P", canVote: true, votingLocationGuid: loc1.LocationGuid);

        var report = await _service.GetVotersAsync(_electionGuid);

        Assert.True(report.HasMultipleLocations);
        Assert.Equal("Hall A", report.People[0].Location);
    }

    [Fact]
    public async Task GetFlagsReport_ReturnsFlags()
    {
        SeedElection(flags: "Arrived|Has Badge");
        AddLocation("Hall");
        AddPerson("Person1", "P", flags: "Arrived|Has Badge");
        AddPerson("Person2", "Q", flags: "Arrived");

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
        AddPerson("P1", "A", canVote: true, area: "North", votingMethod: "P");
        AddPerson("P2", "B", canVote: true, area: "North", votingMethod: "M");
        AddPerson("P3", "C", canVote: true, area: "South", votingMethod: "P");

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
        var loc1 = AddLocation("Hall A");
        var loc2 = AddLocation("Hall B");
        AddPerson("P1", "A", canVote: true, votingMethod: "P", votingLocationGuid: loc1.LocationGuid);
        AddPerson("P2", "B", canVote: true, votingMethod: "M", votingLocationGuid: loc2.LocationGuid);

        var report = await _service.GetVotersByLocationAsync(_electionGuid);

        Assert.Equal(2, report.Locations.Count);
        Assert.Equal(2, report.Total.TotalVoters);
    }

    [Fact]
    public async Task GetVotersByLocationArea_GroupsByLocationThenArea()
    {
        var loc = AddLocation("Hall A");
        AddPerson("P1", "A", canVote: true, area: "North", votingLocationGuid: loc.LocationGuid, votingMethod: "P");
        AddPerson("P2", "B", canVote: true, area: "South", votingLocationGuid: loc.LocationGuid, votingMethod: "P");

        var report = await _service.GetVotersByLocationAreaAsync(_electionGuid);

        Assert.Single(report.Locations);
        Assert.Equal(2, report.Locations[0].Areas.Count);
        Assert.Equal(2, report.Locations[0].TotalCount);
    }

    [Fact]
    public async Task GetChangedPeople_ReturnsChangedAndNew()
    {
        AddPerson("Changed", "C", combinedInfo: "new-info", combinedInfoAtStart: "old-info");
        AddPerson("New", "N", combinedInfo: "some-info", combinedInfoAtStart: "");
        AddPerson("Same", "S", combinedInfo: "same", combinedInfoAtStart: "same");

        var report = await _service.GetChangedPeopleAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => p.Change == "Changed");
        Assert.Contains(report.People, p => p.Change == "New");
    }

    [Fact]
    public async Task GetAllNonEligible_ReturnsIneligiblePeople()
    {
        AddPerson("FullEligible", "F", canVote: true, canReceiveVotes: true);
        AddPerson("CantVote", "C", canVote: false, canReceiveVotes: true,
            ineligibleReasonGuid: IneligibleReasonEnum.R01_NotADelegateInThisElection.ReasonGuid);
        AddPerson("CantReceive", "R", canVote: true, canReceiveVotes: false,
            ineligibleReasonGuid: IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid);

        var report = await _service.GetAllNonEligibleAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => !p.CanVote);
        Assert.Contains(report.People, p => !p.CanReceiveVotes);
    }

    [Fact]
    public async Task GetVoterEmails_ReturnsEmailsAndPhones()
    {
        AddPerson("WithEmail", "E", email: "test@example.com");
        AddPerson("WithPhone", "P", phone: "555-1234");
        AddPerson("Neither", "N");

        var report = await _service.GetVoterEmailsAsync(_electionGuid);

        Assert.Equal(2, report.People.Count);
        Assert.Contains(report.People, p => p.Email == "test@example.com");
        Assert.Contains(report.People, p => p.Phone == "555-1234");
    }

    [Fact]
    public async Task GetVoterEmails_WithOnlineVoter_SetsSignedInFlags()
    {
        AddPerson("WithEmail", "E", email: "voter@test.com");
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
        var loc = AddLocation("Hall");
        var person = AddPerson("Ineligible", "I", canReceiveVotes: false,
            ineligibleReasonGuid: IneligibleReasonEnum.V01_YouthAged181920.ReasonGuid);
        var ballot = AddBallot(loc.LocationGuid);
        AddVote(ballot.BallotGuid, person.PersonGuid, 1, VoteStatus.Ok, ineligibleReasonCode: "V01");

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
        SeedElection(onlineWhenOpen: DateTime.UtcNow);
        var person = AddPerson("Online", "O", votingMethod: "O", email: "test@example.com");
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
        AddPerson("NoArea", "N", canVote: true, area: null, votingMethod: "P");

        var report = await _service.GetVotersByAreaAsync(_electionGuid);

        Assert.Single(report.Areas);
        Assert.Equal("(unknown)", report.Areas[0].AreaName);
    }

    [Fact]
    public async Task GetVotersByArea_CustomMethods_PassedThrough()
    {
        SeedElection(customMethods: "Special1|Special2|Special3");
        AddPerson("P1", "A", canVote: true, area: "North", votingMethod: "1");

        var report = await _service.GetVotersByAreaAsync(_electionGuid);

        Assert.Equal("Special1", report.Custom1Name);
        Assert.Equal("Special2", report.Custom2Name);
        Assert.Equal("Special3", report.Custom3Name);
        Assert.Equal(1, report.Areas[0].Custom1);
    }
}
