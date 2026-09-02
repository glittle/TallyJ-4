using Backend;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Identity;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class OnlineVotingServiceAcceptAllTests : ServiceTestBase
{
    private readonly OnlineBallotAcceptLock _lock = new();
    private readonly OnlineVotingService _service;

    public OnlineVotingServiceAcceptAllTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(e => e.EnvironmentName).Returns("Testing");
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>()))
            .Returns(Task.CompletedTask);

        _service = new OnlineVotingService(
            Context,
            configuration,
            hostEnvironment.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            emailSender.Object,
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            _lock);
    }

    [Fact]
    public async Task Submit_DoesNotCreateRegularBallot_UntilAcceptAll()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);

        var submit = await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        Assert.True(submit.Success);
        Assert.Equal(0, Context.Ballots.Count());
        Assert.Equal(OnlineBallotStatus.Submitted, Context.OnlineVotingInfos.Single().Status);
        Assert.Null(Context.OnlineVotingInfos.Single().BallotGuid);
        Assert.False(string.IsNullOrWhiteSpace(Context.OnlineVotingInfos.Single().ListPool));
    }

    [Fact]
    public async Task AcceptAll_CreatesOneRegularBallot_AndWipesOnlinePayload()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);

        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(0, result.PendingRemaining);
        Assert.Equal(1, Context.Ballots.Count());
        var ballot = Context.Ballots.Single();
        Assert.Equal(ComputerCodeHelper.Online, ballot.ComputerCode);
        Assert.Equal(1, Context.Votes.Count(v => v.BallotGuid == ballot.BallotGuid));

        var ovi = Context.OnlineVotingInfos.Single();
        Assert.Equal(OnlineBallotStatus.Processed, ovi.Status);
        Assert.Null(ovi.ListPool);
        Assert.Null(ovi.BallotGuid);
        Assert.Null(ovi.PoolLocked);
        Assert.Contains(OnlineBallotStatus.Processed, ovi.HistoryStatus);
    }

    [Fact]
    public async Task AcceptAll_SecondRun_DoesNotCreateAnotherBallot()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);

        await _service.AcceptAllPendingAsync(election.ElectionGuid);
        var second = await _service.AcceptAllPendingAsync(election.ElectionGuid);

        Assert.True(second.Success);
        Assert.Equal(0, second.AcceptedCount);
        Assert.Equal("monitoring.acceptAll.nonePending", second.MessageKey);
        Assert.Equal(1, Context.Ballots.Count());
    }

    [Fact]
    public async Task AcceptAll_WhileLockHeld_DoesNotCreateBallots()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);

        Assert.True(_lock.TryEnter(election.ElectionGuid));
        try
        {
            var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);
            Assert.False(result.Success);
            Assert.True(result.AlreadyInProgress);
            Assert.Equal("monitoring.acceptAll.inProgress", result.MessageKey);
            Assert.Equal(0, Context.Ballots.Count());
            Assert.Equal(OnlineBallotStatus.Submitted, Context.OnlineVotingInfos.Single().Status);
        }
        finally
        {
            _lock.Exit(election.ElectionGuid);
        }
    }

    [Fact]
    public async Task AcceptAll_DoesNotRequireOnlineWindowClosed()
    {
        var election = await SeedOpenElectionAsync();
        Assert.True(election.OnlineWhenClose > DateTimeOffset.UtcNow);

        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);
    }

    [Fact]
    public async Task AcceptAll_TwoPeople_ThenNewSubmit_SecondRunAcceptsOnlyNewPending()
    {
        var election = await SeedOpenElectionAsync();
        var first = await SeedVoterAsync(election.ElectionGuid, "one@example.com");
        var second = await SeedVoterAsync(election.ElectionGuid, "two@example.com");
        await SubmitPendingAsync(election.ElectionGuid, first.Email, first.Person.PersonGuid);
        await SubmitPendingAsync(election.ElectionGuid, second.Email, second.Person.PersonGuid);

        var firstRun = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.Equal(2, firstRun.AcceptedCount);
        Assert.Equal(2, Context.Ballots.Count());

        var third = await SeedVoterAsync(election.ElectionGuid, "three@example.com");
        await SubmitPendingAsync(election.ElectionGuid, third.Email, third.Person.PersonGuid);

        var secondRun = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.Equal(1, secondRun.AcceptedCount);
        Assert.Equal(0, secondRun.PendingRemaining);
        Assert.Equal(3, Context.Ballots.Count());
        Assert.Equal(0, Context.OnlineVotingInfos.Count(o => o.Status == OnlineBallotStatus.Submitted));
        Assert.Equal(3, Context.OnlineVotingInfos.Count(o => o.Status == OnlineBallotStatus.Processed));
    }

    [Fact]
    public async Task Submit_AfterProcessed_IsRejected()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        await _service.AcceptAllPendingAsync(election.ElectionGuid);

        var again = await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        Assert.False(again.Success);
        Assert.Equal("voting.submit.alreadyProcessed", again.Error);
        Assert.Equal(1, Context.Ballots.Count());
    }

    [Fact]
    public async Task Submit_WhilePending_ReplacesPayload_DoesNotCreateBallot()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid, voteName: "First Name");

        var second = await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid, voteName: "Changed Name");
        Assert.True(second.Success);
        Assert.Equal(0, Context.Ballots.Count());
        Assert.Equal(1, Context.OnlineVotingInfos.Count());

        var status = await _service.GetVoteStatusAsync(election.ElectionGuid, email);
        Assert.True(status.CanChangeVote);
        Assert.Contains(status.PriorVotes, v => v.VoteName == "Changed Name");
    }

    [Fact]
    public async Task GetVoteStatus_AfterProcessed_CannotChangeVote()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        await _service.AcceptAllPendingAsync(election.ElectionGuid);

        var status = await _service.GetVoteStatusAsync(election.ElectionGuid, email);
        Assert.True(status.HasVoted);
        Assert.False(status.CanChangeVote);
        Assert.Equal("voting.status.alreadyProcessed", status.Message);
        Assert.Empty(status.PriorVotes);
    }

    [Fact]
    public async Task GetAcceptAllSummary_CountsPendingAndProcessed()
    {
        var election = await SeedOpenElectionAsync();
        var first = await SeedVoterAsync(election.ElectionGuid, "a@example.com");
        var second = await SeedVoterAsync(election.ElectionGuid, "b@example.com");
        await SubmitPendingAsync(election.ElectionGuid, first.Email, first.Person.PersonGuid);
        await SubmitPendingAsync(election.ElectionGuid, second.Email, second.Person.PersonGuid);
        await _service.AcceptAllPendingAsync(election.ElectionGuid);

        var third = await SeedVoterAsync(election.ElectionGuid, "c@example.com");
        await SubmitPendingAsync(election.ElectionGuid, third.Email, third.Person.PersonGuid);

        var summary = await _service.GetAcceptAllSummaryAsync(election.ElectionGuid);
        Assert.NotNull(summary);
        Assert.Equal(1, summary.PendingCount);
        Assert.Equal(2, summary.ProcessedCount);
    }

    [Fact]
    public async Task AcceptAll_FinalizedElection_DoesNotCreateBallots()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        election.ElectionStage = ElectionStage.Finalized;
        await Context.SaveChangesAsync();

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.False(result.Success);
        Assert.Equal("monitoring.acceptAll.finalized", result.MessageKey);
        Assert.Equal(0, Context.Ballots.Count());
    }

    [Fact]
    public async Task AcceptAll_LegacySubmittedWithBallot_DoesNotCreateSecondBallot()
    {
        var election = await SeedOpenElectionAsync();
        var (person, _) = await SeedVoterAsync(election.ElectionGuid);
        await SeedLegacySubmittedWithBallotAsync(election, person);

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(1, Context.Ballots.Count());
        var ovi = Context.OnlineVotingInfos.Single();
        Assert.Equal(OnlineBallotStatus.Processed, ovi.Status);
        Assert.Null(ovi.BallotGuid);
        Assert.Null(ovi.ListPool);
    }

    [Fact]
    public async Task Submit_LegacySubmittedWithBallot_IsRejected_AndAcceptAllDoesNotCreateSecondBallot()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        var ballotGuid = await SeedLegacySubmittedWithBallotAsync(election, person);

        var status = await _service.GetVoteStatusAsync(election.ElectionGuid, email);
        Assert.False(status.CanChangeVote);
        Assert.Equal("voting.status.alreadyProcessed", status.Message);

        var submit = await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        Assert.False(submit.Success);
        Assert.Equal("voting.submit.alreadyProcessed", submit.Error);
        Assert.Equal(1, Context.Ballots.Count());
        Assert.Equal(ballotGuid, Context.OnlineVotingInfos.Single().BallotGuid);
        Assert.Equal(OnlineBallotStatus.Submitted, Context.OnlineVotingInfos.Single().Status);

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(1, Context.Ballots.Count());
        var ovi = Context.OnlineVotingInfos.Single();
        Assert.Equal(OnlineBallotStatus.Processed, ovi.Status);
        Assert.Null(ovi.BallotGuid);
        Assert.Null(ovi.ListPool);
    }

    [Fact]
    public async Task AcceptAll_ProcessingRowFromPriorRun_CompletesWithoutSecondBallot()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        Context.OnlineVotingInfos.Single().Status = OnlineBallotStatus.Processing;
        await Context.SaveChangesAsync();

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);

        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(1, Context.Ballots.Count());
        Assert.Equal(OnlineBallotStatus.Processed, Context.OnlineVotingInfos.Single().Status);
        Assert.Null(Context.OnlineVotingInfos.Single().ListPool);
    }

    [Fact]
    public async Task Submit_WhileProcessing_IsRejected_AndAcceptAllCreatesOneBallot()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        Context.OnlineVotingInfos.Single().Status = OnlineBallotStatus.Processing;
        await Context.SaveChangesAsync();

        var status = await _service.GetVoteStatusAsync(election.ElectionGuid, email);
        Assert.False(status.CanChangeVote);
        Assert.Equal("voting.status.alreadyProcessed", status.Message);

        var submit = await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid, voteName: "Changed");
        Assert.False(submit.Success);
        Assert.Equal("voting.submit.alreadyProcessed", submit.Error);
        Assert.Equal(0, Context.Ballots.Count());
        Assert.Equal(OnlineBallotStatus.Processing, Context.OnlineVotingInfos.Single().Status);

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid);
        Assert.True(result.Success);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(1, Context.Ballots.Count());
    }

    [Fact]
    public async Task GetAvailableElections_SetsCanChangeVoteFromCannotChangeRules()
    {
        var pendingElection = await SeedOpenElectionAsync();
        pendingElection.Name = "Pending election";
        var processingElection = await SeedOpenElectionAsync();
        processingElection.Name = "Processing election";
        var processedElection = await SeedOpenElectionAsync();
        processedElection.Name = "Processed election";
        var legacyElection = await SeedOpenElectionAsync();
        legacyElection.Name = "Legacy election";
        await Context.SaveChangesAsync();

        var email = "same-voter@example.com";
        var pending = await SeedVoterAsync(pendingElection.ElectionGuid, email);
        var processing = await SeedVoterAsync(processingElection.ElectionGuid, email);
        var processed = await SeedVoterAsync(processedElection.ElectionGuid, email);
        var legacy = await SeedVoterAsync(legacyElection.ElectionGuid, email);

        await SubmitPendingAsync(pendingElection.ElectionGuid, email, pending.Person.PersonGuid);
        await SubmitPendingAsync(processingElection.ElectionGuid, email, processing.Person.PersonGuid);
        Context.OnlineVotingInfos.Single(o => o.ElectionGuid == processingElection.ElectionGuid)
            .Status = OnlineBallotStatus.Processing;
        await SubmitPendingAsync(processedElection.ElectionGuid, email, processed.Person.PersonGuid);
        await _service.AcceptAllPendingAsync(processedElection.ElectionGuid);
        await SeedLegacySubmittedWithBallotAsync(legacyElection, legacy.Person);
        await Context.SaveChangesAsync();

        var elections = await _service.GetAvailableElectionsAsync(email);
        Assert.Equal(4, elections.Count);

        var pendingDto = elections.Single(e => e.ElectionGuid == pendingElection.ElectionGuid);
        Assert.Equal(OnlineBallotStatus.Submitted, pendingDto.BallotStatus);
        Assert.True(pendingDto.CanChangeVote);

        var processingDto = elections.Single(e => e.ElectionGuid == processingElection.ElectionGuid);
        Assert.Equal(OnlineBallotStatus.Processing, processingDto.BallotStatus);
        Assert.False(processingDto.CanChangeVote);

        var processedDto = elections.Single(e => e.ElectionGuid == processedElection.ElectionGuid);
        Assert.Equal(OnlineBallotStatus.Processed, processedDto.BallotStatus);
        Assert.False(processedDto.CanChangeVote);

        var legacyDto = elections.Single(e => e.ElectionGuid == legacyElection.ElectionGuid);
        Assert.Equal(OnlineBallotStatus.Submitted, legacyDto.BallotStatus);
        Assert.False(legacyDto.CanChangeVote);
    }

    [Fact]
    public async Task GetAcceptAllSummary_CountsProcessingAsPending()
    {
        var election = await SeedOpenElectionAsync();
        var first = await SeedVoterAsync(election.ElectionGuid, "a@example.com");
        var second = await SeedVoterAsync(election.ElectionGuid, "b@example.com");
        await SubmitPendingAsync(election.ElectionGuid, first.Email, first.Person.PersonGuid);
        await SubmitPendingAsync(election.ElectionGuid, second.Email, second.Person.PersonGuid);
        Context.OnlineVotingInfos.OrderBy(o => o.RowId).First().Status = OnlineBallotStatus.Processing;
        await Context.SaveChangesAsync();

        var summary = await _service.GetAcceptAllSummaryAsync(election.ElectionGuid);
        Assert.NotNull(summary);
        Assert.Equal(2, summary.PendingCount);
        Assert.Equal(0, summary.ProcessedCount);
    }

    [Fact]
    public async Task AcceptAll_WritesAudit_WithWhoWhenAndCounts()
    {
        var election = await SeedOpenElectionAsync();
        var first = await SeedVoterAsync(election.ElectionGuid, "one@example.com");
        var second = await SeedVoterAsync(election.ElectionGuid, "two@example.com");
        await SubmitPendingAsync(election.ElectionGuid, first.Email, first.Person.PersonGuid);
        await SubmitPendingAsync(election.ElectionGuid, second.Email, second.Person.PersonGuid);
        await SeedTellerAsync("teller-1", "Jane Teller", "teller@example.com");

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid, "teller-1");

        Assert.True(result.Success);
        Assert.Equal(2, result.AcceptedCount);
        var log = Assert.Single(Context.SecurityAuditLogs);
        Assert.True(AcceptAllOnlineBallotsAudit.IsAcceptAllLog(log));
        Assert.Equal("teller-1", log.UserId);
        Assert.Equal(election.ElectionGuid, log.ElectionGuid);
        Assert.Null(log.Email);
        Assert.Null(log.OnlineVoterId);
        Assert.True(log.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.DoesNotContain("one@example.com", log.Details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("two@example.com", log.Details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("teller@example.com", log.Details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("one@example.com", log.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("teller@example.com", log.MetadataJson, StringComparison.OrdinalIgnoreCase);

        var run = AcceptAllOnlineBallotsAudit.ToRunDto(log);
        Assert.Equal("Jane Teller", run.AcceptedBy);
        Assert.Equal("teller-1", run.AcceptedByUserId);
        Assert.Equal(2, run.PendingBefore);
        Assert.Equal(0, run.AcceptedBefore);
        Assert.Equal(0, run.PendingAfter);
        Assert.Equal(2, run.AcceptedAfter);
    }

    [Fact]
    public async Task AcceptAll_SecondRun_WritesItsOwnAuditRow()
    {
        var election = await SeedOpenElectionAsync();
        var first = await SeedVoterAsync(election.ElectionGuid, "one@example.com");
        await SubmitPendingAsync(election.ElectionGuid, first.Email, first.Person.PersonGuid);
        await _service.AcceptAllPendingAsync(election.ElectionGuid, "teller-1");

        var second = await SeedVoterAsync(election.ElectionGuid, "two@example.com");
        await SubmitPendingAsync(election.ElectionGuid, second.Email, second.Person.PersonGuid);
        await _service.AcceptAllPendingAsync(election.ElectionGuid, "teller-2");

        var logs = Context.SecurityAuditLogs
            .Where(AcceptAllOnlineBallotsAudit.IsAcceptAllLog)
            .OrderBy(l => l.Timestamp)
            .ToList();
        Assert.Equal(2, logs.Count);
        var firstRun = AcceptAllOnlineBallotsAudit.ToRunDto(logs[0]);
        var secondRun = AcceptAllOnlineBallotsAudit.ToRunDto(logs[1]);
        Assert.Equal("teller-1", firstRun.AcceptedByUserId);
        Assert.Equal(1, firstRun.PendingBefore);
        Assert.Equal(0, firstRun.AcceptedBefore);
        Assert.Equal(0, firstRun.PendingAfter);
        Assert.Equal(1, firstRun.AcceptedAfter);
        Assert.Equal("teller-2", secondRun.AcceptedByUserId);
        Assert.Equal(1, secondRun.PendingBefore);
        Assert.Equal(1, secondRun.AcceptedBefore);
        Assert.Equal(0, secondRun.PendingAfter);
        Assert.Equal(2, secondRun.AcceptedAfter);
    }

    [Fact]
    public async Task AcceptAll_WhileLockHeld_DoesNotWriteAudit()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);

        Assert.True(_lock.TryEnter(election.ElectionGuid));
        try
        {
            var result = await _service.AcceptAllPendingAsync(election.ElectionGuid, "teller-1");
            Assert.True(result.AlreadyInProgress);
            Assert.Empty(Context.SecurityAuditLogs);
        }
        finally
        {
            _lock.Exit(election.ElectionGuid);
        }
    }

    [Fact]
    public async Task AcceptAll_FinalizedElection_DoesNotWriteAudit()
    {
        var election = await SeedOpenElectionAsync();
        var (person, email) = await SeedVoterAsync(election.ElectionGuid);
        await SubmitPendingAsync(election.ElectionGuid, email, person.PersonGuid);
        election.ElectionStage = ElectionStage.Finalized;
        await Context.SaveChangesAsync();

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid, "teller-1");
        Assert.False(result.Success);
        Assert.Empty(Context.SecurityAuditLogs);
    }

    [Fact]
    public async Task AcceptAll_MissingElection_DoesNotWriteAudit()
    {
        var result = await _service.AcceptAllPendingAsync(Guid.NewGuid(), "teller-1");
        Assert.False(result.Success);
        Assert.Empty(Context.SecurityAuditLogs);
    }

    [Fact]
    public async Task AcceptAll_NonePending_WritesZeroCountAudit()
    {
        var election = await SeedOpenElectionAsync();

        var result = await _service.AcceptAllPendingAsync(election.ElectionGuid, "teller-1");

        Assert.True(result.Success);
        Assert.Equal(0, result.AcceptedCount);
        var log = Assert.Single(Context.SecurityAuditLogs);
        var run = AcceptAllOnlineBallotsAudit.ToRunDto(log);
        Assert.Equal(0, run.PendingBefore);
        Assert.Equal(0, run.AcceptedBefore);
        Assert.Equal(0, run.PendingAfter);
        Assert.Equal(0, run.AcceptedAfter);
        Assert.Equal("teller-1", run.AcceptedByUserId);
    }

    private async Task SeedTellerAsync(string userId, string displayName, string email)
    {
        Context.Users.Add(new AppUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            DisplayName = displayName,
            AuthMethod = "Local"
        });
        await Context.SaveChangesAsync();
    }

    private async Task<Guid> SeedLegacySubmittedWithBallotAsync(Election election, Person person)
    {
        var location = new Location
        {
            LocationGuid = Guid.NewGuid(),
            ElectionGuid = election.ElectionGuid,
            Name = "Online",
            LocationTypeCode = nameof(LocationType.Online),
            LocationTallyStatus = LocationTallyStatus.NotStarted
        };
        Context.Locations.Add(location);
        var ballotGuid = Guid.NewGuid();
        Context.Ballots.Add(new Ballot
        {
            BallotGuid = ballotGuid,
            LocationGuid = location.LocationGuid,
            ComputerCode = ComputerCodeHelper.Online,
            BallotNumAtComputer = 1,
            BallotCode = "OL1",
            StatusCode = BallotStatus.Ok,
            RowVersion = new byte[8]
        });
        Context.OnlineVotingInfos.Add(new OnlineVotingInfo
        {
            ElectionGuid = election.ElectionGuid,
            PersonGuid = person.PersonGuid,
            BallotGuid = ballotGuid,
            Status = OnlineBallotStatus.Submitted,
            ListPool = """[{"FullName":"Pool Person"}]""",
            WhenBallotCreated = DateTimeOffset.UtcNow
        });
        await Context.SaveChangesAsync();
        return ballotGuid;
    }

    private async Task<Election> SeedOpenElectionAsync()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Accept-all election",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            NumberToElect = 9,
            RowVersion = new byte[8]
        };
        Context.Elections.Add(election);
        await Context.SaveChangesAsync();
        return election;
    }

    private async Task<(Person Person, string Email)> SeedVoterAsync(Guid electionGuid, string? email = null)
    {
        email ??= $"voter_{Guid.NewGuid():N}@example.com";
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Voter",
            Email = email,
            CanVote = true,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        if (!Context.OnlineVoters.Any(ov => ov.VoterId == email))
        {
            Context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = email,
                VoterIdType = "E",
                WhenRegistered = DateTimeOffset.UtcNow
            });
        }

        await Context.SaveChangesAsync();
        return (person, email);
    }

    private Task<(bool Success, string? Error)> SubmitPendingAsync(
        Guid electionGuid,
        string email,
        Guid personGuid,
        string voteName = "Free Voter")
    {
        return _service.SubmitBallotAsync(new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            Votes =
            [
                new OnlineVoteDto
                {
                    PersonGuid = personGuid,
                    VoteName = voteName,
                    PositionOnBallot = 1
                }
            ]
        });
    }
}
