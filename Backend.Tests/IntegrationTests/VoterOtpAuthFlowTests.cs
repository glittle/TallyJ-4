using System.Net;
using System.Net.Http.Json;
using Backend.Context;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;
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
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task PhoneRequestCode_ThenVerifyCode_ReturnsVoterSession()
    {
        var phone = "+15559870123";
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
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
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
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
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