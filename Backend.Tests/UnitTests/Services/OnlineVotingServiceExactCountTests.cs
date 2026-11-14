using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class OnlineVotingServiceExactCountTests : ServiceTestBase
{
    private readonly OnlineVotingService _service;

    public OnlineVotingServiceExactCountTests()
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
            new OnlineBallotAcceptLock());
    }

    [Fact]
    public async Task Submit_ListMode_RejectsFewerOrMoreThanNumberToElect()
    {
        var election = await SeedElectionAsync("A", numberToElect: 2);
        var email = await SeedVoterAsync(election);
        var ann = await SeedCandidateAsync(election, "Ann", "Doe");
        var bo = await SeedCandidateAsync(election, "Bo", "Lee");
        var cy = await SeedCandidateAsync(election, "Cy", "Ng");

        var tooFew = await SubmitAsync(election, email, false, Vote(ann, "Doe, Ann"));
        Assert.False(tooFew.Success);
        Assert.Equal(OnlineBallotNameCount.ExactCountError, tooFew.Error);
        Assert.Empty(Context.OnlineVotingInfos);

        var duplicate = await SubmitAsync(
            election,
            email,
            false,
            Vote(ann, "Doe, Ann"),
            Vote(ann, "Doe, Ann", position: 2));
        Assert.False(duplicate.Success);
        Assert.Equal(OnlineBallotNameCount.ExactCountError, duplicate.Error);

        var typedOnly = await SubmitAsync(
            election,
            email,
            false,
            new OnlineVoteDto { VoteName = "Ann Doe", PositionOnBallot = 1 },
            new OnlineVoteDto { VoteName = "Bo Lee", PositionOnBallot = 2 });
        Assert.False(typedOnly.Success);
        Assert.Equal(OnlineBallotNameCount.ExactCountError, typedOnly.Error);

        var exact = await SubmitAsync(
            election,
            email,
            false,
            Vote(ann, "Doe, Ann"),
            Vote(bo, "Lee, Bo", position: 2));
        Assert.True(exact.Success);
        Assert.Equal(OnlineBallotStatus.Submitted, Context.OnlineVotingInfos.Single().Status);

        var tooMany = await SubmitAsync(
            election,
            email,
            false,
            Vote(ann, "Doe, Ann"),
            Vote(bo, "Lee, Bo", position: 2),
            Vote(cy, "Ng, Cy", position: 3));
        Assert.False(tooMany.Success);
        Assert.Equal(OnlineBallotNameCount.ExactCountError, tooMany.Error);
        Assert.Contains("Doe, Ann", Context.OnlineVotingInfos.Single().ListPool);
        Assert.DoesNotContain("Ng, Cy", Context.OnlineVotingInfos.Single().ListPool);
    }

    [Fact]
    public async Task Submit_Draft_AllowsAShortBallot()
    {
        var election = await SeedElectionAsync("A", numberToElect: 2);
        var email = await SeedVoterAsync(election);
        var ann = await SeedCandidateAsync(election, "Ann", "Doe");

        var draft = await SubmitAsync(election, email, true, Vote(ann, "Doe, Ann"));
        Assert.True(draft.Success);
        Assert.Equal(OnlineBallotStatus.Draft, Context.OnlineVotingInfos.Single().Status);
    }

    [Fact]
    public async Task Submit_RandomMode_RequiresDistinctTypedNames()
    {
        var election = await SeedElectionAsync("B", numberToElect: 2);
        var email = await SeedVoterAsync(election);

        var duplicateName = await SubmitAsync(
            election,
            email,
            false,
            new OnlineVoteDto { VoteName = "Ann Doe", PositionOnBallot = 1 },
            new OnlineVoteDto { VoteName = "ann   doe", PositionOnBallot = 2 });
        Assert.False(duplicateName.Success);
        Assert.Equal(OnlineBallotNameCount.ExactCountError, duplicateName.Error);

        var exact = await SubmitAsync(
            election,
            email,
            false,
            new OnlineVoteDto { VoteName = "Ann Doe", PositionOnBallot = 1 },
            new OnlineVoteDto { VoteName = "Bo Lee", PositionOnBallot = 2 });
        Assert.True(exact.Success);
    }

    private async Task<Guid> SeedElectionAsync(string selectionProcess, int numberToElect)
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Exact count",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            NumberToElect = numberToElect,
            OnlineSelectionProcess = selectionProcess,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
        return electionGuid;
    }

    private async Task<string> SeedVoterAsync(Guid electionGuid)
    {
        var email = $"voter_{Guid.NewGuid():N}@example.com";
        Context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Voter",
            Email = email,
            CanVote = true,
            CanReceiveVotes = false,
            RowVersion = new byte[8]
        });
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = email,
            VoterIdType = "E",
            WhenRegistered = DateTimeOffset.UtcNow
        });
        await Context.SaveChangesAsync();
        return email;
    }

    private async Task<Guid> SeedCandidateAsync(Guid electionGuid, string first, string last)
    {
        var personGuid = Guid.NewGuid();
        Context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            FirstName = first,
            LastName = last,
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
        return personGuid;
    }

    private Task<(bool Success, string? Error)> SubmitAsync(
        Guid electionGuid,
        string email,
        bool isDraft,
        params OnlineVoteDto[] votes)
    {
        return _service.SubmitBallotAsync(new SubmitOnlineBallotDto
        {
            ElectionGuid = electionGuid,
            VoterId = email,
            Votes = votes.ToList(),
            IsDraft = isDraft
        });
    }

    private static OnlineVoteDto Vote(Guid personGuid, string name, int position = 1)
    {
        return new OnlineVoteDto
        {
            PersonGuid = personGuid,
            VoteName = name,
            PositionOnBallot = position
        };
    }
}
