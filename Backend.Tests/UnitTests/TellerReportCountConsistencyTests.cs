using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Analyzers;

namespace Backend.Tests.UnitTests;

/// <summary>
/// Front Desk, entered ballots, analysis, and VotersByArea must use the same
/// voted / eligible rules. No invented v3 golden totals.
/// </summary>
public class TellerReportCountConsistencyTests : ServiceTestBase
{
    private readonly Guid _electionGuid = Guid.NewGuid();
    private readonly Guid _locationGuid = Guid.NewGuid();

    [Fact]
    public async Task FrontDeskBallotsAnalysisAndVotersByArea_ShareEligibleAndVotedCounts()
    {
        SeedElection();
        var adultInPerson = AddPerson("Adult", "P", "P", area: "North");
        var youth = AddPerson(
            "Youth",
            "Y",
            "M",
            area: "North",
            canReceiveVotes: false,
            ineligibleReasonCode: IneligibleReasonEnum.V01_YouthAged181920.Code);
        var imported = AddPerson("Imported", "I", "I", area: "South");
        var online = AddPerson("Online", "O", votingMethod: null, area: "South");
        AddPerson("NotVoted", "N", votingMethod: null, area: "South");

        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = _electionGuid,
            PersonGuid = online.PersonGuid,
            Status = OnlineBallotStatus.Processed,
            WhenStatus = DateTimeOffset.UtcNow
        });

        AddBallot();
        AddBallot();
        AddBallot();
        AddBallot();
        await Context.SaveChangesAsync();

        var frontDesk = new FrontDeskService(
            Context,
            new Mock<ILogger<FrontDeskService>>().Object,
            new Mock<ISignalRNotificationService>().Object);
        var localizer = new Mock<IStringLocalizer<ReportService>>();
        var reports = new ReportService(Context, localizer.Object);

        var stats = await frontDesk.GetStatsAsync(_electionGuid);
        var voters = await frontDesk.GetEligibleVotersAsync(_electionGuid);
        var recon = await ElectionCountReconciliation.EvaluateAsync(Context, _electionGuid);
        var byArea = await reports.GetVotersByAreaAsync(_electionGuid);

        var election = Context.Elections.Single(e => e.ElectionGuid == _electionGuid);
        var analyzer = new ElectionAnalyzerNormal(Context, NullLogger.Instance, election);
        await analyzer.AnalyzeAsync();

        var summary = Context.ResultSummaries.Single(rs =>
            rs.ElectionGuid == _electionGuid && rs.ResultType == "F");
        var main = await reports.GetMainReportAsync(_electionGuid);

        Assert.Equal(5, stats.TotalEligible);
        Assert.Equal(4, stats.CheckedIn);
        Assert.Equal(4, voters.Count(v => v.IsCheckedIn));

        Assert.True(recon.IsReconciled);
        Assert.Equal(4, recon.FrontDeskCount);
        Assert.Equal(4, recon.BallotCount);

        Assert.Equal(5, byArea.Total.Eligible18Plus);
        Assert.Equal(1, byArea.Total.Eligible18To21);
        Assert.Equal(4, byArea.Total.Voted);
        Assert.Equal(1, byArea.Total.InPerson);
        Assert.Equal(1, byArea.Total.MailedIn);
        Assert.Equal(1, byArea.Total.Imported);
        Assert.Equal(1, byArea.Total.Online);
        Assert.True(byArea.ShowImported);

        Assert.Equal(5, summary.NumEligibleToVote);
        Assert.Equal(4, summary.NumVoters);
        Assert.Equal(4, summary.BallotsReceived);
        Assert.Equal(0, summary.SpoiledBallots);
        Assert.Equal(1, summary.InPersonBallots);
        Assert.Equal(1, summary.MailedInBallots);
        Assert.Equal(1, summary.ImportedBallots);
        Assert.Equal(1, summary.OnlineBallots);

        Assert.Equal(summary.NumEligibleToVote, main.NumEligibleToVote);
        Assert.Equal(summary.NumVoters, main.SumOfEnvelopesCollected);
        Assert.Equal(summary.BallotsReceived, main.NumBallotsWithManual);
        Assert.Equal(stats.TotalEligible, summary.NumEligibleToVote);
        Assert.Equal(stats.CheckedIn, summary.NumVoters);
        Assert.Equal(recon.BallotCount, (summary.BallotsReceived ?? 0) + (summary.SpoiledBallots ?? 0));
        Assert.Equal(byArea.Total.Voted, summary.NumVoters);
        Assert.Equal(byArea.Total.Eligible18Plus, summary.NumEligibleToVote);

        Assert.Contains(adultInPerson.PersonGuid, voters.Where(v => v.IsCheckedIn).Select(v => v.PersonGuid));
        Assert.Contains(youth.PersonGuid, voters.Where(v => v.IsCheckedIn).Select(v => v.PersonGuid));
        Assert.Contains(imported.PersonGuid, voters.Where(v => v.IsCheckedIn).Select(v => v.PersonGuid));
        Assert.Contains(online.PersonGuid, voters.Where(v => v.IsCheckedIn).Select(v => v.PersonGuid));
    }

    [Fact]
    public async Task FrontDeskVsBallots_Mismatch_WhenBallotMissing()
    {
        SeedElection();
        AddPerson("OnlyDesk", "D", "P", area: "North");
        await Context.SaveChangesAsync();

        var recon = await ElectionCountReconciliation.EvaluateAsync(Context, _electionGuid);

        Assert.False(recon.IsReconciled);
        Assert.Equal(1, recon.FrontDeskCount);
        Assert.Equal(0, recon.BallotCount);
        Assert.Contains(recon.Mismatches, m => m.Kind == CountReconciliationMismatchKinds.FrontDeskVsBallots);
    }

    private void SeedElection()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionGuid,
            Name = "Count consistency",
            ElectionType = "LSA",
            NumberToElect = 3,
            NumberExtra = 0,
            VotingMethods = "P,M,IM",
            ElectionStage = ElectionStage.Tallying,
            RowVersion = new byte[8]
        });
        Context.Locations.Add(new Location
        {
            ElectionGuid = _electionGuid,
            LocationGuid = _locationGuid,
            Name = "Hall"
        });
        Context.SaveChanges();
    }

    private Person AddPerson(
        string lastName,
        string firstName,
        string? votingMethod,
        string area,
        bool canReceiveVotes = true,
        string? ineligibleReasonCode = null)
    {
        var person = new Person
        {
            ElectionGuid = _electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = lastName,
            FirstName = firstName,
            CanVote = true,
            CanReceiveVotes = canReceiveVotes,
            VotingMethod = votingMethod,
            RegistrationTime = votingMethod == null ? null : DateTimeOffset.UtcNow,
            Area = area,
            IneligibleReasonCode = ineligibleReasonCode,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        return person;
    }

    private void AddBallot(BallotStatus status = BallotStatus.Ok)
    {
        Context.Ballots.Add(new Ballot
        {
            LocationGuid = _locationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = status,
            ComputerCode = "A",
            BallotNumAtComputer = Context.Ballots.Count() + 1,
            RowVersion = new byte[8]
        });
    }
}
