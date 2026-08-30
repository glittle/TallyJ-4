using System.Net;
using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// HTTP form-bind path for the single Twilio status callback (v3 Public/SmsStatus).
/// </summary>
public class TwilioSmsStatusCallbackTests : IntegrationTestBase
{
    public TwilioSmsStatusCallbackTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SmsStatus_Undelivered30003_Anonymous_LearnsOnPhoneRow()
    {
        const string phone = "+16048971234";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = phone,
                VoterIdType = OnlineVoterPhoneHelper.PhoneVoterIdType
            });
            await db.SaveChangesAsync();
        }

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["MessageSid"] = "SMintegration1",
            ["MessageStatus"] = "undelivered",
            ["To"] = phone,
            ["ErrorCode"] = "30003"
        });

        var response = await Client.PostAsync("/api/Public/smsStatus", form);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = Factory.Services.CreateScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await dbVerify.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("twilio-30003", row.SmsStatus);
    }

    [Fact]
    public async Task SmsStatus_Delivered_DoesNotWriteSmsStatus()
    {
        const string phone = "+16048971235";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = phone,
                VoterIdType = OnlineVoterPhoneHelper.PhoneVoterIdType
            });
            await db.SaveChangesAsync();
        }

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["MessageSid"] = "SMintegration2",
            ["MessageStatus"] = "delivered",
            ["To"] = phone
        });

        var response = await Client.PostAsync("/api/Public/smsStatus", form);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verify = Factory.Services.CreateScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await dbVerify.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Null(row.SmsStatus);
    }
}
