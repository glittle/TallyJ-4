using System.Net;
using System.Security.Cryptography;
using System.Text;
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
/// Every voter path uses <see cref="OnlineVotingWindow"/>: open time set and
/// reached, close unset or still ahead, exact close closed.
/// </summary>
public class OnlineVotingWindowRuleTests : ServiceTestBase
{
    private const string BotToken = "test-bot-token";
    private const string Email = "voter@example.com";
    private const string Phone = "+14165560100";
    private const long TelegramId = 424242;

    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<IPaidVerificationSender> _paid = new();
    private readonly Mock<IGoogleIdTokenValidator> _google = new();
    private readonly JsonHandler _http = new();
    private readonly OnlineVotingService _service;

    public OnlineVotingWindowRuleTests()
    {
        _email.Setup(sender => sender.SendAsync(It.IsAny<MimeKit.MimeMessage>()))
            .Returns(Task.CompletedTask);
        _paid.Setup(sender => sender.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var signalR = new Mock<ISignalRNotificationService>();
        signalR.Setup(s => s.NotifyVoterLoginElsewhereAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        signalR.Setup(s => s.NotifyOnlineBallotsChangedAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:BotToken"] = BotToken
            })
            .Build();
        var host = new Mock<IHostEnvironment>();
        host.Setup(environment => environment.EnvironmentName).Returns("Testing");

        _service = new OnlineVotingService(
            Context,
            configuration,
            host.Object,
            Mock.Of<ILogger<OnlineVotingService>>(),
            new SingleHandlerFactory(_http),
            _email.Object,
            _paid.Object,
            _google.Object,
            signalR.Object,
            new OnlineBallotAcceptLock());
    }

    [Theory]
    [InlineData(WindowShape.OpenOnly, true, null)]
    [InlineData(WindowShape.ExactClose, false, "voting.auth.google.noOpenElections")]
    [InlineData(WindowShape.UnsetOpen, false, "voting.auth.google.noOpenElections")]
    public async Task GoogleSignIn_UsesTheSharedWindow(WindowShape shape, bool success, string? error)
    {
        await SeedElectionAsync("Google", shape);
        await AddPersonAsync(Context.Elections.Single().ElectionGuid, email: Email);
        if (shape == WindowShape.ExactClose)
        {
            await StampCloseToNowAsync();
        }

        _google.Setup(validator => validator.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleIdTokenValidationResult { Email = Email, EmailVerified = true });

        var result = await _service.AuthenticateVoterWithGoogleAsync(new GoogleAuthForVoterDto
        {
            Credential = "token"
        });

        Assert.Equal(success, result.Success);
        Assert.Equal(error, result.Error);
    }

    [Theory]
    [InlineData(WindowShape.OpenOnly, true, null)]
    [InlineData(WindowShape.ExactClose, false, "voting.auth.facebook.noOpenElections")]
    [InlineData(WindowShape.UnsetOpen, false, "voting.auth.facebook.noOpenElections")]
    public async Task FacebookSignIn_UsesTheSharedWindow(WindowShape shape, bool success, string? error)
    {
        await SeedElectionAsync("Facebook", shape);
        await AddPersonAsync(Context.Elections.Single().ElectionGuid, email: Email);
        if (shape == WindowShape.ExactClose)
        {
            await StampCloseToNowAsync();
        }

        _http.Body = """{"id":"1","email":"voter@example.com"}""";
        var result = await _service.AuthenticateVoterWithFacebookAsync(new FacebookAuthForVoterDto
        {
            AccessToken = "token"
        });

        Assert.Equal(success, result.Success);
        Assert.Equal(error, result.Error);
    }

    [Theory]
    [InlineData(WindowShape.OpenOnly, true, null)]
    [InlineData(WindowShape.ExactClose, false, "voting.auth.kakao.noOpenElections")]
    [InlineData(WindowShape.UnsetOpen, false, "voting.auth.kakao.noOpenElections")]
    public async Task KakaoSignIn_UsesTheSharedWindow(WindowShape shape, bool success, string? error)
    {
        await SeedElectionAsync("Kakao", shape);
        await AddPersonAsync(Context.Elections.Single().ElectionGuid, email: Email);
        if (shape == WindowShape.ExactClose)
        {
            await StampCloseToNowAsync();
        }

        _http.Body = """{"kakao_account":{"email":"voter@example.com"}}""";
        var result = await _service.AuthenticateVoterWithKakaoAsync(new KakaoAuthForVoterDto
        {
            AccessToken = "token"
        });

        Assert.Equal(success, result.Success);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task Telegram_Succeeds_WhenTheVotersOwnElectionHasOnlyAnOpenTime()
    {
        var own = await SeedElectionAsync("Own", WindowShape.OpenOnly);
        await AddPersonAsync(own, email: TelegramId.ToString());
        AddTelegramVoter();
        await Context.SaveChangesAsync();

        var result = await _service.AuthenticateVoterWithTelegramAsync(TelegramDto());

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Telegram_AtTheCloseInstant_IgnoresAnotherOpenElection()
    {
        var own = await SeedElectionAsync("Own", WindowShape.ExactClose);
        await SeedElectionAsync("Other", WindowShape.OpenOnly);
        await AddPersonAsync(own, email: TelegramId.ToString());
        AddTelegramVoter();
        await Context.SaveChangesAsync();
        await StampCloseToNowAsync(own);

        var result = await _service.AuthenticateVoterWithTelegramAsync(TelegramDto());

        Assert.False(result.Success);
        Assert.Equal("voting.auth.telegram.noOpenElections", result.Error);
    }

    [Fact]
    public async Task Telegram_NotRegistered_WhenTheIdIsNotListedOnAnyElection()
    {
        await SeedElectionAsync("Other", WindowShape.OpenOnly);
        AddTelegramVoter();
        await Context.SaveChangesAsync();

        var result = await _service.AuthenticateVoterWithTelegramAsync(TelegramDto());

        Assert.False(result.Success);
        Assert.Equal("voting.auth.telegram.notRegistered", result.Error);
    }

    [Theory]
    [InlineData(WindowShape.OpenOnly, true)]
    [InlineData(WindowShape.ExactClose, false)]
    [InlineData(WindowShape.UnsetOpen, false)]
    public async Task EmailLoginCodeRequest_UsesTheSharedWindow(WindowShape shape, bool sends)
    {
        var electionId = await SeedElectionAsync("Email code", shape);
        await AddPersonAsync(electionId, email: Email);
        if (shape == WindowShape.ExactClose)
        {
            await StampCloseToNowAsync(electionId);
        }

        var response = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = Email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        if (sends)
        {
            Assert.False(string.IsNullOrEmpty(response.DevVerificationCode));
            _email.Verify(sender => sender.SendAsync(It.IsAny<MimeKit.MimeMessage>()), Times.Once);
        }
        else
        {
            Assert.Null(response.DevVerificationCode);
            _email.Verify(sender => sender.SendAsync(It.IsAny<MimeKit.MimeMessage>()), Times.Never);
        }
    }

    [Theory]
    [InlineData(WindowShape.OpenOnly, true)]
    [InlineData(WindowShape.ExactClose, false)]
    [InlineData(WindowShape.UnsetOpen, false)]
    public async Task PhoneLoginCodeRequest_UsesTheSharedWindow(WindowShape shape, bool sends)
    {
        var electionId = await SeedElectionAsync("Phone code", shape);
        await AddPersonAsync(electionId, phone: Phone);
        if (shape == WindowShape.ExactClose)
        {
            await StampCloseToNowAsync(electionId);
        }

        var response = await _service.RequestVerificationCodeAsync(new RequestCodeDto
        {
            VoterId = Phone,
            VoterIdType = "P",
            DeliveryMethod = "sms"
        });

        if (sends)
        {
            Assert.False(string.IsNullOrEmpty(response.DevVerificationCode));
            _paid.Verify(sender => sender.SendSmsAsync(Phone, It.IsAny<string>()), Times.Once);
        }
        else
        {
            Assert.Null(response.DevVerificationCode);
            _paid.Verify(sender => sender.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    [Theory]
    [InlineData("E", Email, WindowShape.OpenOnly, true, null)]
    [InlineData("E", Email, WindowShape.ExactClose, false, "voting.auth.noOpenElections")]
    [InlineData("P", Phone, WindowShape.OpenOnly, true, null)]
    [InlineData("P", Phone, WindowShape.ExactClose, false, "voting.auth.noOpenElections")]
    public async Task LoginCodeVerify_UsesTheSharedWindow(
        string voterIdType,
        string voterId,
        WindowShape shape,
        bool success,
        string? error)
    {
        var electionId = await SeedElectionAsync("Verify", shape);
        await AddPersonAsync(
            electionId,
            email: voterIdType == "E" ? voterId : null,
            phone: voterIdType == "P" ? voterId : null);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = voterId,
            VoterIdType = voterIdType,
            VerifyCode = "123456",
            VerifyCodeDate = DateTimeOffset.UtcNow,
            VerifyAttempts = 0
        });
        await Context.SaveChangesAsync();
        if (shape == WindowShape.ExactClose)
        {
            await StampCloseToNowAsync(electionId);
        }

        var result = await _service.VerifyCodeAsync(new VerifyCodeDto
        {
            VoterId = voterId,
            VerifyCode = "123456"
        });

        Assert.Equal(success, result.Success);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task KioskCode_Succeeds_WhenOnlyTheOpenTimeHasBeenReached()
    {
        var electionId = await SeedElectionAsync("Kiosk", WindowShape.OpenOnly);
        await SeedKioskAsync(electionId, "AB12C");

        var result = await VerifyKioskAsync("AB12C");

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
    }

    [Fact]
    public async Task KioskCode_AtTheCloseInstant_IsRejected()
    {
        var electionId = await SeedElectionAsync("Kiosk", WindowShape.ExactClose);
        await SeedKioskAsync(electionId, "AB12C");
        await StampCloseToNowAsync(electionId);

        var result = await VerifyKioskAsync("AB12C");

        Assert.False(result.Success);
        Assert.Equal(VoterVerifyError.VoterNotFound, result.Error);
    }

    [Fact]
    public async Task KioskCode_FlaggedElection_IsRejected()
    {
        var electionId = await SeedElectionAsync("Kiosk", WindowShape.OpenOnly);
        await SeedKioskAsync(electionId, "AB12C");
        Context.ElectionSendControls.Add(new ElectionSendControl
        {
            ElectionGuid = electionId,
            Flagged = true
        });
        await Context.SaveChangesAsync();

        var result = await VerifyKioskAsync("AB12C");

        Assert.False(result.Success);
        Assert.Equal(VoterVerifyError.VoterNotFound, result.Error);
    }

    [Fact]
    public async Task KioskCode_RejectedOwnerWithNoApprovedOwner_IsRejected()
    {
        var electionId = await SeedElectionAsync("Kiosk", WindowShape.OpenOnly);
        await SeedKioskAsync(electionId, "AB12C");
        var ownerId = Guid.NewGuid();
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = electionId,
            UserId = ownerId,
            Role = "Owner"
        });
        Context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = ownerId,
            RestrictionStatus = OwnerRestrictionStatus.Rejected,
            PaidSendsApproved = false
        });
        await Context.SaveChangesAsync();

        var result = await VerifyKioskAsync("AB12C");

        Assert.False(result.Success);
        Assert.Equal(VoterVerifyError.VoterNotFound, result.Error);
    }

    [Fact]
    public async Task AvailableElections_AndElectionInfo_UseTheSharedWindow()
    {
        var openOnly = await SeedElectionAsync("Open only", WindowShape.OpenOnly);
        var exactClose = await SeedElectionAsync("Exact close", WindowShape.ExactClose);
        var unsetOpen = await SeedElectionAsync("Unset open", WindowShape.UnsetOpen);
        await AddPersonAsync(openOnly, email: Email);
        await AddPersonAsync(exactClose, email: Email);
        await AddPersonAsync(unsetOpen, email: Email);
        await StampCloseToNowAsync(exactClose);

        var available = await _service.GetAvailableElectionsAsync(Email);
        Assert.Equal(openOnly, Assert.Single(available).ElectionGuid);

        Assert.True((await _service.GetElectionInfoAsync(openOnly))!.IsOpen);
        Assert.False((await _service.GetElectionInfoAsync(exactClose))!.IsOpen);
        Assert.False((await _service.GetElectionInfoAsync(unsetOpen))!.IsOpen);
    }

    [Fact]
    public async Task Submit_OpenTimeOnly_Succeeds()
    {
        var electionId = await SeedElectionAsync("Submit", WindowShape.OpenOnly);
        await SeedBallotPeopleAsync(electionId);

        var result = await SubmitAsync(electionId);

        Assert.True(result.Success);
        Assert.Equal(OnlineBallotStatus.Submitted, Context.OnlineVotingInfos.Single().Status);
    }

    [Fact]
    public async Task Submit_AtTheCloseInstant_IsRejected()
    {
        var electionId = await SeedElectionAsync("Submit", WindowShape.ExactClose);
        await SeedBallotPeopleAsync(electionId);
        await StampCloseToNowAsync(electionId);

        var result = await SubmitAsync(electionId);

        Assert.False(result.Success);
        Assert.Equal("voting.submit.notOpen", result.Error);
        Assert.Empty(Context.OnlineVotingInfos);
    }

    [Fact]
    public async Task Submit_ClearedOpenTime_RejectsAnExistingSession()
    {
        var electionId = await SeedElectionAsync("Submit", WindowShape.OpenOnly);
        await SeedBallotPeopleAsync(electionId);

        var first = await SubmitAsync(electionId);
        Assert.True(first.Success);

        var election = Context.Elections.Single(row => row.ElectionGuid == electionId);
        election.OnlineWhenOpen = null;
        await Context.SaveChangesAsync();

        var second = await SubmitAsync(electionId);

        Assert.False(second.Success);
        Assert.Equal("voting.submit.notOpen", second.Error);
        Assert.Equal(OnlineBallotStatus.Submitted, Context.OnlineVotingInfos.Single().Status);
    }

    public enum WindowShape
    {
        OpenOnly,
        ExactClose,
        UnsetOpen
    }

    private async Task<Guid> SeedElectionAsync(string name, WindowShape shape)
    {
        var electionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionId,
            Name = name,
            UseOnlineVoting = true,
            OnlineWhenOpen = shape == WindowShape.UnsetOpen ? null : now.AddHours(-1),
            OnlineWhenClose = shape switch
            {
                WindowShape.OpenOnly => null,
                WindowShape.ExactClose => now,
                _ => now.AddHours(2)
            },
            ElectionStage = ElectionStage.GatheringBallots,
            NumberToElect = 1,
            OnlineSelectionProcess = "A",
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
        return electionId;
    }

    private async Task StampCloseToNowAsync(Guid? electionId = null)
    {
        var election = electionId == null
            ? Context.Elections.Single()
            : Context.Elections.Single(row => row.ElectionGuid == electionId);
        election.OnlineWhenClose = DateTimeOffset.UtcNow;
        await Context.SaveChangesAsync();
    }

    private async Task AddPersonAsync(Guid electionId, string? email = null, string? phone = null, string? kioskCode = null)
    {
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
            CanReceiveVotes = false,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
    }

    private void AddTelegramVoter()
    {
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = TelegramId.ToString(),
            VoterIdType = "T",
            WhenRegistered = DateTimeOffset.UtcNow
        });
    }

    private TelegramAuthForVoterDto TelegramDto()
    {
        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const string firstName = "Ada";
        return new TelegramAuthForVoterDto
        {
            Id = TelegramId,
            FirstName = firstName,
            AuthDate = authDate,
            Hash = TelegramHash(TelegramId, firstName, authDate)
        };
    }

    private static string TelegramHash(long id, string firstName, long authDate)
    {
        var data = $"auth_date={authDate}\nfirst_name={firstName}\nid={id}";
        var secret = SHA256.HashData(Encoding.UTF8.GetBytes(BotToken));
        var hash = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task SeedKioskAsync(Guid electionId, string code)
    {
        await AddPersonAsync(electionId, kioskCode: code);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = KioskCodeLifetime.ToVoterId(electionId, code),
            VoterIdType = KioskCodeLifetime.VoterIdType,
            VerifyCode = code,
            VerifyCodeDate = DateTimeOffset.UtcNow,
            VerifyAttempts = 0
        });
        await Context.SaveChangesAsync();
    }

    private Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> VerifyKioskAsync(string code)
    {
        return _service.VerifyCodeAsync(new VerifyCodeDto
        {
            VoterId = code,
            VerifyCode = code
        });
    }

    private async Task SeedBallotPeopleAsync(Guid electionId)
    {
        await AddPersonAsync(electionId, email: Email);
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = Email,
            VoterIdType = "E",
            WhenRegistered = DateTimeOffset.UtcNow
        });
        Context.People.Add(new Person
        {
            ElectionGuid = electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Ann",
            LastName = "Doe",
            CanReceiveVotes = true,
            CanVote = true,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();
    }

    private Task<(bool Success, string? Error)> SubmitAsync(Guid electionId)
    {
        var candidate = Context.People.Single(person =>
            person.ElectionGuid == electionId && person.CanReceiveVotes == true);
        return _service.SubmitBallotAsync(new SubmitOnlineBallotDto
        {
            ElectionGuid = electionId,
            VoterId = Email,
            Votes =
            [
                new OnlineVoteDto
                {
                    PersonGuid = candidate.PersonGuid,
                    VoteName = "Doe, Ann",
                    PositionOnBallot = 1
                }
            ]
        });
    }

    private sealed class JsonHandler : HttpMessageHandler
    {
        public string Body { get; set; } = "{}";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://example.test/")
            };
    }
}
