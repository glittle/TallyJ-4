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

/// <summary>
/// A blocked paid channel returns the same message key as a successful send and does not call the provider.
/// </summary>
public class OnlineVotingServicePaidSendGateTests : ServiceTestBase
{
    [Fact]
    public async Task RequestCode_BlockedPaidChannel_ReturnsSentWithoutCallingProvider()
    {
        var electionId = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionId,
            Name = "Blocked",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        Context.People.Add(new Person
        {
            ElectionGuid = electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Phone = "+14168972671",
            CanVote = true,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var paid = new Mock<IPaidVerificationSender>();
        var guard = new Mock<ICodeSendGuard>();
        guard.Setup(item => item.ReserveAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                "sms",
                "+14168972671",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeSendReservation(false, "not-approved", electionId, null, false));

        var host = new Mock<IHostEnvironment>();
        host.Setup(item => item.EnvironmentName).Returns("Testing");
        var service = new OnlineVotingService(
            Context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            host.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<IEmailSender>(),
            paid.Object,
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            Mock.Of<IOnlineBallotAcceptLock>(),
            codeSendGuard: guard.Object);

        var result = await service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = "+14168972671",
            VoterIdType = "P",
            DeliveryMethod = "sms"
        });

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        Assert.Null(result.DevVerificationCode);
        paid.Verify(sender => sender.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestCode_Email_StillSendsWhenGuardAllowsIt()
    {
        var electionId = Guid.NewGuid();
        const string email = "ada@example.com";
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionId,
            Name = "Email",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });
        Context.People.Add(new Person
        {
            ElectionGuid = electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Voter",
            Email = email,
            CanVote = true,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var emailSender = new Mock<IEmailSender>();
        emailSender.Setup(sender => sender.SendAsync(It.IsAny<MimeKit.MimeMessage>())).Returns(Task.CompletedTask);
        var guard = new Mock<ICodeSendGuard>();
        guard.Setup(item => item.ReserveAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                "email",
                email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeSendReservation(true, null, electionId, Guid.NewGuid(), false));

        var host = new Mock<IHostEnvironment>();
        host.Setup(item => item.EnvironmentName).Returns("Testing");
        var service = new OnlineVotingService(
            Context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            host.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            emailSender.Object,
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            Mock.Of<IOnlineBallotAcceptLock>(),
            codeSendGuard: guard.Object);

        var result = await service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        Assert.False(string.IsNullOrWhiteSpace(result.DevVerificationCode));
        guard.Verify(item => item.LogOutcomeAsync(
            It.Is<CodeSendReservation>(reservation => reservation.Allowed),
            "email",
            email,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
