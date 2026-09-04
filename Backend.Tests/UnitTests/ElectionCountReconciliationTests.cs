using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class ElectionCountReconciliationTests : ServiceTestBase
{
    [Fact]
    public async Task EvaluateAsync_MatchingFrontDeskAndBallots_IsReconciled()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        await AddPersonAsync(electionGuid, "Ada", "Lovelace", votingMethod: "P");
        await AddPersonAsync(electionGuid, "Grace", "Hopper", votingMethod: "P");
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Empty, 2);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        Assert.True(report.IsReconciled);
        Assert.Empty(report.Mismatches);
        Assert.Equal(2, report.FrontDeskCount);
        Assert.Equal(2, report.BallotCount);
        Assert.Equal(1, report.SpoiledBallotCount);
    }

    [Fact]
    public async Task EvaluateAsync_SpoiledBallotsAreIncludedInBallotCount()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        await AddPersonAsync(electionGuid, "Ada", "Lovelace", votingMethod: "P");
        await AddPersonAsync(electionGuid, "Grace", "Hopper", votingMethod: "P");
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Dup, 2);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        Assert.True(report.IsReconciled);
        Assert.DoesNotContain(
            report.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.FrontDeskVsBallots);
        Assert.Equal(2, report.BallotCount);
        Assert.Equal(1, report.SpoiledBallotCount);
    }

    [Fact]
    public async Task EvaluateAsync_FrontDeskCountDoesNotMatchBallots_EmitsCountRow()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        await AddPersonAsync(electionGuid, "Ada", "Lovelace", votingMethod: "P");
        await AddPersonAsync(electionGuid, "Grace", "Hopper", votingMethod: "P");
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        Assert.False(report.IsReconciled);
        var row = Assert.Single(
            report.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.FrontDeskVsBallots);
        Assert.Equal(2, row.FrontDeskCount);
        Assert.Equal(1, row.BallotCount);
        Assert.Null(row.PersonName);
    }

    [Fact]
    public async Task EvaluateAsync_PendingOnline_ListsNamedVoterAndExcludesFromFrontDeskCount()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        await AddPersonAsync(electionGuid, "Ada", "Lovelace", votingMethod: "P");
        var pending = await AddPersonAsync(electionGuid, "Online", "Voter", votingMethod: "O");
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);
        await AddOnlineAsync(electionGuid, pending.PersonGuid, OnlineBallotStatus.Submitted);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        Assert.False(report.IsReconciled);
        Assert.Equal(1, report.FrontDeskCount);
        Assert.Equal(1, report.BallotCount);
        Assert.DoesNotContain(
            report.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.FrontDeskVsBallots);
        var row = Assert.Single(
            report.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.PendingOnline);
        Assert.Equal(pending.PersonGuid, row.PersonGuid);
        Assert.Equal("Online Voter", row.PersonName);
        Assert.Equal(OnlineBallotStatus.Submitted, row.OnlineStatus);
    }

    [Fact]
    public async Task EvaluateAsync_ProcessingOnline_IsPendingNotProcessed()
    {
        var electionGuid = await SeedElectionAsync();
        var person = await AddPersonAsync(electionGuid, "Pat", "Pending");
        await AddOnlineAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Processing);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        var row = Assert.Single(report.Mismatches);
        Assert.Equal(CountReconciliationMismatchKinds.PendingOnline, row.Kind);
        Assert.Equal(OnlineBallotStatus.Processing, row.OnlineStatus);
        Assert.Equal(0, report.FrontDeskCount);
    }

    [Fact]
    public async Task EvaluateAsync_ProcessedOnlineWithoutVotingMethod_CountsAsFrontDeskAndIsNotPending()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        var person = await AddPersonAsync(electionGuid, "Olly", "Online");
        await AddOnlineAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Processed);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        Assert.True(report.IsReconciled);
        Assert.Equal(1, report.FrontDeskCount);
        Assert.Equal(1, report.BallotCount);
        Assert.DoesNotContain(
            report.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.PendingOnline);
    }

    [Fact]
    public async Task EvaluateAsync_DuplicateEnvelope_ListsEachPerson()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        await AddPersonAsync(electionGuid, "Ada", "Lovelace", votingMethod: "M", envNum: 7);
        await AddPersonAsync(electionGuid, "Grace", "Hopper", votingMethod: "M", envNum: 7);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 2);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        var rows = report.Mismatches
            .Where(m => m.Kind == CountReconciliationMismatchKinds.DuplicateEnvelope)
            .ToList();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(7, r.EnvNum));
        Assert.Contains(rows, r => r.PersonName == "Ada Lovelace");
        Assert.Contains(rows, r => r.PersonName == "Grace Hopper");
    }

    [Fact]
    public async Task EvaluateAsync_PaperMethodAndOnline_IsDuplicateVotingPath()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        var person = await AddPersonAsync(electionGuid, "Both", "Ways", votingMethod: "P");
        await AddOnlineAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Processed);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        var row = Assert.Single(
            report.Mismatches,
            m => m.Kind == CountReconciliationMismatchKinds.DuplicateVotingPath);
        Assert.Equal(person.PersonGuid, row.PersonGuid);
        Assert.Equal("Both Ways", row.PersonName);
        Assert.Equal("P", row.VotingMethod);
        Assert.Equal(OnlineBallotStatus.Processed, row.OnlineStatus);
    }

    [Fact]
    public async Task EvaluateAsync_DoesNotLogOrReturnEmailOrPhone()
    {
        var electionGuid = await SeedElectionAsync();
        var person = await AddPersonAsync(
            electionGuid,
            "Secret",
            "Voter",
            votingMethod: null,
            email: "secret.voter@example.com",
            phone: "555-0199");
        await AddOnlineAsync(electionGuid, person.PersonGuid, OnlineBallotStatus.Submitted);

        var report = await ElectionCountReconciliation.EvaluateAsync(Context, electionGuid);

        var json = System.Text.Json.JsonSerializer.Serialize(report);
        Assert.DoesNotContain("secret.voter@example.com", json);
        Assert.DoesNotContain("555-0199", json);
        Assert.Equal("Secret Voter", report.Mismatches[0].PersonName);
        Assert.Equal("secret.voter@example.com", GetPersonEmail(person.PersonGuid));
    }

    [Fact]
    public async Task EnsureReconciledAsync_ThrowsWhenMismatchesExist()
    {
        var electionGuid = await SeedElectionAsync();
        var location = await AddLocationAsync(electionGuid);
        await AddBallotAsync(location.LocationGuid, BallotStatus.Ok, 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ElectionCountReconciliation.EnsureReconciledAsync(Context, electionGuid));

        Assert.StartsWith(ElectionStageMessageKeys.CountsDoNotReconcile, ex.Message);
        Assert.Contains("count=1", ex.Message);
    }

    private async Task<Guid> SeedElectionAsync()
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Reconciliation Election",
            ElectionType = "LSA",
            NumberToElect = 3,
            ElectionStage = ElectionStage.ProcessingBallots,
            DateOfElection = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
        return electionGuid;
    }

    private async Task<Location> AddLocationAsync(Guid electionGuid)
    {
        var location = new Location
        {
            ElectionGuid = electionGuid,
            LocationGuid = Guid.NewGuid(),
            Name = "Hall"
        };
        Context.Locations.Add(location);
        await Context.SaveChangesAsync();
        return location;
    }

    private async Task<Person> AddPersonAsync(
        Guid electionGuid,
        string first,
        string last,
        string? votingMethod = null,
        int? envNum = null,
        string? email = null,
        string? phone = null)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = first,
            LastName = last,
            CanVote = true,
            VotingMethod = votingMethod,
            EnvNum = envNum,
            Email = email,
            Phone = phone,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();
        return person;
    }

    private async Task AddBallotAsync(Guid locationGuid, BallotStatus status, int number)
    {
        Context.Ballots.Add(new Ballot
        {
            LocationGuid = locationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = status,
            ComputerCode = "A",
            BallotNumAtComputer = number,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
    }

    private async Task AddOnlineAsync(Guid electionGuid, Guid personGuid, string status)
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

    private string? GetPersonEmail(Guid personGuid) =>
        Context.People.Single(p => p.PersonGuid == personGuid).Email;
}
