using System.Net;
using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// HTTP form-bind path for the single Twilio status callback (v3 Public/SmsStatus).
/// Unsigned or badly signed POSTs must not write SmsStatus.
/// </summary>
public class TwilioSmsStatusCallbackTests : IntegrationTestBase
{
    public TwilioSmsStatusCallbackTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SmsStatus_SignedUndelivered30003_Anonymous_LearnsOnPhoneRow()
    {
        const string phone = "+16048971234";
        await SeedPhoneRow(phone);

        var fields = new Dictionary<string, string>
        {
            ["MessageSid"] = "SMintegration1",
            ["MessageStatus"] = "undelivered",
            ["To"] = phone,
            ["ErrorCode"] = "30003"
        };

        var response = await PostSmsStatusAsync(fields, sign: true);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("twilio-30003", await ReadSmsStatus(phone));
    }

    [Fact]
    public async Task SmsStatus_SignedDelivered_DoesNotWriteSmsStatus()
    {
        const string phone = "+16048971235";
        await SeedPhoneRow(phone);

        var fields = new Dictionary<string, string>
        {
            ["MessageSid"] = "SMintegration2",
            ["MessageStatus"] = "delivered",
            ["To"] = phone
        };

        var response = await PostSmsStatusAsync(fields, sign: true);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await ReadSmsStatus(phone));
    }

    [Fact]
    public async Task SmsStatus_Unsigned_Forbidden_DoesNotWriteSmsStatus()
    {
        const string phone = "+16048971236";
        await SeedPhoneRow(phone);

        var fields = new Dictionary<string, string>
        {
            ["MessageSid"] = "SMunsigned",
            ["MessageStatus"] = "undelivered",
            ["To"] = phone,
            ["ErrorCode"] = "30003"
        };

        var response = await PostSmsStatusAsync(fields, sign: false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(await ReadSmsStatus(phone));
    }

    [Fact]
    public async Task SmsStatus_InvalidSignature_Forbidden_DoesNotWriteSmsStatus()
    {
        const string phone = "+16048971237";
        await SeedPhoneRow(phone);

        var fields = new Dictionary<string, string>
        {
            ["MessageSid"] = "SMbadsig",
            ["MessageStatus"] = "undelivered",
            ["To"] = phone,
            ["ErrorCode"] = "30003"
        };

        var content = new FormUrlEncodedContent(fields);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Public/smsStatus")
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation(TwilioRequestSignature.HeaderName, "AAAAAAAAAAAAAAAAAAAAAAAAAAA=");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(await ReadSmsStatus(phone));
    }

    private async Task SeedPhoneRow(string phone)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = OnlineVoterPhoneHelper.PhoneVoterIdType
        });
        await db.SaveChangesAsync();
    }

    private async Task<string?> ReadSmsStatus(string phone)
    {
        using var verify = Factory.Services.CreateScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await dbVerify.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        return row.SmsStatus;
    }

    private async Task<HttpResponseMessage> PostSmsStatusAsync(
        Dictionary<string, string> fields,
        bool sign)
    {
        const string path = "/api/Public/smsStatus";
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        if (sign)
        {
            var url = new Uri(Client.BaseAddress!, path).ToString();
            var signature = TwilioRequestSignature.Compute(
                CustomWebApplicationFactory.TwilioAuthToken, url, fields);
            request.Headers.TryAddWithoutValidation(TwilioRequestSignature.HeaderName, signature);
        }

        return await Client.SendAsync(request);
    }
}
