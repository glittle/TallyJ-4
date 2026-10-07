using System.Diagnostics;
using Backend.Configuration;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// requestCode returns one success-shaped reply whether or not the identifier is listed.
/// A send happens only for an eligible voter. The reply waits out a shared minimum.
/// </summary>
public class OnlineVotingServiceRequestCodeNeutralTests : ServiceTestBase
{
    private const string ListedPhone = "+14168972671";
    private const string UnlistedPhone = "+14168972680";
    private readonly Mock<IPaidVerificationSender> _paid = new();
    private readonly Mock<IEmailSender> _email = new();

    public OnlineVotingServiceRequestCodeNeutralTests()
    {
        _paid.Setup(sender => sender.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _paid.Setup(sender => sender.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _paid.Setup(sender => sender.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        _email.Setup(sender => sender.SendAsync(It.IsAny<MimeMessage>())).Returns(Task.CompletedTask);
    }

    [Theory]
    [InlineData("email", "E", "voter@example.com", "other@example.com")]
    [InlineData("sms", "P", ListedPhone, UnlistedPhone)]
    [InlineData("voice", "P", ListedPhone, UnlistedPhone)]
    [InlineData("whatsapp", "P", ListedPhone, UnlistedPhone)]
    public async Task RequestCode_UnlistedMatchesListed_AndNothingIsSent(
        string method,
        string voterIdType,
        string listedId,
        string unlistedId)
    {
        await SeedListedAsync(voterIdType == "E" ? listedId : null, voterIdType == "P" ? listedId : null);
        var service = CreateService(Environments.Production, pacer: null);

        var listed = await service.RequestVerificationCodeAsync(Request(listedId, voterIdType, method));
        var unlisted = await service.RequestVerificationCodeAsync(Request(unlistedId, voterIdType, method));

        Assert.Equal(RequestCodeReply.NeutralMessageKey, listed.MessageKey);
        Assert.Equal(listed.MessageKey, unlisted.MessageKey);
        Assert.Equal(listed.DevVerificationCode, unlisted.DevVerificationCode);
        Assert.Equal(listed.ChannelToken, unlisted.ChannelToken);
        Assert.Null(listed.DevVerificationCode);
        Assert.Null(unlisted.DevVerificationCode);

        if (method == "email")
        {
            _email.Verify(sender => sender.SendAsync(It.Is<MimeMessage>(message =>
                message.To.ToString()!.Contains(listedId))), Times.Once);
            _email.Verify(sender => sender.SendAsync(It.Is<MimeMessage>(message =>
                message.To.ToString()!.Contains(unlistedId))), Times.Never);
        }
        else
        {
            VerifyPaid(method, listedId, Times.Once());
            VerifyPaid(method, unlistedId, Times.Never());
        }
    }

    [Fact]
    public async Task RequestCode_KioskCode_DoesNotSend_AndMatchesTheNeutralReply()
    {
        await SeedListedAsync(email: null, phone: null, kioskCode: "SMART");
        var service = CreateService(Environments.Production, pacer: null);

        var listed = await service.RequestVerificationCodeAsync(Request("SMART", "C", "email"));
        var unknown = await service.RequestVerificationCodeAsync(Request("nobody@example.com", "E", "email"));

        Assert.Equal(RequestCodeReply.NeutralMessageKey, listed.MessageKey);
        Assert.Equal(listed.MessageKey, unknown.MessageKey);
        Assert.Null(listed.DevVerificationCode);
        Assert.Null(unknown.DevVerificationCode);
        _email.Verify(sender => sender.SendAsync(It.IsAny<MimeMessage>()), Times.Never);
        _paid.Verify(sender => sender.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestCode_ListedAndUnlisted_BothWaitOutTheMinimum()
    {
        await SeedListedAsync("paced@example.com", null);
        var service = CreateService(
            Environments.Production,
            new RequestCodePacer(Options.Create(new AntiAbuseOptions
            {
                RequestCodeMinimumMilliseconds = 120
            })));

        var listedElapsed = await ElapsedAsync(() =>
            service.RequestVerificationCodeAsync(Request("paced@example.com", "E", "email")));
        var unlistedElapsed = await ElapsedAsync(() =>
            service.RequestVerificationCodeAsync(Request("missing@example.com", "E", "email")));

        Assert.True(listedElapsed >= TimeSpan.FromMilliseconds(100), listedElapsed.ToString());
        Assert.True(unlistedElapsed >= TimeSpan.FromMilliseconds(100), unlistedElapsed.ToString());
        var gap = listedElapsed > unlistedElapsed
            ? listedElapsed - unlistedElapsed
            : unlistedElapsed - listedElapsed;
        Assert.True(gap < TimeSpan.FromMilliseconds(500), gap.ToString());
    }

    private void VerifyPaid(string method, string phone, Times times)
    {
        switch (method)
        {
            case "sms":
                _paid.Verify(sender => sender.SendSmsAsync(phone, It.IsAny<string>()), times);
                break;
            case "voice":
                _paid.Verify(sender => sender.SendVoiceAsync(phone, It.IsAny<string>()), times);
                break;
            default:
                _paid.Verify(sender => sender.SendWhatsAppAsync(phone, It.IsAny<string>()), times);
                break;
        }
    }

    private OnlineVotingService CreateService(string environment, IRequestCodePacer? pacer)
    {
        var host = new Mock<IHostEnvironment>();
        host.Setup(item => item.EnvironmentName).Returns(environment);
        return new OnlineVotingService(
            Context,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            host.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            _email.Object,
            _paid.Object,
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>(),
            Mock.Of<IOnlineBallotAcceptLock>(),
            requestCodePacer: pacer);
    }

    private async Task SeedListedAsync(string? email, string? phone, string? kioskCode = null)
    {
        var electionId = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionId,
            Name = "Neutral reply election",
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
            Phone = phone,
            KioskCode = kioskCode,
            CanVote = true,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
    }

    private static RequestCodeDto Request(string voterId, string voterIdType, string method) => new()
    {
        VoterId = voterId,
        VoterIdType = voterIdType,
        DeliveryMethod = method
    };

    private static async Task<TimeSpan> ElapsedAsync(Func<Task> action)
    {
        var started = Stopwatch.GetTimestamp();
        await action();
        return Stopwatch.GetElapsedTime(started);
    }
}
