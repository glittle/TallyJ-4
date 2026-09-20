using System.Text.Json;
using Backend.DTOs.OnlineVoting;
using Backend.DTOs.SignalR;
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
/// requestCode issues a delivery-status channel only after pumping / eligibility gates,
/// and status payloads never include the OTP.
/// </summary>
public class OnlineVotingServiceRequestCodeChannelTests : ServiceTestBase
{
    private const string ValidPhone = "+14168972671";

    private readonly Mock<IPaidVerificationSender> _paidSender = new();
    private readonly Mock<ISignalRNotificationService> _signalR = new();
    private readonly VoterCodeDeliveryChannelService _channels = new();
    private readonly List<VoterCodeDeliveryStatusDto> _pushed = [];
    private readonly OnlineVotingService _service;

    public OnlineVotingServiceRequestCodeChannelTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(e => e.EnvironmentName).Returns("Testing");
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>()))
            .Returns(Task.CompletedTask);
        _signalR
            .Setup(s => s.SendVoterCodeDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<VoterCodeDeliveryStatusDto>()))
            .Callback<string, VoterCodeDeliveryStatusDto>((_, status) => _pushed.Add(status))
            .Returns(Task.CompletedTask);
        _paidSender
            .Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _service = new OnlineVotingService(
            Context,
            configuration,
            hostEnvironment.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            Mock.Of<IHttpClientFactory>(),
            emailSender.Object,
            _paidSender.Object,
            Mock.Of<IGoogleIdTokenValidator>(),
            _signalR.Object,
            Mock.Of<IOnlineBallotAcceptLock>(),
            _channels);
    }

    [Fact]
    public async Task RequestCode_NotRegistered_DoesNotIssueChannel()
    {
        await SeedOpenElection(phone: ValidPhone);

        var result = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = "+14168972672",
            VoterIdType = "P",
            DeliveryMethod = "sms"
        });

        Assert.Equal("voting.auth.requestCode.notRegistered", result.MessageKey);
        Assert.Null(result.ChannelToken);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Empty(_pushed);
    }

    [Fact]
    public async Task RequestCode_SuccessfulSms_IssuesJoinableToken_AndStatusHasNoOtp()
    {
        await SeedOpenElection(phone: ValidPhone);

        var result = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = ValidPhone,
            VoterIdType = "P",
            DeliveryMethod = "sms"
        });

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        Assert.False(string.IsNullOrWhiteSpace(result.ChannelToken));
        Assert.True(result.ChannelToken!.Length >= 64);
        Assert.NotEqual(result.DevVerificationCode, result.ChannelToken);
        Assert.True(_channels.TryJoin(result.ChannelToken, "conn-1", out _));
        Assert.Contains(_pushed, s => s.Status == VoterCodeDeliveryStatuses.Sending);
        Assert.Contains(_pushed, s => s.Status == VoterCodeDeliveryStatuses.Sent);
        Assert.Contains(_pushed, s => s.Status == VoterCodeDeliveryStatuses.Final && s.Okay == true);

        foreach (var status in _pushed)
        {
            var json = JsonSerializer.Serialize(status);
            Assert.DoesNotContain(result.DevVerificationCode ?? "___none___", json);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                Assert.DoesNotContain("code", prop.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("otp", prop.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task RequestCode_SuccessfulEmail_IssuesChannelAndFinal()
    {
        const string email = "voter-channel@example.com";
        await SeedOpenElection(email: email);

        var result = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        Assert.False(string.IsNullOrWhiteSpace(result.ChannelToken));
        Assert.Contains(_pushed, s => s.Status == VoterCodeDeliveryStatuses.Final && s.Okay == true);
    }

    private async Task SeedOpenElection(string? email = null, string? phone = null)
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Channel requestCode election",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTimeOffset.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            NumberToElect = 9,
            RowVersion = new byte[8]
        });
        Context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Voter",
            Email = email,
            Phone = phone,
            CanVote = true,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
    }
}
