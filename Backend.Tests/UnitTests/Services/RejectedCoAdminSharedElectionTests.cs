using Backend.Configuration;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// A rejected co-admin does not hide an election that still has an approved owner.
/// </summary>
public class RejectedCoAdminSharedElectionTests : ServiceTestBase
{
    [Fact]
    public async Task SharedElection_StaysVisible_AndIssuesAVoterCode_AfterCoAdminIsRejected()
    {
        var sharedId = Guid.NewGuid();
        var soleId = Guid.NewGuid();
        var approvedId = Guid.NewGuid();
        var rejectedId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Context.Elections.AddRange(
            new Election
            {
                ElectionGuid = sharedId,
                Name = "Shared live",
                UseOnlineVoting = true,
                OnlineWhenOpen = now.AddHours(-1),
                OnlineWhenClose = now.AddHours(2),
                ElectionStage = ElectionStage.GatheringBallots,
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = soleId,
                Name = "Rejected only",
                UseOnlineVoting = true,
                OnlineWhenOpen = now.AddHours(-1),
                OnlineWhenClose = now.AddHours(2),
                ElectionStage = ElectionStage.GatheringBallots,
                RowVersion = new byte[8]
            });
        Context.JoinElectionUsers.AddRange(
            new JoinElectionUser { ElectionGuid = sharedId, UserId = approvedId, Role = "Owner" },
            new JoinElectionUser { ElectionGuid = sharedId, UserId = rejectedId, Role = "Admin" },
            new JoinElectionUser { ElectionGuid = soleId, UserId = rejectedId, Role = "Admin" });
        Context.OwnerSendControls.AddRange(
            new OwnerSendControl
            {
                UserId = approvedId,
                RestrictionStatus = OwnerRestrictionStatus.Approved,
                PaidSendsApproved = true
            },
            new OwnerSendControl
            {
                UserId = rejectedId,
                RestrictionStatus = OwnerRestrictionStatus.Rejected,
                PaidSendsApproved = false
            });
        Context.People.AddRange(
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = sharedId,
                FirstName = "Ada",
                LastName = "Shared",
                Email = "shared-voter@example.com",
                CanVote = true,
                RowVersion = new byte[8]
            },
            new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = soleId,
                FirstName = "Bea",
                LastName = "Hidden",
                Email = "hidden-voter@example.com",
                CanVote = true,
                RowVersion = new byte[8]
            });
        await Context.SaveChangesAsync();

        var email = new CapturingEmailSender();
        var service = CreateService(email);

        var visible = await service.GetAvailableElectionsAsync("shared-voter@example.com");
        Assert.Contains(visible, election => election.ElectionGuid == sharedId);
        Assert.Empty(await service.GetAvailableElectionsAsync("hidden-voter@example.com"));

        var sharedCode = await service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = "shared-voter@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        });
        Assert.False(string.IsNullOrEmpty(sharedCode.DevVerificationCode));
        Assert.Contains(email.Messages, message =>
            message.To.Mailboxes.Any(box => box.Address == "shared-voter@example.com"));

        var hiddenCode = await service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = "hidden-voter@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        });
        Assert.True(string.IsNullOrEmpty(hiddenCode.DevVerificationCode));
        Assert.DoesNotContain(email.Messages, message =>
            message.To.Mailboxes.Any(box => box.Address == "hidden-voter@example.com"));
    }

    private OnlineVotingService CreateService(CapturingEmailSender email)
    {
        var host = new Mock<IHostEnvironment>();
        host.Setup(item => item.EnvironmentName).Returns("Testing");
        var guard = new CodeSendGuard(
            Context,
            new PaidSendCounters(Context),
            Mock.Of<IAbuseAlertService>(),
            new SystemCodeSendClock(),
            Options.Create(new AntiAbuseOptions()),
            NullLogger<CodeSendGuard>.Instance);
        return new OnlineVotingService(
            Context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            host.Object,
            NullLogger<OnlineVotingService>.Instance,
            Mock.Of<IHttpClientFactory>(),
            email,
            Mock.Of<IPaidVerificationSender>(),
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            Mock.Of<IOnlineBallotAcceptLock>(),
            codeSendGuard: guard);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<MimeMessage> Messages { get; } = new();

        public Task SendAsync(MimeMessage message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
