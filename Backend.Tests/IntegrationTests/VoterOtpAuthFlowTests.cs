using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Context;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the full OTP auth path: requestCode → verifyCode → session.
/// </summary>
public class VoterOtpAuthFlowTests : IntegrationTestBase
{
    public VoterOtpAuthFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task EmailRequestCode_ThenVerifyCode_ReturnsVoterSession()
    {
        var email = $"otp_email_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);

        var requestResponse = await Client.PostAsJsonAsync("/api/online-voting/requestCode", new RequestCodeDto
        {
            VoterId = email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);
        var requestBody = await requestResponse.Content.ReadFromJsonAsync<RequestCodeResponseDto>();
        Assert.NotNull(requestBody);
        Assert.False(string.IsNullOrWhiteSpace(requestBody.DevVerificationCode),
            "Testing environment must echo devVerificationCode for OTP flow tests");

        var verifyResponse = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = requestBody.DevVerificationCode!
        });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var auth = await verifyResponse.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(email, auth.VoterId);
        Assert.Equal("E", auth.VoterIdType);
        Assert.True(string.IsNullOrEmpty(auth.Token));
        Assert.False(string.IsNullOrWhiteSpace(GetSetCookieValue(verifyResponse, "voter_token")));
    }

    [Fact]
    public async Task PhoneRequestCode_ThenVerifyCode_ReturnsVoterSession()
    {
        var phone = "+14168972672";
        await SetupOpenElectionWithVoter(phone: phone);

        var requestResponse = await Client.PostAsJsonAsync("/api/online-voting/requestCode", new RequestCodeDto
        {
            VoterId = phone,
            VoterIdType = "P",
            DeliveryMethod = "sms"
        });

        Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);
        var requestBody = await requestResponse.Content.ReadFromJsonAsync<RequestCodeResponseDto>();
        Assert.NotNull(requestBody);
        Assert.False(string.IsNullOrWhiteSpace(requestBody.DevVerificationCode));

        var verifyResponse = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = phone,
            VerifyCode = requestBody.DevVerificationCode!
        });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var auth = await verifyResponse.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(phone, auth.VoterId);
        Assert.Equal("P", auth.VoterIdType);
        Assert.True(string.IsNullOrEmpty(auth.Token));
        Assert.False(string.IsNullOrWhiteSpace(GetSetCookieValue(verifyResponse, "voter_token")));
    }

    [Fact]
    public async Task GoogleAuth_WithDevCredential_ReturnsVoterSession()
    {
        var email = $"otp_google_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);

        var response = await Client.PostAsJsonAsync("/api/online-voting/googleAuth", new GoogleAuthForVoterDto
        {
            Credential = $"dev-google:{email}"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(email, auth.VoterId);
        Assert.Equal("E", auth.VoterIdType);
        Assert.True(string.IsNullOrEmpty(auth.Token));
        Assert.False(string.IsNullOrWhiteSpace(GetSetCookieValue(response, "voter_token")));
    }

    [Fact]
    public async Task VerifyCode_ExpiredOtp_ReturnsCodeExpired()
    {
        var email = $"otp_expired_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        await SeedOnlineVoter(email, "ABCDEF", DateTimeOffset.UtcNow.AddMinutes(-16));

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = "ABCDEF"
        });

        await AssertVerifyErrorAsync(response, VoterVerifyError.CodeExpired);
    }

    [Fact]
    public async Task VerifyCode_AfterSuccessfulLogin_ReturnsAlreadyUsed()
    {
        var email = $"otp_used_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);

        var requestResponse = await Client.PostAsJsonAsync("/api/online-voting/requestCode", new RequestCodeDto
        {
            VoterId = email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });
        var requestBody = await requestResponse.Content.ReadFromJsonAsync<RequestCodeResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(requestBody?.DevVerificationCode));

        var first = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = requestBody!.DevVerificationCode!
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var replay = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = requestBody.DevVerificationCode!
        });

        await AssertVerifyErrorAsync(replay, VoterVerifyError.AlreadyUsed);
    }

    [Fact]
    public async Task VerifyCode_NeverIssued_ReturnsNoCodeFound()
    {
        var email = $"otp_nocode_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        await SeedOnlineVoter(email, verifyCode: null, verifyCodeDate: null);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = "ABCDEF"
        });

        await AssertVerifyErrorAsync(response, VoterVerifyError.NoCodeFound);
    }

    [Fact]
    public async Task VerifyCode_FifthMismatch_ReturnsTooManyAttempts()
    {
        var email = $"otp_locked_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        await SeedOnlineVoter(email, "ABCDEF", DateTimeOffset.UtcNow, attempts: 4);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = "XXXXXX"
        });

        await AssertVerifyErrorAsync(response, VoterVerifyError.TooManyAttempts);
    }

    [Fact]
    public async Task VerifyCode_AlreadyLocked_ReturnsTooManyAttempts()
    {
        var email = $"otp_already_locked_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        await SeedOnlineVoter(email, "ABCDEF", DateTimeOffset.UtcNow, attempts: 5);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = "XXXXXX"
        });

        await AssertVerifyErrorAsync(response, VoterVerifyError.TooManyAttempts);
    }

    [Fact]
    public async Task VerifyCode_Mismatch_ReturnsStableInvalidCodeKeyAndAttempts()
    {
        var email = $"otp_mismatch_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        await SeedOnlineVoter(email, "ABCDEF", DateTimeOffset.UtcNow);

        var response = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = "XXXXXX"
        });

        var (body, json) = await ReadVerifyErrorAsync(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(VoterVerifyError.InvalidCode, body.Error);
        Assert.Equal(4, body.Attempts);
        Assert.DoesNotContain("invalidCode:", json, StringComparison.Ordinal);
    }

    private async Task SeedOnlineVoter(
        string voterId,
        string? verifyCode,
        DateTimeOffset? verifyCodeDate,
        int? attempts = 0)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = voterId,
            VoterIdType = "E",
            WhenRegistered = DateTimeOffset.UtcNow,
            VerifyCode = verifyCode,
            VerifyCodeDate = verifyCodeDate,
            VerifyAttempts = attempts
        });
        await context.SaveChangesAsync();
    }

    private async Task<VerifyErrorBody> AssertVerifyErrorAsync(
        HttpResponseMessage response,
        string expectedKey)
    {
        var (body, _) = await ReadVerifyErrorAsync(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedKey, body.Error);
        return body;
    }

    private async Task<(VerifyErrorBody Body, string Json)> ReadVerifyErrorAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<VerifyErrorBody>(json, JsonOptions);
        Assert.NotNull(body);
        return (body, json);
    }

    private sealed class VerifyErrorBody
    {
        public string? Error { get; set; }
        public int? Attempts { get; set; }
    }

    private async Task<Guid> SetupOpenElectionWithVoter(string? email = null, string? phone = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "OTP Auth Election",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            NumberToElect = 9,
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });

        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "OTP",
            LastName = "Voter",
            Email = email,
            Phone = phone,
            CanVote = true,
            RowVersion = new byte[8]
        });

        await context.SaveChangesAsync();
        return electionGuid;
    }
}