using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Paid-channel requestCode gate: reserved/malformed phones and blocked SmsStatus
/// never reach the provider (SmsStatus is checked before registration);
/// null/"OK" still hits send or the registration check.
/// </summary>
public class OnlineVotingServiceRequestCodePaidPhoneTests : ServiceTestBase
{
    private const string ValidPhone = "+14168972671";

    private readonly Mock<IPaidVerificationSender> _paidSender = new();
    private readonly OnlineVotingService _service;

    public OnlineVotingServiceRequestCodePaidPhoneTests()
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
            _paidSender.Object,
            Mock.Of<IGoogleIdTokenValidator>(),
            Mock.Of<ISignalRNotificationService>());
    }

    [Theory]
    [InlineData("+15551234567")]
    [InlineData("+14155550100")]
    [InlineData("+14155551212")]
    [InlineData("not-a-phone")]
    [InlineData("+123")]
    [InlineData("+0123456789")]
    public async Task RequestCode_ReservedOrMalformedPhone_DoesNotCallProvider(string phone)
    {
        await SeedOpenElectionWithPerson(phone: phone);

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(phone));

        Assert.Equal("voting.auth.requestCode.invalidPhone", result.MessageKey);
        Assert.Null(result.DevVerificationCode);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.False(await Context.OnlineVoters.AnyAsync(ov => ov.VoterId == phone));
    }

    [Theory]
    [InlineData("voice")]
    [InlineData("whatsapp")]
    public async Task RequestCode_ReservedPhone_BlocksVoiceAndWhatsApp(string deliveryMethod)
    {
        const string phone = "+15550123456";
        await SeedOpenElectionWithPerson(phone: phone);

        var result = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = phone,
            VoterIdType = "P",
            DeliveryMethod = deliveryMethod
        });

        Assert.Equal("voting.auth.requestCode.invalidPhone", result.MessageKey);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestCode_ValidE164_NotRegistered_ReachesRegistrationCheck()
    {
        await SeedOpenElectionWithPerson(email: "other@example.com");

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.notRegistered", result.MessageKey);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.False(await Context.OnlineVoters.AnyAsync(ov => ov.VoterId == ValidPhone));
    }

    [Fact]
    public async Task RequestCode_ValidE164_Registered_CallsMockedProvider()
    {
        await SeedOpenElectionWithPerson(phone: ValidPhone);
        _paidSender
            .Setup(s => s.SendSmsAsync(ValidPhone, It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        Assert.False(string.IsNullOrWhiteSpace(result.DevVerificationCode));
        _paidSender.Verify(s => s.SendSmsAsync(ValidPhone, It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData("undeliverable")]
    [InlineData("555-range")]
    public async Task RequestCode_SmsStatusBlocked_DoesNotCallProvider(string smsStatus)
    {
        await SeedOpenElectionWithPerson(phone: ValidPhone);
        await SeedOnlineVoter(ValidPhone, smsStatus);

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.invalidPhone", result.MessageKey);
        Assert.Null(result.DevVerificationCode);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("voice")]
    [InlineData("whatsapp")]
    public async Task RequestCode_SmsStatusBlocked_BlocksVoiceAndWhatsApp(string deliveryMethod)
    {
        await SeedOpenElectionWithPerson(phone: ValidPhone);
        await SeedOnlineVoter(ValidPhone, "undeliverable");

        var result = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = ValidPhone,
            VoterIdType = "P",
            DeliveryMethod = deliveryMethod
        });

        Assert.Equal("voting.auth.requestCode.invalidPhone", result.MessageKey);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestCode_SmsStatusOk_Registered_CallsMockedProvider()
    {
        await SeedOpenElectionWithPerson(phone: ValidPhone);
        await SeedOnlineVoter(ValidPhone, OnlineVoterSmsStatus.Ok);
        _paidSender
            .Setup(s => s.SendSmsAsync(ValidPhone, It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        Assert.False(string.IsNullOrWhiteSpace(result.DevVerificationCode));
        _paidSender.Verify(s => s.SendSmsAsync(ValidPhone, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestCode_SmsStatusNull_Registered_CallsMockedProvider()
    {
        await SeedOpenElectionWithPerson(phone: ValidPhone);
        await SeedOnlineVoter(ValidPhone, smsStatus: null);
        _paidSender
            .Setup(s => s.SendSmsAsync(ValidPhone, It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        _paidSender.Verify(s => s.SendSmsAsync(ValidPhone, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestCode_SmsStatusOk_NotRegistered_ReachesRegistrationCheck()
    {
        await SeedOpenElectionWithPerson(email: "other@example.com");
        await SeedOnlineVoter(ValidPhone, OnlineVoterSmsStatus.Ok);

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.notRegistered", result.MessageKey);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestCode_SmsStatusBlocked_NotRegistered_ReturnsInvalidPhone()
    {
        await SeedOpenElectionWithPerson(email: "other@example.com");
        await SeedOnlineVoter(ValidPhone, "undeliverable");

        var result = await _service.RequestVerificationCodeAsync(PaidSmsRequest(ValidPhone));

        Assert.Equal("voting.auth.requestCode.invalidPhone", result.MessageKey);
        Assert.NotEqual("voting.auth.requestCode.notRegistered", result.MessageKey);
        Assert.Null(result.DevVerificationCode);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestCode_Email_DoesNotUsePaidSender()
    {
        const string email = "voter@example.com";
        await SeedOpenElectionWithPerson(email: email);

        var result = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        Assert.Equal("voting.auth.requestCode.sent", result.MessageKey);
        _paidSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendVoiceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _paidSender.Verify(s => s.SendWhatsAppAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static RequestCodeDto PaidSmsRequest(string phone) => new()
    {
        VoterId = phone,
        VoterIdType = "P",
        DeliveryMethod = "sms"
    };

    private async Task SeedOpenElectionWithPerson(string? email = null, string? phone = null)
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Paid phone gate election",
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

    private async Task SeedOnlineVoter(string voterId, string? smsStatus, string voterIdType = "P")
    {
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = voterId,
            VoterIdType = voterIdType,
            SmsStatus = smsStatus
        });
        await Context.SaveChangesAsync();
    }
}
